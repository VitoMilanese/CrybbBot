using System;
using System.Threading.Tasks;
using CrybbBot.ViewModels;

namespace CrybbBot.Helpers
{
    internal static class HeavyTaskManager
    {
        private static MainWindowViewModel? MainWindowViewModel { get; set; }

        public static bool HasMainWindowViewModelSet => MainWindowViewModel != null;

        public static void SetMainWindowViewModel(MainWindowViewModel mainWindowViewModel) => MainWindowViewModel = mainWindowViewModel;

        public static async Task DoHeavyWorkAsync(string busyText, Func<Task> work)
        {
            if (MainWindowViewModel == null)
            {
                throw new MainWindowViewModelNotSetException();
            }

            try
            {
                MainWindowViewModel.IsBusy = true;
                MainWindowViewModel.BusyText = busyText;

                await work();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                MainWindowViewModel.IsBusy = false;
                MainWindowViewModel.BusyText = null;
            }
        }

        public static async Task<T> DoHeavyWorkAsync<T>(string busyText, Task<T> task)
        {
            if (MainWindowViewModel == null)
            {
                throw new MainWindowViewModelNotSetException();
            }

            try
            {
                MainWindowViewModel.IsBusy = true;
                MainWindowViewModel.BusyText = busyText;

                return await task;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                MainWindowViewModel.IsBusy = false;
                MainWindowViewModel.BusyText = null;
            }
        }

        public sealed class MainWindowViewModelNotSetException : Exception
        {
            public MainWindowViewModelNotSetException() : base("MainWindowViewModel not set") { }
        }
    }
}
