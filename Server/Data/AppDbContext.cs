using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistVideo> PlaylistVideos => Set<PlaylistVideo>();
    public DbSet<Moderation> Moderations => Set<Moderation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Login).IsUnique();
            entity.Property(e => e.Login).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
        });
        
        // Video configuration
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FilePath).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Tags).HasColumnType("text[]");
            
            entity.HasOne(e => e.Author)
                .WithMany(u => u.Videos)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Comment configuration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            
            entity.HasOne(e => e.Video)
                .WithMany(v => v.Comments)
                .HasForeignKey(e => e.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Like configuration
        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Video)
                .WithMany(v => v.Likes)
                .HasForeignKey(e => e.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.VideoId, e.UserId }).IsUnique();
        });
        
        // Playlist configuration
        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Playlists)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PlaylistVideo configuration
        modelBuilder.Entity<PlaylistVideo>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Playlist)
                .WithMany(p => p.PlaylistVideos)
                .HasForeignKey(e => e.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Video)
                .WithMany()
                .HasForeignKey(e => e.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Moderation configuration
        modelBuilder.Entity<Moderation>(entity =>
        {
            entity.HasKey(e => e.VideoId);
            
            entity.HasOne(e => e.Video)
                .WithOne(v => v.Moderation)
                .HasForeignKey<Moderation>(e => e.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Moderator)
                .WithMany()
                .HasForeignKey(e => e.ModeratorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Message).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
