using System.Windows;
using System.Windows.Controls;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for ButtonsPannel.xaml
    /// </summary>
    public partial class ButtonsPannel : UserControl
    {
        public ButtonsPannel()
        {
            InitializeComponent();
        }

        public void SetPannel(Grid grid, object dataContext, bool setVisible = true)
        {
            btnPnl = grid;
            DataContext = dataContext;
            btnPnl.DataContext = dataContext;

            if (grid.Parent is Panel oldPanel)
            {
                oldPanel.Children.Remove(grid);
            }
            else if (grid.Parent is ContentControl oldContent)
            {
                oldContent.Content = null;
            }

            Content = grid;

            if (setVisible)
            {
                grid.Visibility = Visibility.Visible;
            }
        }

        public void Free()
        {
            if (btnPnl != null)
            {
                btnPnl.Visibility = Visibility.Collapsed;
            }
            btnPnl = null;
        }
    }
}
