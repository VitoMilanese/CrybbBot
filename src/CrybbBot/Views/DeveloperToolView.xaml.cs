using System;
using System.Windows.Controls;
using CrybbBot.ViewModels;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for DeveloperToolView.xaml
    /// </summary>
    public partial class DeveloperToolView : UserControl
    {
        private DeveloperToolViewModel _vm => (DeveloperToolViewModel)Resources["ViewModel"];

        public EventHandler? CancelClicked { get; set; }
        public EventHandler<Models.Settings>? SaveClicked { get; set; }
        
        public DeveloperToolView()
        {
            InitializeComponent();
        }
    }
}
