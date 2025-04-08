namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cocona;
    using Dapper;
    using MySqlConnector;

    public partial class DailyMetricMigration
    {
        private readonly string dailyMetricDbName = "DailyMetrics";
        private string _connectionString;
        private StreamWriter _dailyMetricsBulkFile;
        private List<string> _dailyMetricFiles;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public async Task Migration(
            [Option("mysql-server", Description = "A hostname of MySQL server.")] string mysqlServer,
            [Option("mysql-port", Description = "A port of MySQL server.")] uint mysqlPort,
            [Option("mysql-username", Description = "The name of MySQL user.")] string mysqlUsername,
            [Option("mysql-password", Description = "The password of MySQL user.")] string mysqlPassword,
            [Option("mysql-database", Description = "The name of MySQL database to use.")] string mysqlDatabase,
            [Option("date", Description = "Date to migrate")] string date)
        {
            DateTimeOffset start = DateTimeOffset.UtcNow;
            var builder = new MySqlConnectionStringBuilder
            {
                Database = mysqlDatabase,
                UserID = mysqlUsername,
                Password = mysqlPassword,
                Server = mysqlServer,
                Port = mysqlPort,
                AllowLoadLocalInfile = true,
                AllowUserVariables = true,
                ConnectionTimeout = 3600,
                DefaultCommandTimeout = 3600,
            };
            _connectionString = builder.ConnectionString;

            _dailyMetricFiles = new List<string>();
            CreateBulkFiles();

            Dictionary<string, object> metrics;
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                metrics = (await connection.QueryFirstAsync<Dictionary<string, object>>(@$"
                    SELECT
                        (SELECT COUNT(DISTINCT Signer) FROM Transactions WHERE Date = @date) AS dau,
                        (SELECT COUNT(TxId) FROM Transactions WHERE Date = @date) AS txCount,
                        (SELECT COUNT(Signer) FROM Transactions WHERE ActionType = 'ApprovePledge' AND Date = @date) AS newDau,
                        (SELECT COUNT(Id) FROM HackAndSlashes WHERE Date = @date) AS hasCount,
                        (SELECT COUNT(DISTINCT AgentAddress) FROM HackAndSlashes WHERE Date = @date) AS hasUsers,
                        (SELECT COUNT(Id) FROM HackAndSlashSweeps WHERE Date = @date) AS sweepCount,
                        (SELECT COUNT(DISTINCT AgentAddress) FROM HackAndSlashSweeps WHERE Date = @date) AS sweepUsers,
                        (SELECT COUNT(Id) FROM CombinationEquipments WHERE Date = @date) AS combinationEquipmentCount,
                        (SELECT COUNT(DISTINCT AgentAddress) FROM CombinationEquipments WHERE Date = @date) AS combinationEquipmentUsers,
                        (SELECT COUNT(Id) FROM CombinationConsumables WHERE Date = @date) AS combinationConsumableCount,
                        (SELECT COUNT(DISTINCT AgentAddress) FROM CombinationConsumables WHERE Date = @date) AS combinationConsumableUsers,
                        (SELECT COUNT(Id) FROM ItemEnhancements WHERE Date = @date) AS itemEnhancementCount,
                        (SELECT COUNT(DISTINCT AgentAddress) FROM ItemEnhancements WHERE Date = @date) AS itemEnhancementUsers,
                        (SELECT IFNULL(SUM(SummonCount), 0) FROM AuraSummons WHERE GroupId = '10002' AND Date = @date) AS auraSummon,
                        (SELECT IFNULL(SUM(SummonCount), 0) FROM RuneSummons WHERE Date = @date) AS runeSummon,
                        (SELECT IFNULL(SUM(ApStoneCount), 0) FROM HackAndSlashSweeps WHERE Date = @date) AS apUsage,
                        (SELECT IFNULL(SUM(HourglassCount), 0) FROM RapidCombinations WHERE Date = @date) AS hourglassUsage,
                        (SELECT IFNULL(SUM(price), 0) FROM (
                            SELECT Price FROM ShopHistoryConsumables WHERE Date = @date
                            UNION ALL SELECT Price FROM ShopHistoryCostumes WHERE Date = @date
                            UNION ALL SELECT Price FROM ShopHistoryEquipments WHERE Date = @date
                            UNION ALL SELECT Price FROM ShopHistoryMaterials WHERE Date = @date
                        ) a) AS ncgTrade,
                        (SELECT IFNULL(SUM(BurntNCG), 0) FROM ItemEnhancements WHERE Date = @date) AS enhanceNcg,
                        (SELECT IFNULL(SUM(BurntNCG), 0) FROM RuneEnhancements WHERE Date = @date) AS runeNcg,
                        (SELECT IFNULL(SUM(BurntNCG), 0) FROM UnlockRuneSlots WHERE Date = @date) AS runeSlotNcg,
                        (SELECT IFNULL(SUM(BurntNCG), 0) FROM BattleArenas WHERE Date = @date) AS arenaNcg,
                        (SELECT IFNULL(SUM(BurntNCG), 0) FROM EventDungeonBattles WHERE Date = @date) AS eventTicketNcg
                    ", new { date })).ToDictionary(kv => kv.Key, kv => kv.Value ?? 0);

                await connection.CloseAsync();
            }

            _dailyMetricsBulkFile.WriteLine(string.Join(";", new[] { date }.Concat(metrics.Values)));
            _dailyMetricsBulkFile.Flush();
            _dailyMetricsBulkFile.Close();

            foreach (var path in _dailyMetricFiles)
            {
                BulkInsert(dailyMetricDbName, path);
            }

            var end = DateTimeOffset.Now;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
        }

        private void CreateBulkFiles()
        {
            string dailyMetricsFilePath = Path.GetRandomFileName();
            _dailyMetricsBulkFile = new StreamWriter(dailyMetricsFilePath);
            _dailyMetricFiles.Add(dailyMetricsFilePath);
        }

        private void BulkInsert(
            string tableName,
            string filePath,
            int? linesToSkip = 0)
        {
            using MySqlConnection connection = new MySqlConnection(_connectionString);
            try
            {
                DateTimeOffset start = DateTimeOffset.Now;
                Console.WriteLine($"Start bulk insert to {tableName}.");
                MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                {
                    TableName = tableName,
                    FileName = filePath,
                    Timeout = 0,
                    LineTerminator = "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                    NumberOfLinesToSkip = linesToSkip ?? 0,
                };

                loader.Load();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine($"Bulk load to {tableName} failed. Retry bulk insert");
                DateTimeOffset start = DateTimeOffset.Now;
                Console.WriteLine($"Start bulk insert to {tableName}.");
                MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                {
                    TableName = tableName,
                    FileName = filePath,
                    Timeout = 0,
                    LineTerminator = "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                    NumberOfLinesToSkip = linesToSkip ?? 0,
                };

                loader.Load();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);
            }
        }
    }
}
