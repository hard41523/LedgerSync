using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using LedgerSyncModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;

namespace LedgerSyncViewModel
{
    public partial class AnalyzeViewModel : ObservableObject
    {
        public AnalyzeViewModel()
        {
            analyzeModels = new AnalyzeModel();
        }

        [ObservableProperty]
        private AnalyzeModel analyzeModels;

        // FIX: was async void with no await, built PlotModel but never saved it (lost in GC)
        // Now: loads real data from SQLite TradeList, populates both PlotModels in AnalyzeModel
        [RelayCommand]
        public void AnalyzeViewLoad()
        {
            try
            {
                var shellVM = Ioc.Default.GetService<ShellViewModel>();
                string dbPath = shellVM.SQLiteDBPath;

                // Key: date string "yyyy-MM-dd", Value: list of (symbol, qty)
                var tradesByDate = new Dictionary<string, List<(string Symbol, double Qty)>>();

                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string query = "SELECT Symbol, QTY, Time FROM TradeList ORDER BY Time ASC";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string timeStr = reader["Time"].ToString();
                            string symbol = reader["Symbol"].ToString();
                            double qty = 0;
                            double.TryParse(reader["QTY"].ToString(), out qty);

                            // Parse date from "yyyy-MM-dd HH:mm:ss"
                            if (DateTime.TryParse(timeStr, out DateTime dt))
                            {
                                string dateKey = dt.ToString("yyyy-MM-dd");
                                if (!tradesByDate.ContainsKey(dateKey))
                                    tradesByDate[dateKey] = new List<(string, double)>();
                                tradesByDate[dateKey].Add((symbol, qty));
                            }
                        }
                    }
                }

                if (tradesByDate.Count == 0)
                {
                    Debug.WriteLine("AnalyzeViewLoad: No trade data found in SQLite.");
                    return;
                }

                var sortedDates = tradesByDate.Keys.OrderBy(d => d).ToList();

                // --- Trading Frequency Chart (trades count per day) ---
                var freqModel = new PlotModel { Title = "Trading Frequency" };
                freqModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Date",
                    StringFormat = "MM-dd",
                    IntervalType = DateTimeIntervalType.Days,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    IsZoomEnabled = true,
                    IsPanEnabled = true
                });
                freqModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Trades Count",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    Minimum = 0
                });

                var freqSeries = new LineSeries
                {
                    Title = "Trades per Day",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerStroke = OxyColors.White,
                    Color = OxyColors.SteelBlue
                };

                foreach (var dateKey in sortedDates)
                {
                    if (DateTime.TryParse(dateKey, out DateTime dt))
                    {
                        int count = tradesByDate[dateKey].Count;
                        freqSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dt), count));
                    }
                }

                freqModel.Series.Add(freqSeries);
                AnalyzeModels.TradingFrequencyPlotModel = freqModel;

                // --- Trading Volume Chart (total QTY per day, one series per symbol) ---
                var volModel = new PlotModel { Title = "Trading Volume" };
                volModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Date",
                    StringFormat = "MM-dd",
                    IntervalType = DateTimeIntervalType.Days,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    IsZoomEnabled = true,
                    IsPanEnabled = true
                });
                volModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Volume (QTY)",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    Minimum = 0
                });

                // Group by symbol for separate series
                var allSymbols = tradesByDate.Values
                    .SelectMany(list => list.Select(t => t.Symbol))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                var palette = new[] {
                    OxyColors.SteelBlue, OxyColors.OrangeRed, OxyColors.SeaGreen,
                    OxyColors.Goldenrod, OxyColors.MediumPurple, OxyColors.Tomato
                };

                for (int si = 0; si < allSymbols.Count; si++)
                {
                    string sym = allSymbols[si];
                    var volSeries = new LineSeries
                    {
                        Title = sym,
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 3,
                        MarkerStroke = OxyColors.White,
                        Color = palette[si % palette.Length]
                    };

                    foreach (var dateKey in sortedDates)
                    {
                        if (DateTime.TryParse(dateKey, out DateTime dt))
                        {
                            double totalQty = tradesByDate[dateKey]
                                .Where(t => t.Symbol == sym)
                                .Sum(t => t.Qty);
                            if (totalQty > 0)
                                volSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dt), totalQty));
                        }
                    }

                    if (volSeries.Points.Count > 0)
                        volModel.Series.Add(volSeries);
                }

                AnalyzeModels.TradingVolumePlotModel = volModel;
                Debug.WriteLine($"AnalyzeViewLoad: loaded {tradesByDate.Count} days, {allSymbols.Count} symbols.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AnalyzeViewLoad error: {ex.Message}");
            }
        }
    }
}
