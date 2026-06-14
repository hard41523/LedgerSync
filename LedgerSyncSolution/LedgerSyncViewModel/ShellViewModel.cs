using Binance.Spot;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using LedgerSyncModel;
using LedgerSyncModel.Entity;
using LedgerSyncViewModel.Helper;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LedgerSyncViewModel
{
    public partial class ShellViewModel : ObservableObject
    {
        public ShellViewModel()
        {
            shellModels = new ShellModel();

            for (int y = 2017; y <= DateTime.Now.Year; y++)
                ShellModels.ObservableCollectionYear.Add(y.ToString());

            ShellModels.ObservableCollectionLanguage.Add("zh-CN");
            ShellModels.ObservableCollectionLanguage.Add("en-US");
            ShellModels.ItemYear = ShellModels.ObservableCollectionYear[ShellModels.ObservableCollectionYear.Count - 1];

            GlobalTradeListEntities = new ObservableCollection<TradeListEntity>();
            ShellModels.CurrentPage = 1;
            ShellModels.TotalPage = 1;
            ShellModels.WaitingVisibility = Visibility.Collapsed;
        }

        public SpotAccountTrade tradingAccountTrade;
        public Wallet wallet;
        public ObservableCollection<TradeListEntity> GlobalTradeListEntities;
        public int pageNumber = 1;

        Window window;

        public string SQLiteDBPath = "LedgerSync.db";
        public string SQLiteDBCreateSecretKeySQL = "CREATE TABLE IF NOT EXISTS SecretKey (ID INTEGER PRIMARY KEY AUTOINCREMENT, ApiKey TEXT NOT NULL, ApiSecret TEXT NOT NULL)";
        public string SQLiteDBCreateTradeListSQL = "CREATE TABLE IF NOT EXISTS TradeList (ID INTEGER PRIMARY KEY AUTOINCREMENT, TradeListID VARCHAR(255) NOT NULL, Symbol VARCHAR(255) NOT NULL, IsBuyers VARCHAR(255) NOT NULL, Price VARCHAR(255) NOT NULL, QTY VARCHAR(255) NOT NULL, Year VARCHAR(255) NOT NULL, Month VARCHAR(255) NOT NULL, Time VARCHAR(255) NOT NULL)";
        public string SQLiteDBCreateCoinSQL = "CREATE TABLE IF NOT EXISTS Coin (ID INTEGER PRIMARY KEY AUTOINCREMENT, Free VARCHAR(255) NOT NULL, Asset VARCHAR(255) NOT NULL)";
        public string SQLiteDBInsertSecretKeySQL = "INSERT INTO SecretKey (ApiKey, ApiSecret) VALUES (@ApiKey, @ApiSecret)";
        public string query = "SELECT ID, ApiKey, ApiSecret FROM SecretKey ORDER BY ID DESC LIMIT 1";
        public string selectCoin;

        [ObservableProperty]
        private ShellModel shellModels;

        [RelayCommand]
        public void ShellViewLoad(FrameworkElement frameworkElement)
        {
            window = (Window)frameworkElement;
            window.MouseLeftButtonDown += delegate { window.DragMove(); };

            ShellModels.CoinVisibility = Visibility.Collapsed;
            ShellModels.SecretKeyVisibility = Visibility.Collapsed;
            ShellModels.NavigationContent = "UI/MenuView.xaml";
            ShellModels.NavigationSecretKey = "UI/MenuView.xaml";

            bool isNewDatabase = !File.Exists(SQLiteDBPath);
            if (isNewDatabase)
            {
                SQLiteConnection.CreateFile(SQLiteDBPath);
                Debug.WriteLine("LedgerSync.db created");
                CreateSecretKey();
                CreateTradeList();
                CreateCoin();
            }

            if (!isNewDatabase)
            {
                QuerySecretKey();
                tradingAccountTrade = new SpotAccountTrade(
                    apiKey: Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiKey,
                    apiSecret: Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiSecret);
                wallet = new Wallet(
                    apiKey: Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiKey,
                    apiSecret: Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiSecret);
            }

            ShellModels.ItemLanguage = ShellModels.ObservableCollectionLanguage[0];
            LanguageSelection();
            ShellModels.WaitingVisibility = Visibility.Collapsed;
        }

        [RelayCommand]
        public void SecretKey()
        {
            ShellModels.CoinVisibility = Visibility.Collapsed;
            ShellModels.SecretKeyVisibility = Visibility.Visible;
            ShellModels.NavigationContent = "UI/MenuView.xaml";
            ShellModels.NavigationSecretKey = "UI/SecretKeyView.xaml";
        }

        [RelayCommand]
        public void TradeData()
        {
            ShellModels.CoinVisibility = Visibility.Visible;
            ShellModels.SecretKeyVisibility = Visibility.Collapsed;
            ShellModels.NavigationContent = "UI/TradeDataView.xaml";
            ShellModels.NavigationSecretKey = "UI/MenuView.xaml";
        }

        [RelayCommand]
        public void SelectCoin(string content)
        {
            selectCoin = content;
            Ioc.Default.GetService<TradeDataViewModel>().GetTradeListData(content);
        }

        [RelayCommand]
        public void QueryTradeListSymbol(string Symbol)
        {
            string isHave = selectCoin;
            Symbol = Symbol + "USDT";

            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity.Clear();
            GlobalTradeListEntities.Clear();

            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT * FROM TradeList WHERE Symbol=@Symbol";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Symbol", Symbol);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            isHave = reader["ID"].ToString();
                            TradeListEntity tradeListEntity = new TradeListEntity();
                            tradeListEntity.TradeListID = reader["TradeListID"].ToString();
                            tradeListEntity.Symbol = reader["Symbol"].ToString();
                            tradeListEntity.IsBuyers = reader["IsBuyers"].ToString();
                            tradeListEntity.Price = reader["Price"].ToString();
                            tradeListEntity.QTY = reader["QTY"].ToString();
                            tradeListEntity.Year = reader["Year"].ToString();
                            tradeListEntity.Month = reader["Month"].ToString();
                            tradeListEntity.Time = reader["Time"].ToString();
                            GlobalTradeListEntities.Add(tradeListEntity);
                            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity.Add(tradeListEntity);
                            if (Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity.Count == 20)
                                Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity = new ObservableCollection<TradeListEntity>(GlobalTradeListEntities.Take(20));
                        }
                    }
                }
            }

            pageNumber = 1;
            int totalPages = (int)Math.Ceiling((double)GlobalTradeListEntities.Count / 20);
            ShellModels.TotalPage = totalPages;
            ShellModels.CurrentPage = pageNumber;
        }

        [RelayCommand]
        public void QueryTradeListMonth(string month)
        {
            if (!string.IsNullOrEmpty(selectCoin))
                QueryTradeListYearMonth(ShellModels.ItemYear, month, selectCoin);
        }

        // FIX: await SyncData + reset WaitingVisibility in finally
        [RelayCommand]
        public async void SyncDataLocal()
        {
            ShellModels.WaitingVisibility = Visibility.Visible;
            try
            {
                Ioc.Default.GetService<TradeDataViewModel>().Print();
                Ioc.Default.GetService<ShellViewModel>().ShellModels.ObservableCollectionCoinEntity =
                    Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionCoinEntity;
                await Ioc.Default.GetService<TradeDataViewModel>().SyncData();
            }
            finally
            {
                ShellModels.WaitingVisibility = Visibility.Collapsed;
            }
        }

        [RelayCommand]
        public void AnalyzeTradeList() { }

        [RelayCommand]
        public void Print()
        {
            Ioc.Default.GetService<TradeDataViewModel>().Print();
        }

        #region Pagination
        [RelayCommand]
        public void PreviousContent()
        {
            int totalPages = (int)Math.Ceiling((double)GlobalTradeListEntities.Count / 20);
            pageNumber--;
            if (pageNumber < 1) pageNumber = 1;
            var pagedData = GlobalTradeListEntities.Skip((pageNumber - 1) * 20).Take(20).ToList();
            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity.Clear();
            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity = new ObservableCollection<TradeListEntity>(pagedData);
            ShellModels.CurrentPage = pageNumber;
            ShellModels.TotalPage = totalPages;
        }

        [RelayCommand]
        public void NextContent()
        {
            int totalPages = (int)Math.Ceiling((double)GlobalTradeListEntities.Count / 20);
            pageNumber++;
            if (pageNumber > totalPages) pageNumber--;
            var pagedData = GlobalTradeListEntities.Skip((pageNumber - 1) * 20).Take(20).ToList();
            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity.Clear();
            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity = new ObservableCollection<TradeListEntity>(pagedData);
            ShellModels.CurrentPage = pageNumber;
            ShellModels.TotalPage = totalPages;
        }
        #endregion

        #region Window controls
        [RelayCommand]
        public void MiniSystem() => ShellModels.SystemState = WindowState.Minimized;

        [RelayCommand]
        public void MaxSystem()
        {
            if (ShellModels.SystemState != WindowState.Maximized)
            {
                ShellModels.SystemState = WindowState.Maximized;
                ShellModels.MaxOrNormal = "\uEF2F";
            }
            else
            {
                ShellModels.SystemState = WindowState.Normal;
                ShellModels.MaxOrNormal = "\uEF2E";
            }
        }

        [RelayCommand]
        public void ExitSystem() => Environment.Exit(0);
        #endregion

        [RelayCommand]
        public void LanguageSelection()
        {
            if (ShellModels.ItemLanguage == "zh-CN")
            {
                ChangeLanguage("zh-CN");
                SwitchLanguage("zh-CN");
            }
            else
            {
                ChangeLanguage("en-US");
                SwitchLanguage("en-US");
            }
        }

        #region Action
        public void ChangeLanguage(string culture)
        {
            string dictPath = $"LedgerSync;component/Resources/Strings.{culture}.xaml";
            var dict = new ResourceDictionary { Source = new Uri(dictPath, UriKind.Relative) };
            var oldDict = Application.Current.Resources.MergedDictionaries[0];
            Application.Current.Resources.MergedDictionaries.Remove(oldDict);
            Application.Current.Resources.MergedDictionaries.Insert(0, dict);
        }

        public void SwitchLanguage(string culture)
        {
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
                dictionaryList.Add(dictionary);

            string requestedCulture = culture == "en-US"
                ? "LedgerSync;component/Resources/Strings.en-US.xaml"
                : "LedgerSync;component/Resources/Strings.zh-CN.xaml";

            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);

            LocalizationManager.ChangeCulture(culture);
        }

        // FIX: removed leftover test code (INSERT INTO Users / SELECT FROM Users)
        // that caused runtime crash on first launch with a fresh database
        public void CreateSecretKey()
        {
            using var db = new SQLiteHelper(SQLiteDBPath);
            db.ExecuteNonQuery(SQLiteDBCreateSecretKeySQL);
        }

        public void CreateTradeList()
        {
            using var db = new SQLiteHelper(SQLiteDBPath);
            db.ExecuteNonQuery(SQLiteDBCreateTradeListSQL);
        }

        public void CreateCoin()
        {
            using var db = new SQLiteHelper(SQLiteDBPath);
            db.ExecuteNonQuery(SQLiteDBCreateCoinSQL);
        }

        /// <summary>
        /// FIX: DELETE existing row first (upsert pattern) to prevent unbounded table growth.
        /// Keys are encrypted with DPAPI before storing.
        /// </summary>
        public void InsertSecretKey()
        {
            string apiKey    = CryptoHelper.Encrypt(Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiKey);
            string apiSecret = CryptoHelper.Encrypt(Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiSecret);

            using (var connection = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                connection.Open();
                // Delete old row so only one row ever exists
                using (var del = new SQLiteCommand("DELETE FROM SecretKey", connection))
                    del.ExecuteNonQuery();

                using (var cmd = new SQLiteCommand(SQLiteDBInsertSecretKeySQL, connection))
                {
                    cmd.Parameters.AddWithValue("@ApiKey", apiKey);
                    cmd.Parameters.AddWithValue("@ApiSecret", apiSecret);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Debug.WriteLine($"InsertSecretKey rows: {rowsAffected}");
                }
            }
        }

        /// <summary>
        /// Reads encrypted API keys and decrypts with DPAPI before use.
        /// </summary>
        public void QuerySecretKey()
        {
            using (var connection = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string apiKey    = CryptoHelper.Decrypt(reader.GetString(1));
                        string apiSecret = CryptoHelper.Decrypt(reader.GetString(2));
                        Debug.WriteLine($"QuerySecretKey ID: {id}");
                        Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiKey    = apiKey;
                        Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiSecret = apiSecret;
                    }
                    else
                    {
                        Debug.WriteLine("No SecretKey row found.");
                    }
                }
            }
        }
        #endregion

        public void InsertTradeList(string TradeListID, string Symbol, string IsBuyers, string Price, string QTY, string Year, string Month, string Time)
        {
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                conn.Open();
                string insertQuery = "INSERT INTO TradeList (TradeListID, Symbol, IsBuyers, Price, QTY, Year, Month, Time) VALUES (@TradeListID, @Symbol, @IsBuyers, @Price, @QTY, @Year, @Month, @Time)";
                using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@TradeListID", TradeListID);
                    cmd.Parameters.AddWithValue("@Symbol", Symbol);
                    cmd.Parameters.AddWithValue("@IsBuyers", IsBuyers);
                    cmd.Parameters.AddWithValue("@Price", Price);
                    cmd.Parameters.AddWithValue("@QTY", QTY);
                    cmd.Parameters.AddWithValue("@Year", Year);
                    cmd.Parameters.AddWithValue("@Month", Month);
                    cmd.Parameters.AddWithValue("@Time", Time);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Debug.WriteLine($"TradeList:{rowsAffected}");
                }
            }
        }

        public string QueryTradeList(string TradeListID)
        {
            string isHave = "";
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT * FROM TradeList WHERE TradeListID=@TradeListID";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TradeListID", TradeListID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                        while (reader.Read())
                            isHave = reader["ID"].ToString();
                }
            }
            return isHave;
        }

        public void QueryTradeListYearMonth(string Year, string Month, string Symbol)
        {
            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity.Clear();
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT * FROM TradeList WHERE Year=@Year AND Month=@Month AND Symbol=@Symbol";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Year", Year);
                    cmd.Parameters.AddWithValue("@Month", Month);
                    cmd.Parameters.AddWithValue("@Symbol", Symbol);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TradeListEntity tradeListEntity = new TradeListEntity();
                            tradeListEntity.TradeListID = reader["TradeListID"].ToString();
                            tradeListEntity.Symbol      = reader["Symbol"].ToString();
                            tradeListEntity.IsBuyers    = reader["IsBuyers"].ToString();
                            tradeListEntity.Price       = reader["Price"].ToString();
                            tradeListEntity.QTY         = reader["QTY"].ToString();
                            tradeListEntity.Year        = reader["Year"].ToString();
                            tradeListEntity.Month       = reader["Month"].ToString();
                            tradeListEntity.Time        = reader["Time"].ToString();
                            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity.Add(tradeListEntity);
                        }
                    }
                }
            }
        }

        public void InsertCoin(string Free, string Asset)
        {
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                conn.Open();
                string insertQuery = "INSERT INTO Coin (Free, Asset) VALUES (@Free, @Asset)";
                using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Free", Free);
                    cmd.Parameters.AddWithValue("@Asset", Asset);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Debug.WriteLine($"Coin:{rowsAffected}");
                }
            }
        }

        public string QueryCoin(string Asset)
        {
            string isHave = "";
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={SQLiteDBPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT * FROM Coin WHERE Asset=@Asset";
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Asset", Asset);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                        while (reader.Read())
                            isHave = reader["ID"].ToString();
                }
            }
            return isHave;
        }
    }
}
