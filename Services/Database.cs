using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Npgsql;
using Newtonsoft.Json;

namespace harambe_trader.Services {
    public class Database {
        IDatabaseClient _databaseClient;

        public Database(IDatabaseClient databaseClient) {
            _databaseClient = databaseClient;
        }

        public async Task SaveOutput(string step, string section, Dictionary<string, dynamic> outputData) {
            var result = await _databaseClient.Insert("outputs", new List<(string, object, string)>() {
                ("step", step, null),
                ("section", section, null),
                ("output_data", JsonConvert.SerializeObject(outputData), "CAST(@output_data AS json)")
            });

            if (result != 1) {
                throw new ApplicationException("Failed to save output");
            }
        }

        public async Task<string> GetOutput(string step, string section) {
            var results = await _databaseClient
                .Select($@"
                    SELECT o.output_data as output_data
                    FROM outputs o
                    WHERE output_uid=(
                        SELECT MAX(o2.output_uid)
                        FROM outputs o2
                        WHERE o2.step='{step}'
                        AND o2.section='{section}'
                    )
                ");

            return (string)results[0][0];
        }
    }

    public class Output {
        public long output_uid { get; set; }
        public string step { get; set; }
        public string section { get; set; }
        public Dictionary<string, dynamic> output_data { get; set; }
    }

    public interface IDatabaseClient {
        Task<int> Insert(string tablename, List<(string, object, string)> datas);
        Task<List<object[]>> Select(string query);
    }

    public class PostgresDatabaseClient : IDatabaseClient {
        private string _connString;
        private NpgsqlConnection _conn;

        public PostgresDatabaseClient(string connectionString) {
            // TODO: initalize DB in startup section?
            _connString = connectionString;
        }

        private async Task<NpgsqlConnection> _GetConn() {
            if (_conn == null) {
                _conn = new NpgsqlConnection(_connString);
                await _conn.OpenAsync();
            }

            return _conn;
        }

        public async Task<int> Insert(string tablename, List<(string, object, string)> datas) {
            string cols = String.Join(",", datas.Select(d => d.Item1));
            string valueAliases = String.Join(",", datas.Select(d => d.Item3 != null ? d.Item3 : $"@{d.Item1}"));

            var cmdString = $"INSERT INTO {tablename} ({cols}) VALUES ({valueAliases})";

            using (var cmd = new NpgsqlCommand(cmdString, await _GetConn())) {
                datas.ForEach(data => {
                    cmd.Parameters.AddWithValue(data.Item1, data.Item2);
                });

                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<object[]>> Select(string query) {
            using (var cmd = new NpgsqlCommand(query, await _GetConn())) {
                using (var reader = await cmd.ExecuteReaderAsync()) {
                    var values = new List<object[]>();

                    while (await reader.ReadAsync()) {
                        values
                            .Add(
                                Enumerable
                                    .Range(0, reader.FieldCount)
                                    .Select(i => reader.GetValue(i))
                                    .ToArray()
                            );
                    }

                    return values;
                }
            }
        }
    }
}
