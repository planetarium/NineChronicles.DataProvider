using System.Net.Http;
using System.Threading.Tasks;

namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Bencodex.Types;
    using Cocona;
    using Lib9c.Model.Order;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using MySqlConnector;
    using Nekoyume;
    using Nekoyume.Action.Loader;
    using Nekoyume.Battle;
    using Nekoyume.Blockchain.Policy;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    public class DailyMetricMigration
    {
        private readonly string dailyMetricDbName = "DailyMetrics";

        private string _connectionString;
        private StreamWriter _dailyMetricsBulkFile;
        private List<string> _dailyMetricFiles;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public void Migration(
            [Option(
                "mysql-server",
                Description = "A hostname of MySQL server.")]
            string mysqlServer,
            [Option(
                "mysql-port",
                Description = "A port of MySQL server.")]
            uint mysqlPort,
            [Option(
                "mysql-username",
                Description = "The name of MySQL user.")]
            string mysqlUsername,
            [Option(
                "mysql-password",
                Description = "The password of MySQL user.")]
            string mysqlPassword,
            [Option(
                "mysql-database",
                Description = "The name of MySQL database to use.")]
            string mysqlDatabase,
            [Option(
                "date",
                Description = "Date to migrate")]
            string date
        )
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
            };

            _connectionString = builder.ConnectionString;
            Console.WriteLine("Start migration.");

            // files to store bulk file paths (new file created every 10000 blocks for bulk load performance)
            _dailyMetricFiles = new List<string>();

            CreateBulkFiles();

            using MySqlConnection connection = new MySqlConnection(_connectionString);

            try
            {
                Console.WriteLine(date);

                var dau = 0;
                var dauQuery =
                    $"SELECT COUNT(DISTINCT Signer) as 'Unique Address' FROM data_provider.Transactions WHERE Date = '{date}'";
                connection.Open();
                var dauCommand = new MySqlCommand(dauQuery, connection);
                dauCommand.CommandTimeout = 600;
                var dauReader = dauCommand.ExecuteReader();
                while (dauReader.Read())
                {
                    if (!dauReader.IsDBNull(0))
                    {
                        Console.WriteLine("dau: {0}", dauReader.GetInt32(0));
                        dau = dauReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("dau is null");
                    }
                }

                connection.Close();

                var txCount = 0;
                var txCountQuery =
                    $"SELECT COUNT(TxId) as 'Transactions' FROM data_provider.Transactions WHERE Date = '{date}'";
                connection.Open();
                var txCountCommand = new MySqlCommand(txCountQuery, connection);
                txCountCommand.CommandTimeout = 600;
                var txCountReader = txCountCommand.ExecuteReader();
                while (txCountReader.Read())
                {
                    if (!txCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("txCount: {0}", txCountReader.GetInt32(0));
                        txCount = txCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("txCount is null");
                    }
                }

                connection.Close();

                var newDau = 0;
                var newDauQuery =
                    $"select count(Signer) from Transactions WHERE ActionType = 'ApprovePledge' AND Date = '{date}'";
                connection.Open();
                var newDauCommand = new MySqlCommand(newDauQuery, connection);
                newDauCommand.CommandTimeout = 600;
                var newDauReader = newDauCommand.ExecuteReader();
                while (newDauReader.Read())
                {
                    if (!newDauReader.IsDBNull(0))
                    {
                        Console.WriteLine("newDau: {0}", newDauReader.GetInt32(0));
                        newDau = newDauReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("newDau is null");
                    }
                }

                connection.Close();

                var hasCount = 0;
                var hasCountQuery = $"select count(Id) as 'Count' from HackAndSlashes where Date = '{date}'";
                connection.Open();
                var hasCountCommand = new MySqlCommand(hasCountQuery, connection);
                hasCountCommand.CommandTimeout = 600;
                var hasCountReader = hasCountCommand.ExecuteReader();
                while (hasCountReader.Read())
                {
                    if (!hasCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("hasCount: {0}", hasCountReader.GetInt32(0));
                        hasCount = hasCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("hasCount is null");
                    }
                }

                connection.Close();

                var hasUsers = 0;
                var hasUsersQuery =
                    $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from HackAndSlashes where Date = '{date}'";
                connection.Open();
                var hasUsersCommand = new MySqlCommand(hasUsersQuery, connection);
                hasUsersCommand.CommandTimeout = 600;
                var hasUsersReader = hasUsersCommand.ExecuteReader();
                while (hasUsersReader.Read())
                {
                    if (!hasUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("hasUsers: {0}", hasUsersReader.GetInt32(0));
                        hasUsers = hasUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("hasUsers is null");
                    }
                }

                connection.Close();

                var sweepCount = 0;
                var sweepCountQuery = $"select count(Id) as 'Count' from HackAndSlashSweeps where Date = '{date}'";
                connection.Open();
                var sweepCountCommand = new MySqlCommand(sweepCountQuery, connection);
                sweepCountCommand.CommandTimeout = 600;
                var sweepCountReader = sweepCountCommand.ExecuteReader();
                while (sweepCountReader.Read())
                {
                    if (!sweepCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("sweepCount: {0}", sweepCountReader.GetInt32(0));
                        sweepCount = sweepCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("sweepCount is null");
                    }
                }

                connection.Close();

                var sweepUsers = 0;
                var sweepUsersQuery =
                    $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from HackAndSlashSweeps where Date =  '{date}'";
                connection.Open();
                var sweepUsersCommand = new MySqlCommand(sweepUsersQuery, connection);
                sweepUsersCommand.CommandTimeout = 600;
                var sweepUsersReader = sweepUsersCommand.ExecuteReader();
                while (sweepUsersReader.Read())
                {
                    if (!sweepUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("sweepUsers: {0}", sweepUsersReader.GetInt32(0));
                        sweepUsers = sweepUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("sweepUsers is null");
                    }
                }

                connection.Close();

                var combinationEquipmentCount = 0;
                var combinationEquipmentCountQuery =
                    $"select count(Id) as 'Count' from CombinationEquipments where Date = '{date}'";
                connection.Open();
                var combinationEquipmentCountCommand = new MySqlCommand(combinationEquipmentCountQuery, connection);
                combinationEquipmentCountCommand.CommandTimeout = 600;
                var combinationEquipmentCountReader = combinationEquipmentCountCommand.ExecuteReader();
                while (combinationEquipmentCountReader.Read())
                {
                    if (!combinationEquipmentCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationEquipmentCount: {0}",
                            combinationEquipmentCountReader.GetInt32(0));
                        combinationEquipmentCount = combinationEquipmentCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationEquipmentCount is null");
                    }
                }

                connection.Close();

                var combinationEquipmentUsers = 0;
                var combinationEquipmentUsersQuery =
                    $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from CombinationConsumables where Date = '{date}'";
                connection.Open();
                var combinationEquipmentUsersCommand = new MySqlCommand(combinationEquipmentUsersQuery, connection);
                combinationEquipmentUsersCommand.CommandTimeout = 600;
                var combinationEquipmentUsersReader = combinationEquipmentUsersCommand.ExecuteReader();
                while (combinationEquipmentUsersReader.Read())
                {
                    if (!combinationEquipmentUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationEquipmentUsers: {0}",
                            combinationEquipmentUsersReader.GetInt32(0));
                        combinationEquipmentUsers = combinationEquipmentUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationEquipmentUsers is null");
                    }
                }

                connection.Close();

                var combinationConsumableCount = 0;
                var combinationConsumableCountQuery =
                    $"select count(Id) as 'Count' from CombinationConsumables where Date = '{date}'";
                connection.Open();
                var combinationConsumableCountCommand = new MySqlCommand(combinationConsumableCountQuery, connection);
                combinationConsumableCountCommand.CommandTimeout = 600;
                var combinationConsumableCountReader = combinationConsumableCountCommand.ExecuteReader();
                while (combinationConsumableCountReader.Read())
                {
                    if (!combinationConsumableCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationConsumableCount: {0}",
                            combinationConsumableCountReader.GetInt32(0));
                        combinationConsumableCount = combinationConsumableCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationConsumableCount is null");
                    }
                }

                connection.Close();

                var combinationConsumableUsers = 0;
                var combinationConsumableUsersQuery =
                    $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from CombinationEquipments where Date = '{date}'";
                connection.Open();
                var combinationConsumableUsersCommand = new MySqlCommand(combinationConsumableUsersQuery, connection);
                combinationConsumableUsersCommand.CommandTimeout = 600;
                var combinationConsumableUsersReader = combinationConsumableUsersCommand.ExecuteReader();
                while (combinationConsumableUsersReader.Read())
                {
                    if (!combinationConsumableUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("combinationConsumableUsers: {0}",
                            combinationConsumableUsersReader.GetInt32(0));
                        combinationConsumableUsers = combinationConsumableUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("combinationConsumableUsers is null");
                    }
                }

                connection.Close();

                var itemEnhancementCount = 0;
                var itemEnhancementCountQuery =
                    $"select count(Id) as 'Count' from ItemEnhancements where Date = '{date}'";
                connection.Open();
                var itemEnhancementCountCommand = new MySqlCommand(itemEnhancementCountQuery, connection);
                itemEnhancementCountCommand.CommandTimeout = 600;
                var itemEnhancementCountReader = itemEnhancementCountCommand.ExecuteReader();
                while (itemEnhancementCountReader.Read())
                {
                    if (!itemEnhancementCountReader.IsDBNull(0))
                    {
                        Console.WriteLine("itemEnhancementCount: {0}", itemEnhancementCountReader.GetInt32(0));
                        itemEnhancementCount = itemEnhancementCountReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("itemEnhancementCount is null");
                    }
                }

                connection.Close();

                var itemEnhancementUsers = 0;
                var itemEnhancementUsersQuery =
                    $"select COUNT(DISTINCT AgentAddress) as 'Player Count' from ItemEnhancements where Date = '{date}'";
                connection.Open();
                var itemEnhancementUsersCommand = new MySqlCommand(itemEnhancementUsersQuery, connection);
                itemEnhancementUsersCommand.CommandTimeout = 600;
                var itemEnhancementUsersReader = itemEnhancementUsersCommand.ExecuteReader();
                while (itemEnhancementUsersReader.Read())
                {
                    if (!itemEnhancementUsersReader.IsDBNull(0))
                    {
                        Console.WriteLine("itemEnhancementUsers: {0}", itemEnhancementUsersReader.GetInt32(0));
                        itemEnhancementUsers = itemEnhancementUsersReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("itemEnhancementUsers is null");
                    }
                }

                connection.Close();

                var auraSummon = 0;
                var auraSummonQuery =
                    $"select SUM(SummonCount) from AuraSummons where GroupId = '10002' AND Date = '{date}'";
                connection.Open();
                var auraSummonCommand = new MySqlCommand(auraSummonQuery, connection);
                auraSummonCommand.CommandTimeout = 600;
                var auraSummonReader = auraSummonCommand.ExecuteReader();
                while (auraSummonReader.Read())
                {
                    if (!auraSummonReader.IsDBNull(0))
                    {
                        Console.WriteLine("auraSummon: {0}", auraSummonReader.GetInt32(0));
                        auraSummon = auraSummonReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("auraSummon is null");
                    }
                }

                connection.Close();

                var runeSummon = 0;
                var runeSummonQuery = $"select SUM(SummonCount) from RuneSummons where Date = '{date}'";
                connection.Open();
                var runeSummonCommand = new MySqlCommand(runeSummonQuery, connection);
                runeSummonCommand.CommandTimeout = 600;
                var runeSummonReader = runeSummonCommand.ExecuteReader();
                while (runeSummonReader.Read())
                {
                    if (!runeSummonReader.IsDBNull(0))
                    {
                        Console.WriteLine("runeSummon: {0}", runeSummonReader.GetInt32(0));
                        runeSummon = runeSummonReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("runeSummon is null");
                    }
                }

                connection.Close();

                var apUsage = 0;
                var apUsageQuery = $"select SUM(ApStoneCount) from HackAndSlashSweeps where Date = '{date}'";
                connection.Open();
                var apUsageCommand = new MySqlCommand(apUsageQuery, connection);
                apUsageCommand.CommandTimeout = 600;
                var apUsageReader = apUsageCommand.ExecuteReader();
                while (apUsageReader.Read())
                {
                    if (!apUsageReader.IsDBNull(0))
                    {
                        Console.WriteLine("apUsage: {0}", apUsageReader.GetInt32(0));
                        apUsage = apUsageReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("apUsage is null");
                    }
                }

                connection.Close();

                var hourglassUsage = 0;
                var hourglassUsageQuery = $"SELECT SUM(HourglassCount) FROM RapidCombinations WHERE Date = '{date}'";
                connection.Open();
                var hourglassUsageCommand = new MySqlCommand(hourglassUsageQuery, connection);
                hourglassUsageCommand.CommandTimeout = 600;
                var hourglassUsageReader = hourglassUsageCommand.ExecuteReader();
                while (hourglassUsageReader.Read())
                {
                    if (!hourglassUsageReader.IsDBNull(0))
                    {
                        Console.WriteLine("hourglassUsage: {0}", hourglassUsageReader.GetInt32(0));
                        hourglassUsage = hourglassUsageReader.GetInt32(0);
                    }
                    else
                    {
                        Console.WriteLine("hourglassUsage is null");
                    }
                }

                connection.Close();

                var ncgTrade = 0m;
                var ncgTradeQuery = @$"select (SUM(price)) as 'Trade NCG(Amount)' from 
                    (
                        (select Price from ShopHistoryConsumables WHERE Date = '{date}') Union all 
                        (select Price from ShopHistoryCostumes WHERE Date = '{date}' ) Union all 
                        (select Price from ShopHistoryEquipments WHERE Date = '{date}' ) Union all 
                        (select Price from ShopHistoryMaterials WHERE Date = '{date}' )
                    ) a";
                connection.Open();
                var ncgTradeCommand = new MySqlCommand(ncgTradeQuery, connection);
                ncgTradeCommand.CommandTimeout = 600;
                var ncgTradeReader = ncgTradeCommand.ExecuteReader();
                while (ncgTradeReader.Read())
                {
                    if (!ncgTradeReader.IsDBNull(0))
                    {
                        Console.WriteLine("ncgTrade: {0}", ncgTradeReader.GetDecimal(0));
                        ncgTrade = ncgTradeReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("ncgTrade is null");
                    }
                }

                connection.Close();

                var enhanceNcg = 0m;
                var enhanceNcgQuery =
                    $"SELECT sum(BurntNCG) as 'Enhance NCG(Amount)' from data_provider.ItemEnhancements  where Date = '{date}'";
                connection.Open();
                var enhanceNcgCommand = new MySqlCommand(enhanceNcgQuery, connection);
                enhanceNcgCommand.CommandTimeout = 600;
                var enhanceNcgReader = enhanceNcgCommand.ExecuteReader();
                while (enhanceNcgReader.Read())
                {
                    if (!enhanceNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("enhanceNcg: {0}", enhanceNcgReader.GetDecimal(0));
                        enhanceNcg = enhanceNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("enhanceNcg is null");
                    }
                }

                connection.Close();

                var runeNcg = 0m;
                var runeNcgQuery =
                    $"SELECT sum(BurntNCG) as 'Rune NCG(Amount)' from data_provider.RuneEnhancements   where Date = '{date}'";
                connection.Open();
                var runeNcgCommand = new MySqlCommand(runeNcgQuery, connection);
                runeNcgCommand.CommandTimeout = 600;
                var runeNcgReader = runeNcgCommand.ExecuteReader();
                while (runeNcgReader.Read())
                {
                    if (!runeNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("runeNcg: {0}", runeNcgReader.GetDecimal(0));
                        runeNcg = runeNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("runeNcg is null");
                    }
                }

                connection.Close();

                var runeSlotNcg = 0m;
                var runeSlotNcgQuery =
                    $"SELECT sum(BurntNCG) as 'RuneSlot NCG(Amount)' from data_provider.UnlockRuneSlots  where Date = '{date}'";
                connection.Open();
                var runeSlotNcgCommand = new MySqlCommand(runeSlotNcgQuery, connection);
                runeSlotNcgCommand.CommandTimeout = 600;
                var runeSlotNcgReader = runeSlotNcgCommand.ExecuteReader();
                while (runeSlotNcgReader.Read())
                {
                    if (!runeSlotNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("runeSlotNcg: {0}", runeSlotNcgReader.GetDecimal(0));
                        runeSlotNcg = runeSlotNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("runeSlotNcg is null");
                    }
                }

                connection.Close();

                var arenaNcg = 0m;
                var arenaNcgQuery =
                    $"SELECT sum(BurntNCG) as 'Arena NCG(Amount)' from data_provider.BattleArenas where Date = '{date}'";
                connection.Open();
                var arenaNcgCommand = new MySqlCommand(arenaNcgQuery, connection);
                arenaNcgCommand.CommandTimeout = 600;
                var arenaNcgReader = arenaNcgCommand.ExecuteReader();
                while (arenaNcgReader.Read())
                {
                    if (!arenaNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("arenaNcg: {0}", arenaNcgReader.GetDecimal(0));
                        arenaNcg = arenaNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("arenaNcg is null");
                    }
                }

                connection.Close();

                var eventTicketNcg = 0m;
                var eventTicketNcgQuery =
                    $"SELECT sum(BurntNCG) as 'EventTicket NCG' from EventDungeonBattles where Date = '{date}'";
                connection.Open();
                var eventTicketNcgCommand = new MySqlCommand(eventTicketNcgQuery, connection);
                eventTicketNcgCommand.CommandTimeout = 600;
                var eventTicketNcgReader = eventTicketNcgCommand.ExecuteReader();
                while (eventTicketNcgReader.Read())
                {
                    if (!eventTicketNcgReader.IsDBNull(0))
                    {
                        Console.WriteLine("eventTicketNcg: {0}", eventTicketNcgReader.GetDecimal(0));
                        eventTicketNcg = eventTicketNcgReader.GetDecimal(0);
                    }
                    else
                    {
                        Console.WriteLine("eventTicketNcg is null");
                    }
                }

                connection.Close();

                _dailyMetricsBulkFile.WriteLine(
                    $"{date};" +
                    $"{dau};" +
                    $"{txCount};" +
                    $"{newDau};" +
                    $"{hasCount};" +
                    $"{hasUsers};" +
                    $"{sweepCount};" +
                    $"{sweepUsers};" +
                    $"{combinationEquipmentCount};" +
                    $"{combinationEquipmentUsers};" +
                    $"{combinationConsumableCount};" +
                    $"{combinationConsumableUsers};" +
                    $"{itemEnhancementCount};" +
                    $"{itemEnhancementUsers};" +
                    $"{auraSummon};" +
                    $"{runeSummon};" +
                    $"{apUsage};" +
                    $"{hourglassUsage};" +
                    $"{ncgTrade};" +
                    $"{enhanceNcg};" +
                    $"{runeNcg};" +
                    $"{runeSlotNcg};" +
                    $"{arenaNcg};" +
                    $"{eventTicketNcg}"
                );

                _dailyMetricsBulkFile.Flush();
                _dailyMetricsBulkFile.Close();

                foreach (var path in _dailyMetricFiles)
                {
                    BulkInsert(dailyMetricDbName, path);
                }

                var end = DateTimeOffset.Now;
                Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void CreateBulkFiles()
        {
            string dailyMetricsFilePath = Path.GetRandomFileName();
            _dailyMetricsBulkFile = new StreamWriter(dailyMetricsFilePath);
            _dailyMetricFiles.Add(dailyMetricsFilePath);
        }

        private void BulkInsert(
            string tableName,
            string filePath)
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
                };

                loader.Load();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);
            }
        }
    }
}
