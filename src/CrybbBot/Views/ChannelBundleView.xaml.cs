using System.Windows.Controls;
using CrybbBot.ViewModels;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for ChannelBundleView.xaml
    /// </summary>
    public partial class ChannelBundleView : UserControl
    {
        private ChannelBundleViewModel _vm => (ChannelBundleViewModel)Resources["ViewModel"];
        
        public ChannelBundleView()
        {
            InitializeComponent();
        }
    }
}
