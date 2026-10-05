using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CrybbBot.ViewModels;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for AttachmentView.xaml
    /// </summary>
    public partial class AttachmentView : UserControl
    {
        private AttachmentViewModel _vm => (AttachmentViewModel)Resources["ViewModel"];
        
        public EventHandler? DeleteClicked { get; set; }

        public string? FilePath => _vm.FilePath;
        public string? OriginalFilePath => _vm.OriginalFilePath;
        public bool IsImage => _vm.IsImage;

        public AttachmentView()
        {
            InitializeComponent();
        }

        public AttachmentView(string filePath, bool showDescription = true)
        {
            InitializeComponent();
            Init(filePath, null, showDescription);
        }

        public AttachmentView(string filePath, string? originalFilePath, bool showDescription = true)
        {
            InitializeComponent();
            Init(filePath, originalFilePath, showDescription);
        }

        private void Init(string filePath, string? originalFilePath, bool showDescription = true)
        {
            _vm.ShowDescription = showDescription;
            _vm.FilePath = filePath;
            _vm.OriginalFilePath = originalFilePath ?? filePath;

            try
            {
                var bitmap = TryLoadBitmapThumbnail(filePath);
                _vm.Thumbnail = bitmap;
                _vm.IsImage = true;
            }
            catch
            {
                var relativePath = "Resources/file_thumbnail.png";

                var uri = new Uri(
                    $"pack://application:,,,/{relativePath}",
                    UriKind.Absolute);

                _vm.Thumbnail = new BitmapImage(uri);
                _vm.IsImage = false;
            }
        }

        public static BitmapSource? TryLoadBitmapThumbnail(string filePath)
        {
            try
            {
                // Open with share flags so we don't block delete/rename even while reading
                using var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                // Copy to memory so decoding never touches the file
                using var ms = new MemoryStream();
                fs.CopyTo(ms);
                ms.Position = 0;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;          // <-- key
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                // not an image / failed decode
                throw;
            }
        }

        private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e) => DeleteClicked?.Invoke(this, e);

        public void UpdateFilePath(string filePath) => _vm.OriginalFilePath = filePath;
    }
}
