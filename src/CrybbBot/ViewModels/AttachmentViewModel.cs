using System.IO;
using System.Windows.Media;

namespace CrybbBot.ViewModels
{
    public class AttachmentViewModel : ModelBase
    {
        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                RaisePropertyChanged();
                RaisePropertyChanged("Description");
            }
        }

        private string? _originalFilePath;
        public string? OriginalFilePath
        {
            get => _originalFilePath;
            set
            {
                _originalFilePath = value;
                RaisePropertyChanged();
            }
        }

        private ImageSource? _thumbnail;
        public ImageSource? Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                RaisePropertyChanged();
            }
        }

        private bool _showDescription;
        public bool ShowDescription
        {
            get => _showDescription;
            set
            {
                _showDescription = value;
                RaisePropertyChanged();
                RaisePropertyChanged("Description");
            }
        }

        private string? _description;
        public string? Description
        {
            get
            {
                if (!ShowDescription)
                {
                    return null;
                }
                var desc = _description ??
                    (string.IsNullOrWhiteSpace(FilePath)
                    ? null
                    : Path.GetFileName(FilePath));
                if (desc != null && desc.Length > 15)
                {
                    desc = desc.Substring(0, 15) + "...";
                }
                return desc;
            }
            set
            {
                _description = value;
                RaisePropertyChanged();
            }
        }

        private bool _isImage;
        public bool IsImage
        {
            get => _isImage;
            set
            {
                _isImage = value;
                RaisePropertyChanged();
            }
        }

        public byte[]? Blob
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FilePath) ||
                    !File.Exists(FilePath) ||
                    new FileInfo(FilePath).Length == 0)
                {
                    return null;
                }
                return File.ReadAllBytes(FilePath);
            }
        }
    }
}
