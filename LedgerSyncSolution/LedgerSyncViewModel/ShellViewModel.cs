using Binance.Spot;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using LedgerSyncModel;
using LedgerSyncModel.Entity;
using LedgerSyncViewModel.Helper;
using LedgerSyncViewModel.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LedgerSyncViewModel
{
    public partial class ShellViewModel : ObservableObject
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        public readonly DatabaseService Db = new DatabaseService();

        public SpotAccountTrade tradingAccountTrade;
        public Wallet wallet;

        // ── State ─────────────────────────────────────────────────────────────
        public ObservableCollection<TradeListEntity> GlobalTradeListEntities;
        public int pageNumber = 1;
        public string SQLiteDBPath = "LedgerSync.db";
        public string selectCoin;

        private Window _window;

        [ObservableProperty]
        private ShellModel shellModels;

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

        // ── Startup ───────────────────────────────────────────────────────────

        [RelayCommand]
        public void ShellViewLoad(FrameworkElement frameworkElement)
        {
            _window = (Window)frameworkElement;
            _window.MouseLeftButtonDown += delegate { _window.DragMove(); };

            ShellModels.CoinVisibility      = Visibility.Collapsed;
            ShellModels.SecretKeyVisibility = Visibility.Collapsed;
            ShellModels.NavigationContent   = "UI/MenuView.xaml";
            ShellModels.NavigationSecretKey = "UI/MenuView.xaml";

            bool isNewDatabase = !File.Exists(SQLiteDBPath);
            if (isNewDatabase)
            {
                System.Data.SQLite.SQLiteConnection.CreateFile(SQLiteDBPath);
                Debug.WriteLine("LedgerSync.db created");
                Db.CreateTables();
            }
            else
            {
                var (apiKey, apiSecret) = Db.QuerySecretKey();
                if (apiKey != null)
                {
                    Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiKey    = apiKey;
                    Ioc.Default.GetService<SecretKeyViewModel>().SecretKeyModels.ApiSecret = apiSecret;
                    tradingAccountTrade = new SpotAccountTrade(apiKey: apiKey, apiSecret: apiSecret);
                    wallet              = new Wallet(apiKey: apiKey, apiSecret: apiSecret);
                }
            }

            ShellModels.ItemLanguage = ShellModels.ObservableCollectionLanguage[0];
            LanguageSelection();
            ShellModels.WaitingVisibility = Visibility.Collapsed;
        }

        // ── Navigation ────────────────────────────────────────────────────────

        [RelayCommand]
        public void SecretKey()
        {
            ShellModels.CoinVisibility      = Visibility.Collapsed;
            ShellModels.SecretKeyVisibility = Visibility.Visible;
            ShellModels.NavigationContent   = "UI/MenuView.xaml";
            ShellModels.NavigationSecretKey = "UI/SecretKeyView.xaml";
        }

        [RelayCommand]
        public void TradeData()
        {
            ShellModels.CoinVisibility      = Visibility.Visible;
            ShellModels.SecretKeyVisibility = Visibility.Collapsed;
            ShellModels.NavigationContent   = "UI/TradeDataView.xaml";
            ShellModels.NavigationSecretKey = "UI/MenuView.xaml";
        }

        // ── SecretKey (delegates to DatabaseService) ──────────────────────────

        public void InsertSecretKey()
        {
            var vm = Ioc.Default.GetService<SecretKeyViewModel>();
            Db.InsertSecretKey(vm.SecretKeyModels.ApiKey, vm.SecretKeyModels.ApiSecret);
        }

        public void QuerySecretKey()
        {
            var (apiKey, apiSecret) = Db.QuerySecretKey();
            if (apiKey == null) return;
            var vm = Ioc.Default.GetService<SecretKeyViewModel>();
            vm.SecretKeyModels.ApiKey    = apiKey;
            vm.SecretKeyModels.ApiSecret = apiSecret;
        }

        // ── TradeList (delegates to DatabaseService) ──────────────────────────

        [RelayCommand]
        public void SelectCoin(string content)
        {
            selectCoin = content;
            Ioc.Default.GetService<TradeDataViewModel>().GetTradeListData(content);
        }

        [RelayCommand]
        public void QueryTradeListSymbol(string symbol)
        {
            symbol = symbol + "USDT";
            GlobalTradeListEntities = Db.QueryTradeListBySymbol(symbol);

            var tdVm = Ioc.Default.GetService<TradeDataViewModel>();
            tdVm.TradeDataModels.ObservableCollectionTradeListEntity =
                new ObservableCollection<TradeListEntity>(GlobalTradeListEntities.Take(20));

            pageNumber = 1;
            int totalPages = (int)Math.Ceiling((double)GlobalTradeListEntities.Count / 20);
            ShellModels.TotalPage   = totalPages;
            ShellModels.CurrentPage = pageNumber;
        }

        [RelayCommand]
        public void QueryTradeListMonth(string month)
        {
            if (!string.IsNullOrEmpty(selectCoin))
            {
                var entities = Db.QueryTradeListByYearMonth(ShellModels.ItemYear, month, selectCoin);
                Ioc.Default.GetService<TradeDataViewModel>()
                    .TradeDataModels.ObservableCollectionTradeListEntity = entities;
            }
        }

        public void InsertTradeList(string tradeListID, string symbol, string isBuyers,
            string price, string qty, string year, string month, string time)
            => Db.InsertTradeList(tradeListID, symbol, isBuyers, price, qty, year, month, time);

        public string QueryTradeList(string tradeListID)
            => Db.QueryTradeList(tradeListID);

        public void InsertCoin(string free, string asset)
            => Db.InsertCoin(free, asset);

        public string QueryCoin(string asset)
            => Db.QueryCoin(asset);

        // ── Sync ──────────────────────────────────────────────────────────────

        [RelayCommand]
        public async void SyncDataLocal()
        {
            ShellModels.WaitingVisibility = Visibility.Visible;
            try
            {
                Ioc.Default.GetService<TradeDataViewModel>().Print();
                ShellModels.ObservableCollectionCoinEntity =
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
        public void Print() => Ioc.Default.GetService<TradeDataViewModel>().Print();

        // ── Pagination ────────────────────────────────────────────────────────

        [RelayCommand]
        public void PreviousContent()
        {
            int totalPages = (int)Math.Ceiling((double)GlobalTradeListEntities.Count / 20);
            pageNumber--;
            if (pageNumber < 1) pageNumber = 1;
            ApplyPage(totalPages);
        }

        [RelayCommand]
        public void NextContent()
        {
            int totalPages = (int)Math.Ceiling((double)GlobalTradeListEntities.Count / 20);
            pageNumber++;
            if (pageNumber > totalPages) pageNumber--;
            ApplyPage(totalPages);
        }

        private void ApplyPage(int totalPages)
        {
            var paged = GlobalTradeListEntities.Skip((pageNumber - 1) * 20).Take(20).ToList();
            Ioc.Default.GetService<TradeDataViewModel>().TradeDataModels.ObservableCollectionTradeListEntity
                = new ObservableCollection<TradeListEntity>(paged);
            ShellModels.CurrentPage = pageNumber;
            ShellModels.TotalPage   = totalPages;
        }

        // ── Window controls ───────────────────────────────────────────────────

        [RelayCommand] public void MiniSystem() => ShellModels.SystemState = WindowState.Minimized;

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

        [RelayCommand] public void ExitSystem() => Environment.Exit(0);

        // ── Language ──────────────────────────────────────────────────────────

        [RelayCommand]
        public void LanguageSelection()
        {
            string culture = ShellModels.ItemLanguage == "zh-CN" ? "zh-CN" : "en-US";
            ChangeLanguage(culture);
            SwitchLanguage(culture);
        }

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
            var dicts = Application.Current.Resources.MergedDictionaries.ToList();
            string requested = culture == "en-US"
                ? "LedgerSync;component/Resources/Strings.en-US.xaml"
                : "LedgerSync;component/Resources/Strings.zh-CN.xaml";

            var dict = dicts.FirstOrDefault(d => d.Source.OriginalString.Equals(requested));
            Application.Current.Resources.MergedDictionaries.Remove(dict);
            Application.Current.Resources.MergedDictionaries.Add(dict);
            LocalizationManager.ChangeCulture(culture);
        }
    }
}
