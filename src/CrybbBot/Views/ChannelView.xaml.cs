using System.Windows.Controls;
using CrybbBot.ViewModels;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for ChannelView.xaml
    /// </summary>
    public partial class ChannelView : UserControl
    {
        private ChannelViewModel _vm => (ChannelViewModel)Resources["ViewModel"];
        
        public ChannelView()
        {
            InitializeComponent();
        }
    }
}
