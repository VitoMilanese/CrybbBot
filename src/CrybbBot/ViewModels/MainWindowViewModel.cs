using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CrybbBot.ViewModels
{
    public class MainWindowViewModel : ModelBase
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                RaisePropertyChanged();
            }
        }

        private string? _busyText;
        public string? BusyText
        {
            get => _busyText;
            set { _busyText = value; RaisePropertyChanged(); }
        }

        private int _transitionerSelectedSlide = 0;
        public int TransitionerSelectedSlide
        {
            get => _transitionerSelectedSlide;
            set
            {
                _transitionerSelectedSlide = value;
                RaisePropertyChanged();
                RaisePropertyChanged("Background");
                RaisePropertyChanged("MainMenuShortcutVisibility");
            }
        }

        private double _windowWidth;
        public double WindowWidth
        {
            get => _windowWidth;
            set
            {
                _windowWidth = value;
                RaisePropertyChanged();
            }
        }

        private double _windowHeight;
        public double WindowHeight
        {
            get => _windowHeight;
            set
            {
                _windowHeight = value;
                RaisePropertyChanged();
            }
        }

        private FrameworkElement? _registeredDialog { get; set; }

        public void RegisterDialog(FrameworkElement dialog)
        {
            _registeredDialog = dialog;
            dialog.IsEnabled = !IsBusy;

            PropertyChanged += DialogPropertyChanged;
        }

        public void UnregisterDialog()
        {
            _registeredDialog = null;
            PropertyChanged -= DialogPropertyChanged;
        }

        private void DialogPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_registeredDialog != null && e.PropertyName == nameof(IsBusy))
            {
                _registeredDialog.IsEnabled = !IsBusy;
            }
        }

        public Visibility MainMenuShortcutVisibility
        {
            get
            {
                var hidden = new[] { 0, 4 };
                return hidden.Contains(TransitionerSelectedSlide) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public BitmapImage? _background;
        public BitmapImage Background
        {
            get
            {
                if (_background != null)
                {
                    return _background;
                }

                var relativePath = "Resources/bg.png";
                //var relativePath = TransitionerSelectedSlide == 0
                //    ? "Resources/bg_main.png"
                //    : "Resources/bg.png";

                var uri = new Uri(
                    $"pack://application:,,,/{relativePath}",
                    UriKind.Absolute);

                _background = new BitmapImage(uri);
                //return new BitmapImage(uri);
                return _background;
            }
        }
    }
}
