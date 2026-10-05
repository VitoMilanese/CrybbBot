using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using CrybbBot.ViewModels;
using DataLayer;
using DataLayer.Models;

namespace CrybbBot.Views
{
    /// <summary>
    /// Interaction logic for AllMessagesView.xaml
    /// </summary>
    public partial class AllMessagesView : UserControl
    {
        private AllMessagesViewModel _vm => (AllMessagesViewModel)Resources["ViewModel"];
        
        public AllMessagesView()
        {
            InitializeComponent();

            DataContext = _vm;

            Load();
        }

        public void Load() => _vm.LoadFromDb();
    }
}
