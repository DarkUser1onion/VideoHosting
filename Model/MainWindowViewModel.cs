using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace VideoHostingByWhoami.Model
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<VideoItem> _allVideos = new();
        private ObservableCollection<VideoItem> _filteredVideos = new();
        private string _searchText = string.Empty;

        public ObservableCollection<VideoItem> Videos
        {
            get => _filteredVideos;
            set
            {
                _filteredVideos = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterVideos();
                }
            }
        }

        public MainWindowViewModel()
        {
            LoadAllVideos();
            FilterVideos();
        }

        private void LoadAllVideos()
        {
            _allVideos.Clear();
            string videoFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos");

            if (!Directory.Exists(videoFolder))
            {
                _allVideos.Add(new VideoItem { Title = "Папка Videos не найдена", FilePath = videoFolder });
                return;
            }

            var files = Directory.GetFiles(videoFolder, "*.mp4", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                _allVideos.Add(new VideoItem
                {
                    Title = Path.GetFileNameWithoutExtension(file),
                    FilePath = file
                });
            }
        }

        private void FilterVideos()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Videos = new ObservableCollection<VideoItem>(_allVideos);
            }
            else
            {
                var filtered = _allVideos.Where(v =>
                    v.Title != null && v.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                Videos = new ObservableCollection<VideoItem>(filtered);
            }
        }

        public void OpenVideo(VideoItem video)
        {
            if (video == null || string.IsNullOrEmpty(video.FilePath)) return;

            var player = new Views.PlayerWindow();
            player.Show();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}