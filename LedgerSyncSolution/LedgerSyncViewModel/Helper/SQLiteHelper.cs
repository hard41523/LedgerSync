using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedgerSyncViewModel.Helper
{
    public class SQLiteHelper : IDisposable
    {
        private readonly string _connectionString;
        private SQLiteConnection _connection;

        public SQLiteHelper(string databasePath)
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
            _connection = new SQLiteConnection(_connectionString);
            _connection.Open();
        }

        // Execute non-query SQL (INSERT, UPDATE, DELETE)
        public int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            using (var cmd = new SQLiteCommand(query, _connection))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        // Execute scalar query (returns single value)
        public object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            using (var cmd = new SQLiteCommand(query, _connection))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteScalar();
            }
        }

        // Execute SELECT query (returns DataTable)
        public DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
        {
            using (var cmd = new SQLiteCommand(query, _connection))
            {
                AddParameters(cmd, parameters);
                using (var adapter = new SQLiteDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        // Add parameters to command
        private void AddParameters(SQLiteCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
        }

        // FIX: ExecuteTransaction now accepts parameters per query to prevent SQL injection
        // Each entry: (sql query, optional parameters dictionary)
        public void ExecuteTransaction(List<(string Query, Dictionary<string, object> Parameters)> queries)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                using (var cmd = new SQLiteCommand(_connection))
                {
                    try
                    {
                        foreach (var (query, parameters) in queries)
                        {
                            cmd.CommandText = query;
                            cmd.Parameters.Clear();
                            AddParameters(cmd, parameters);
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SQLiteHelper transaction rollback: {ex.Message}");
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // Release resources
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
