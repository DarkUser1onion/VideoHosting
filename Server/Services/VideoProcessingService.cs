using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public interface IVideoProcessingService
{
    Task ProcessVideoAsync(Guid videoId, string inputPath, string outputFolder);
    Task<string?> GeneratePreviewAsync(string inputPath, string outputFolder);
    Task<int> GetVideoDurationAsync(string inputPath);
    Task<List<string>> ConvertToHlsAsync(string inputPath, string outputFolder);
}

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VideoProcessingService> _logger;
    
    public VideoProcessingService(IServiceProvider serviceProvider, ILogger<VideoProcessingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task ProcessVideoAsync(Guid videoId, string inputPath, string outputFolder)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            _logger.LogInformation("Processing video {VideoId}", videoId);
            
            // Get video duration
            var duration = await GetVideoDurationAsync(inputPath);
            
            // Generate preview
            var previewPath = await GeneratePreviewAsync(inputPath, outputFolder);
            
            // Convert to HLS
            var hlsFiles = await ConvertToHlsAsync(inputPath, outputFolder);
            
            // Update video in database
            var video = await context.Videos.FindAsync(videoId);
            if (video != null)
            {
                video.Duration = duration;
                video.PreviewPath = previewPath;
                video.Status = "pending"; // Ready for moderation
                await context.SaveChangesAsync();
            }
            
            _logger.LogInformation("Video {VideoId} processed successfully", videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video {VideoId}", videoId);
            
            var video = await context.Videos.FindAsync(videoId);
            if (video != null)
            {
                video.Status = "error";
                await context.SaveChangesAsync();
            }
        }
    }
    
    public async Task<string?> GeneratePreviewAsync(string inputPath, string outputFolder)
    {
        try
        {
            var outputPath = Path.Combine(outputFolder, "preview.jpg");
            
            await Task.Run(() =>
            {
                // Use FFmpeg to extract a frame at 1 second
                FFMpeg.Snapshot(inputPath, outputPath);
            });
            
            return File.Exists(outputPath) ? outputPath : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for {InputPath}", inputPath);
            return null;
        }
    }
    
    public async Task<int> GetVideoDurationAsync(string inputPath)
    {
        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(inputPath);
            return (int)mediaInfo.Duration.TotalSeconds;
        }
        catch
        {
            return 0;
        }
    }
    
    public async Task<List<string>> ConvertToHlsAsync(string inputPath, string outputFolder)
    {
        var outputFiles = new List<string>();
        var playlistPath = Path.Combine(outputFolder, "playlist.m3u8");
        
        try
        {
            // Check if FFmpeg is available
            if (!File.Exists("/usr/bin/ffmpeg") && !File.Exists("/usr/local/bin/ffmpeg"))
            {
                _logger.LogWarning("FFmpeg not found, creating placeholder HLS files");
                
                // Create a simple placeholder playlist for development
                var placeholderContent = @"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:10
#EXTINF:10.0,
segment0.ts
#EXT-X-ENDLIST
";
                await File.WriteAllTextAsync(playlistPath, placeholderContent);
                outputFiles.Add(playlistPath);
                
                // Create placeholder segment
                var segmentPath = Path.Combine(outputFolder, "segment0.ts");
                await File.WriteAllBytesAsync(segmentPath, new byte[1024]);
                outputFiles.Add(segmentPath);
                
                return outputFiles;
            }
            
            await Task.Run(() =>
            {
                // Convert to HLS with multiple qualities
                FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(playlistPath, true, options => options
                        .WithCustomArgument("-c:v libx264 -c:a aac -hls_time 10 -hls_list_size 0 -f hls")
                    )
                    .ProcessSynchronously();
            });
            
            // Collect all generated files
            outputFiles.AddRange(Directory.GetFiles(outputFolder, "*.m3u8"));
            outputFiles.AddRange(Directory.GetFiles(outputFolder, "*.ts"));
            
            return outputFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to HLS: {InputPath}", inputPath);
            
            // Create placeholder for development
            var placeholderContent = @"#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:10
#EXTINF:10.0,
segment0.ts
#EXT-X-ENDLIST
";
            await File.WriteAllTextAsync(playlistPath, placeholderContent);
            
            return new List<string> { playlistPath };
        }
    }
}
