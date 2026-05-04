using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using VideoHostingByWhoami.Services;

namespace VideoHostingByWhoami.Model;

public class UploadViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    private readonly Window _window;
    
    public ObservableCollection<string> Categories { get; } = new()
    {
        "education", "entertainment", "technology", "music", "sport", "news", "other"
    };
    
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanUpload)); }
    }
    
    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }
    
    private string _selectedCategory = string.Empty;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set { _selectedCategory = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanUpload)); }
    }
    
    private string _tagsText = string.Empty;
    public string TagsText
    {
        get => _tagsText;
        set { _tagsText = value; OnPropertyChanged(); }
    }
    
    private string _selectedFilePath = string.Empty;
    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set { _selectedFilePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFile)); OnPropertyChanged(nameof(SelectedFileName)); OnPropertyChanged(nameof(CanUpload)); }
    }
    
    public string SelectedFileName => System.IO.Path.GetFileName(SelectedFilePath);
    public bool HasFile => !string.IsNullOrEmpty(SelectedFilePath);
    
    public bool CanUpload => !string.IsNullOrWhiteSpace(Title) && 
                              !string.IsNullOrWhiteSpace(SelectedCategory) && 
                              HasFile && !IsUploading;
    
    private bool _isUploading;
    public bool IsUploading
    {
        get => _isUploading;
        set { _isUploading = value; OnPropertyChanged(); }
    }
    
    private int _uploadProgress;
    public int UploadProgress
    {
        get => _uploadProgress;
        set { _uploadProgress = value; OnPropertyChanged(); }
    }
    
    private string _error = string.Empty;
    public string Error
    {
        get => _error;
        set { _error = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }
    
    public bool HasError => !string.IsNullOrEmpty(Error);
    
    public SimpleCommand SelectFileCommand { get; }
    public SimpleCommand UploadCommand { get; }
    public SimpleCommand CancelCommand { get; }
    
    public UploadViewModel(IApiService api, Window window)
    {
        _api = api;
        _window = window;
        
        SelectFileCommand = new SimpleCommand(async () => await SelectFile());
        UploadCommand = new SimpleCommand(async () => await Upload(), () => CanUpload);
        CancelCommand = new SimpleCommand(() => _window.Close());
    }
    
    private async Task SelectFile()
    {
        var storage = _window.StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите видеофайл",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Video Files") { Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.webm" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            },
            AllowMultiple = false
        });
        
        if (files != null && files.Count > 0)
        {
            SelectedFilePath = files[0].Path.LocalPath;
        }
    }
    
    private async Task Upload()
    {
        if (!CanUpload) return;
        
        IsUploading = true;
        Error = "";
        UploadProgress = 0;
        
        var tags = TagsText.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();
        
        try
        {
            var success = await _api.UploadVideoAsync(Title, Description, SelectedCategory, tags, SelectedFilePath);
            
            if (success)
            {
                _window.Close();
            }
            else
            {
                Error = "Ошибка при загрузке видео";
            }
        }
        catch (Exception ex)
        {
            Error = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        UploadCommand.RaiseCanExecuteChanged();
    }
}
