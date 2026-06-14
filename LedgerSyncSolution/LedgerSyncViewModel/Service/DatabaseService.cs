using CommunityToolkit.Mvvm.DependencyInjection;
using LedgerSyncModel.Entity;
using LedgerSyncViewModel.Helper;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace LedgerSyncViewModel.Service
{
    /// <summary>
    /// Handles all SQLite database operations for LedgerSync.
    /// Extracted from ShellViewModel to follow Single Responsibility Principle.
    /// </summary>
    public class DatabaseService
    {
        private string DbPath => Ioc.Default.GetService<ShellViewModel>().SQLiteDBPath;

        private const string CreateSecretKeySQL =
            "CREATE TABLE IF NOT EXISTS SecretKey (ID INTEGER PRIMARY KEY AUTOINCREMENT, ApiKey TEXT NOT NULL, ApiSecret TEXT NOT NULL)";
        private const string CreateTradeListSQL =
            "CREATE TABLE IF NOT EXISTS TradeList (ID INTEGER PRIMARY KEY AUTOINCREMENT, TradeListID VARCHAR(255) NOT NULL, Symbol VARCHAR(255) NOT NULL, IsBuyers VARCHAR(255) NOT NULL, Price VARCHAR(255) NOT NULL, QTY VARCHAR(255) NOT NULL, Year VARCHAR(255) NOT NULL, Month VARCHAR(255) NOT NULL, Time VARCHAR(255) NOT NULL)";
        private const string CreateCoinSQL =
            "CREATE TABLE IF NOT EXISTS Coin (ID INTEGER PRIMARY KEY AUTOINCREMENT, Free VARCHAR(255) NOT NULL, Asset VARCHAR(255) NOT NULL)";

        // ── Schema ────────────────────────────────────────────────────────────

        public void CreateTables()
        {
            using var db = new SQLiteHelper(DbPath);
            db.ExecuteNonQuery(CreateSecretKeySQL);
            db.ExecuteNonQuery(CreateTradeListSQL);
            db.ExecuteNonQuery(CreateCoinSQL);
        }

        // ── SecretKey ─────────────────────────────────────────────────────────

        /// <summary>
        /// Upsert: deletes existing row then inserts new one (table always has max 1 row).
        /// Keys are encrypted with DPAPI before storing.
        /// </summary>
        public void InsertSecretKey(string apiKey, string apiSecret)
        {
            string encKey    = CryptoHelper.Encrypt(apiKey);
            string encSecret = CryptoHelper.Encrypt(apiSecret);

            using var db = new SQLiteHelper(DbPath);
            db.ExecuteNonQuery("DELETE FROM SecretKey");
            db.ExecuteNonQuery(
                "INSERT INTO SecretKey (ApiKey, ApiSecret) VALUES (@ApiKey, @ApiSecret)",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "@ApiKey",    encKey    },
                    { "@ApiSecret", encSecret }
                });

            Debug.WriteLine("InsertSecretKey: saved.");
        }

        /// <summary>
        /// Returns decrypted (apiKey, apiSecret) or (null, null) if no row found.
        /// Falls back gracefully for legacy unencrypted rows.
        /// </summary>
        public (string ApiKey, string ApiSecret) QuerySecretKey()
        {
            using var db = new SQLiteHelper(DbPath);
            var table = db.ExecuteQuery("SELECT ID, ApiKey, ApiSecret FROM SecretKey ORDER BY ID DESC LIMIT 1");
            if (table.Rows.Count == 0)
            {
                Debug.WriteLine("QuerySecretKey: no row found.");
                return (null, null);
            }

            var row       = table.Rows[0];
            string apiKey    = CryptoHelper.Decrypt(row["ApiKey"].ToString());
            string apiSecret = CryptoHelper.Decrypt(row["ApiSecret"].ToString());
            Debug.WriteLine($"QuerySecretKey: loaded ID={row["ID"]}.");
            return (apiKey, apiSecret);
        }

        // ── TradeList ─────────────────────────────────────────────────────────

        public void InsertTradeList(string tradeListID, string symbol, string isBuyers,
            string price, string qty, string year, string month, string time)
        {
            using var db = new SQLiteHelper(DbPath);
            db.ExecuteNonQuery(
                "INSERT INTO TradeList (TradeListID, Symbol, IsBuyers, Price, QTY, Year, Month, Time) " +
                "VALUES (@TradeListID, @Symbol, @IsBuyers, @Price, @QTY, @Year, @Month, @Time)",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "@TradeListID", tradeListID },
                    { "@Symbol",      symbol      },
                    { "@IsBuyers",    isBuyers     },
                    { "@Price",       price        },
                    { "@QTY",         qty          },
                    { "@Year",        year         },
                    { "@Month",       month        },
                    { "@Time",        time         }
                });
        }

        /// <summary>Returns the row ID string if found, empty string otherwise.</summary>
        public string QueryTradeList(string tradeListID)
        {
            using var db = new SQLiteHelper(DbPath);
            var table = db.ExecuteQuery(
                "SELECT * FROM TradeList WHERE TradeListID=@TradeListID",
                new System.Collections.Generic.Dictionary<string, object> { { "@TradeListID", tradeListID } });

            return table.Rows.Count > 0 ? table.Rows[table.Rows.Count - 1]["ID"].ToString() : "";
        }

        public ObservableCollection<TradeListEntity> QueryTradeListBySymbol(string symbol)
        {
            var result = new ObservableCollection<TradeListEntity>();
            using var db = new SQLiteHelper(DbPath);
            var table = db.ExecuteQuery(
                "SELECT * FROM TradeList WHERE Symbol=@Symbol",
                new System.Collections.Generic.Dictionary<string, object> { { "@Symbol", symbol } });

            foreach (System.Data.DataRow row in table.Rows)
                result.Add(MapTradeListEntity(row));

            return result;
        }

        public ObservableCollection<TradeListEntity> QueryTradeListByYearMonth(string year, string month, string symbol)
        {
            var result = new ObservableCollection<TradeListEntity>();
            using var db = new SQLiteHelper(DbPath);
            var table = db.ExecuteQuery(
                "SELECT * FROM TradeList WHERE Year=@Year AND Month=@Month AND Symbol=@Symbol",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "@Year",   year   },
                    { "@Month",  month  },
                    { "@Symbol", symbol }
                });

            foreach (System.Data.DataRow row in table.Rows)
                result.Add(MapTradeListEntity(row));

            return result;
        }

        public ObservableCollection<TradeListEntity> QueryAllTradeList()
        {
            var result = new ObservableCollection<TradeListEntity>();
            using var db = new SQLiteHelper(DbPath);
            var table = db.ExecuteQuery("SELECT * FROM TradeList");
            foreach (System.Data.DataRow row in table.Rows)
                result.Add(MapTradeListEntity(row));
            return result;
        }

        private static TradeListEntity MapTradeListEntity(System.Data.DataRow row) => new TradeListEntity
        {
            TradeListID = row["TradeListID"].ToString(),
            Symbol      = row["Symbol"].ToString(),
            IsBuyers    = row["IsBuyers"].ToString(),
            Price       = row["Price"].ToString(),
            QTY         = row["QTY"].ToString(),
            Year        = row["Year"].ToString(),
            Month       = row["Month"].ToString(),
            Time        = row["Time"].ToString()
        };

        // ── Coin ──────────────────────────────────────────────────────────────

        public void InsertCoin(string free, string asset)
        {
            using var db = new SQLiteHelper(DbPath);
            db.ExecuteNonQuery(
                "INSERT INTO Coin (Free, Asset) VALUES (@Free, @Asset)",
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "@Free",  free  },
                    { "@Asset", asset }
                });
        }

        public string QueryCoin(string asset)
        {
            using var db = new SQLiteHelper(DbPath);
            var table = db.ExecuteQuery(
                "SELECT * FROM Coin WHERE Asset=@Asset",
                new System.Collections.Generic.Dictionary<string, object> { { "@Asset", asset } });

            return table.Rows.Count > 0 ? table.Rows[table.Rows.Count - 1]["ID"].ToString() : "";
        }
    }
}
