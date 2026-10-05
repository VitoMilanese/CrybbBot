using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using CrybbBot.Helpers;
using DataLayer;
using DataLayer.Enums;
using DataLayer.Interfaces;
using Newtonsoft.Json;
using static DataLayer.MockingData;

namespace CrybbBot.ViewModels
{
    public class DeveloperToolViewModel : ModelBase
    {
        private DbKind _dbKind { get; set; }
        private IDbConnectionFactory? _sqlite { get; set; }
        private IDbConnectionFactory? _mssql { get; set; }
        private DbConnectionFactory? DbConnectionFactory
        {
            get
            {
                switch (_dbKind)
                {
                    case DbKind.Sqlite:
                        {
                            if (_sqlite == null)
                            {
                                var oAssembly = Assembly.GetExecutingAssembly();
                                var root = Path.GetDirectoryName(oAssembly.Location)!;
                                var dbDir = Path.Combine(root, "DB");
                                Directory.CreateDirectory(dbDir);
                                var dbFilePath = Path.Combine(dbDir, SQLiteFileName);
                                _sqlite = new DbConnectionFactory(DbKind.Sqlite, $"Data Source={dbFilePath};");
                            }
                            return (DbConnectionFactory?)_sqlite;
                        }
                    case DbKind.SqlServer:
                        {
                            if (_mssql == null)
                            {
                                _mssql = new DbConnectionFactory(DbKind.SqlServer, MsSqlConnectionString);
                            }
                            return (DbConnectionFactory?)_mssql;
                        }
                    default:
                        return null;
                }
            }
            set
            {
                switch (_dbKind)
                {
                    case DbKind.Sqlite:
                        _sqlite = value;
                        break;
                    case DbKind.SqlServer:
                        _mssql = value;
                        break;
                }
                if (value == null)
                {
                    _dbKind = DbKind.None;
                }
            }
        }

        private string _sqliteFileName = "app.db";
        public string SQLiteFileName
        {
            get => _sqliteFileName;
            set
            {
                _sqliteFileName = value;
                RaisePropertyChanged();
            }
        }

        private string _msSqlInstanceName = @"localhost\SQLEXPRESS";
        public string MsSqlInstanceName
        {
            get => _msSqlInstanceName;
            set
            {
                _msSqlInstanceName = value;
                RaisePropertyChanged();
                RaisePropertyChanged("MsSqlConnectionString");
            }
        }

        private string _msSqlUsername = "sa";
        public string MsSqlUsername
        {
            get => _msSqlUsername;
            set
            {
                _msSqlUsername = value;
                RaisePropertyChanged();
                RaisePropertyChanged("MsSqlConnectionString");
            }
        }

        private string _msSqlPassword = "aepadmin";
        public string MsSqlPassword
        {
            get => _msSqlPassword;
            set
            {
                _msSqlPassword = value;
                RaisePropertyChanged();
                RaisePropertyChanged("MsSqlConnectionString");
            }
        }

        private static string DatabaseName { get; } = "CrybbBot";
        public string MsSqlConnectionString => $"Server={MsSqlInstanceName};Database={DatabaseName};Database=master;User Id={MsSqlUsername};Password={MsSqlPassword};TrustServerCertificate=True;";

        public ICommand SqliteConnectCommand { get; }
        public ICommand MsSqlConnectCommand { get; }
        public ICommand LoadMockingDataCommand { get; }
        public ICommand SaveMockingDataCommand { get; }

        public DeveloperToolViewModel()
        {
            SqliteConnectCommand = new RelayCommand(SqliteConnect);
            MsSqlConnectCommand = new RelayCommand(MsSqlConnect);
            LoadMockingDataCommand = new RelayCommand(LoadMockingData);
            SaveMockingDataCommand = new RelayCommand(SaveMockingData);
        }

        private async void SqliteConnect()
        {
            try
            {
                await HeavyTaskManager.DoHeavyWorkAsync("Connecting to SQLite...", async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    _dbKind = DbKind.Sqlite;
                    DbConnectionFactory?.Create();
                    await SqliteBootstrap.EnsureDbAsync();
                    DbConnectionFactory = null;
                });
                await MainWindow.ShowDialog("Done", string.Empty);
            }
            catch (Exception ex)
            {
                DbConnectionFactory = null;
                await MainWindow.ShowDialog("Error", ex.Message, false);
            }
        }

        private async void MsSqlConnect()
        {
            try
            {
                await HeavyTaskManager.DoHeavyWorkAsync("Connecting to MS SQL...", async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    _dbKind = DbKind.SqlServer;
                    DbConnectionFactory?.Create();
                    await SqlServerBootstrap.EnsureDbAsync(MsSqlConnectionString);
                    DbConnectionFactory = null;
                });
                await MainWindow.ShowDialog("Done", string.Empty);
            }
            catch (Exception ex)
            {
                DbConnectionFactory = null;
                await MainWindow.ShowDialog("Error", ex.Message, false);
            }
        }

        private async void LoadMockingData()
        {
            try
            {
                var oAssembly = Assembly.GetExecutingAssembly();
                var root = Path.GetDirectoryName(oAssembly.Location)!;
                var saveFilePath = Path.Combine(root, "mock.json");

                if (!File.Exists(saveFilePath))
                {
                    await MainWindow.ShowDialog("Помилка", "Не знайдено ніяких збережень");
                    return;
                }

                await HeavyTaskManager.DoHeavyWorkAsync("Loading mocking data...", async () =>
                {
                    var serialized = await File.ReadAllTextAsync(saveFilePath);
                    var save = JsonConvert.DeserializeObject<SavedMockingData>(serialized);
                    if (save != null)
                    {
                        Load(save);
                        MainWindow.LoadChannels();
                    }
                });
            }
            catch (Exception ex)
            {
                await MainWindow.ShowDialog("Error", ex.Message, false);
            }
        }

        private async void SaveMockingData()
        {
            try
            {
                var oAssembly = Assembly.GetExecutingAssembly();
                var root = Path.GetDirectoryName(oAssembly.Location)!;
                var saveFilePath = Path.Combine(root, "mock.json");

                await HeavyTaskManager.DoHeavyWorkAsync("Saving mocking data...", async () =>
                {
                    var serialized = GetSerialized();
                    await File.WriteAllTextAsync(saveFilePath, serialized);
                });
            }
            catch (Exception ex)
            {
                await MainWindow.ShowDialog("Error", ex.Message, false);
            }
        }
    }
}
