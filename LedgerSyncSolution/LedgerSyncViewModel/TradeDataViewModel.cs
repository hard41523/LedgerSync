using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using LedgerSyncModel;
using LedgerSyncModel.Entity;
using LedgerSyncModel.ResponseModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace LedgerSyncViewModel
{
    public partial class TradeDataViewModel : ObservableObject
    {
        public TradeDataViewModel()
        {
            tradeDataModels = new TradeDataModel();
            ListCoinEntity = new List<CoinEntity>();
            ListTradeListEntity = new List<TradeListEntity>();
        }

        List<CoinEntity> ListCoinEntity;
        List<TradeListEntity> ListTradeListEntity;
        FrameworkElement newelement;
        string newAsset;

        [ObservableProperty]
        private TradeDataModel tradeDataModels;

        // FIX: async void -> async Task so exceptions propagate correctly
        [RelayCommand]
        public async Task TradeDataViewLoad(FrameworkElement element)
        {
            if (Ioc.Default.GetService<ShellViewModel>().tradingAccountTrade == null)
            {
                return;
            }

            try
            {
                ListCoinEntity.Clear();
                TradeDataModels.ObservableCollectionCoinEntity.Clear();

                var result = await Ioc.Default.GetService<ShellViewModel>().tradingAccountTrade.AccountInformation(8000);
                if (result == null)
                {
                    return;
                }

                AccountInformationResponseModel accountInformationResponseModel = JsonConvert.DeserializeObject<AccountInformationResponseModel>(result);

                for (int i = 0; i < accountInformationResponseModel.balances.Length; i++)
                {
                    var free = System.Convert.ToDouble(accountInformationResponseModel.balances[i].free);
                    if (free > 0)
                    {
                        var name = accountInformationResponseModel.balances[i].asset;
                        CoinEntity coinEntity = new CoinEntity();
                        coinEntity.Asset = name;
                        coinEntity.Free = free.ToString();

                        var querycoinhave = ListCoinEntity.Where(x => x.Asset == coinEntity.Asset).ToList();
                        if (querycoinhave.Count == 0)
                        {
                            ListCoinEntity.Add(coinEntity);
                            TradeDataModels.ObservableCollectionCoinEntity.Add(coinEntity);
                            string ishave = Ioc.Default.GetService<ShellViewModel>().QueryCoin(coinEntity.Asset);
                            if (string.IsNullOrEmpty(ishave))
                            {
                                Ioc.Default.GetService<ShellViewModel>().InsertCoin(coinEntity.Free, coinEntity.Asset);
                            }
                        }
                    }
                }
                Ioc.Default.GetService<ShellViewModel>().ShellModels.ObservableCollectionCoinEntity = TradeDataModels.ObservableCollectionCoinEntity;

                var results = await Ioc.Default.GetService<ShellViewModel>().wallet.FundingWallet();
                if (results == null)
                {
                    return;
                }

                List<FundingWalletResponseModel> fundingWalletResponseModel = JsonConvert.DeserializeObject<List<FundingWalletResponseModel>>(results);

                for (int i = 0; i < fundingWalletResponseModel.Count; i++)
                {
                    var free = System.Convert.ToDouble(fundingWalletResponseModel[i].free);
                    var name = fundingWalletResponseModel[i].asset;
                    CoinEntity coinEntity = new CoinEntity();
                    coinEntity.Asset = name;
                    coinEntity.Free = free.ToString();

                    var querycoinhave = ListCoinEntity.Where(x => x.Asset == coinEntity.Asset).ToList();
                    if (querycoinhave.Count == 0)
                    {
                        ListCoinEntity.Add(coinEntity);
                        TradeDataModels.ObservableCollectionCoinEntity.Add(coinEntity);

                        string ishave = Ioc.Default.GetService<ShellViewModel>().QueryCoin(coinEntity.Asset);
                        if (string.IsNullOrEmpty(ishave))
                        {
                            Ioc.Default.GetService<ShellViewModel>().InsertCoin(coinEntity.Free, coinEntity.Asset);
                        }
                    }
                }
                Ioc.Default.GetService<ShellViewModel>().ShellModels.ObservableCollectionCoinEntity = TradeDataModels.ObservableCollectionCoinEntity;

                newelement = element;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TradeDataViewLoad error: {ex.Message}");
                MessageBox.Show($"Error loading trade data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Action

        // FIX: async void -> async Task so exceptions propagate correctly
        public async Task GetTradeListData(string Asset)
        {
            newAsset = Asset;
            ListTradeListEntity.Clear();
            TradeDataModels.ObservableCollectionTradeListEntity.Clear();
            string symbol = Asset + "USDT";

            try
            {
                var priceresult = await Ioc.Default.GetService<ShellViewModel>().tradingAccountTrade.AccountTradeList(symbol);
                if (priceresult == null)
                {
                    return;
                }

                List<AccountTradeListResponseModel> exchangeInformationResponseModel = JsonConvert.DeserializeObject<List<AccountTradeListResponseModel>>(priceresult);

                for (int m = 0; m < exchangeInformationResponseModel.Count; m++)
                {
                    TradeListEntity tradeListEntity = new TradeListEntity();
                    tradeListEntity.TradeListID = exchangeInformationResponseModel[m].id.ToString();
                    tradeListEntity.Symbol = exchangeInformationResponseModel[m].symbol;

                    // FIX: Store language-neutral "BUY"/"SELL" in DB instead of Chinese "买"/"卖"
                    // UI translation should be done via ValueConverter in XAML
                    tradeListEntity.IsBuyers = exchangeInformationResponseModel[m].isBuyer ? "BUY" : "SELL";

                    tradeListEntity.Price = exchangeInformationResponseModel[m].price;
                    tradeListEntity.QTY = exchangeInformationResponseModel[m].qty;
                    DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(exchangeInformationResponseModel[m].time).LocalDateTime;
                    tradeListEntity.Time = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    tradeListEntity.Year = dateTime.Year.ToString();
                    tradeListEntity.Month = dateTime.Month.ToString();

                    string ishave = Ioc.Default.GetService<ShellViewModel>().QueryTradeList(tradeListEntity.TradeListID);
                    if (string.IsNullOrEmpty(ishave))
                    {
                        Ioc.Default.GetService<ShellViewModel>().InsertTradeList(
                            tradeListEntity.TradeListID, tradeListEntity.Symbol, tradeListEntity.IsBuyers,
                            tradeListEntity.Price, tradeListEntity.QTY,
                            tradeListEntity.Year, tradeListEntity.Month, tradeListEntity.Time);
                    }

                    ListTradeListEntity.Add(tradeListEntity);
                    TradeDataModels.ObservableCollectionTradeListEntity.Add(tradeListEntity);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTradeListData error for {Asset}: {ex.Message}");
            }
            finally
            {
                Ioc.Default.GetService<ShellViewModel>().ShellModels.WaitingVisibility = Visibility.Collapsed;
            }
        }

        public void Print()
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                Size pageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                double scaleX = pageSize.Width / newelement.ActualWidth;
                double scaleY = pageSize.Height / newelement.ActualHeight;
                double scale = Math.Min(scaleX, scaleY);

                newelement.LayoutTransform = new ScaleTransform(scale, scale);
                newelement.Measure(pageSize);
                newelement.Arrange(new Rect(new Point(0, 0), pageSize));

                printDialog.PrintVisual(newelement, "Print " + newAsset);

                newelement.LayoutTransform = Transform.Identity;
            }
        }

        // FIX: Sequential await instead of fire-and-forget loop
        // Prevents Binance API rate limit storm when syncing many coins at once
        public async Task SyncData()
        {
            try
            {
                for (int m = 0; m < TradeDataModels.ObservableCollectionCoinEntity.Count; m++)
                {
                    await GetTradeListData(TradeDataModels.ObservableCollectionCoinEntity[m].Asset);
                }
            }
            finally
            {
                Ioc.Default.GetService<ShellViewModel>().ShellModels.WaitingVisibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}
