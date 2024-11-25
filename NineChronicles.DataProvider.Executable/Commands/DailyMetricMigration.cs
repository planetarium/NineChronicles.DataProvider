namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Cocona;
    using Dapper;
    using MySqlConnector;
    using NineChronicles.DataProvider.Store.Models;

    public class DailyMetricMigration
    {
        private readonly string dailyMetricDbName = "DailyMetrics";

        private readonly Dictionary<string, string> _avatarCache = new (); // Cache for Avatar details
        private string _connectionString;
        private StreamWriter _dailyMetricsBulkFile;
        private List<string> _dailyMetricFiles;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public async Task Migration(
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
                AllowUserVariables = true,
                ConnectionTimeout = 1800,
                DefaultCommandTimeout = 1800,
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
                    $"SELECT COUNT(DISTINCT Signer) as 'Unique Address' FROM Transactions WHERE Date = '{date}'";
                connection.Open();
                var dauCommand = new MySqlCommand(dauQuery, connection);
                dauCommand.CommandTimeout = 3600;
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
                    $"SELECT COUNT(TxId) as 'Transactions' FROM Transactions WHERE Date = '{date}'";
                connection.Open();
                var txCountCommand = new MySqlCommand(txCountQuery, connection);
                txCountCommand.CommandTimeout = 3600;
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
                newDauCommand.CommandTimeout = 3600;
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
                hasCountCommand.CommandTimeout = 3600;
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
                hasUsersCommand.CommandTimeout = 3600;
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
                sweepCountCommand.CommandTimeout = 3600;
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
                sweepUsersCommand.CommandTimeout = 3600;
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
                combinationEquipmentCountCommand.CommandTimeout = 3600;
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
                combinationEquipmentUsersCommand.CommandTimeout = 3600;
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
                combinationConsumableCountCommand.CommandTimeout = 3600;
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
                combinationConsumableUsersCommand.CommandTimeout = 3600;
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
                itemEnhancementCountCommand.CommandTimeout = 3600;
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
                itemEnhancementUsersCommand.CommandTimeout = 3600;
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
                auraSummonCommand.CommandTimeout = 3600;
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
                runeSummonCommand.CommandTimeout = 3600;
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
                apUsageCommand.CommandTimeout = 3600;
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
                hourglassUsageCommand.CommandTimeout = 3600;
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
                ncgTradeCommand.CommandTimeout = 3600;
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
                    $"SELECT sum(BurntNCG) as 'Enhance NCG(Amount)' from ItemEnhancements  where Date = '{date}'";
                connection.Open();
                var enhanceNcgCommand = new MySqlCommand(enhanceNcgQuery, connection);
                enhanceNcgCommand.CommandTimeout = 3600;
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
                    $"SELECT sum(BurntNCG) as 'Rune NCG(Amount)' from RuneEnhancements   where Date = '{date}'";
                connection.Open();
                var runeNcgCommand = new MySqlCommand(runeNcgQuery, connection);
                runeNcgCommand.CommandTimeout = 3600;
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
                    $"SELECT sum(BurntNCG) as 'RuneSlot NCG(Amount)' from UnlockRuneSlots  where Date = '{date}'";
                connection.Open();
                var runeSlotNcgCommand = new MySqlCommand(runeSlotNcgQuery, connection);
                runeSlotNcgCommand.CommandTimeout = 3600;
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
                    $"SELECT sum(BurntNCG) as 'Arena NCG(Amount)' from BattleArenas where Date = '{date}'";
                connection.Open();
                var arenaNcgCommand = new MySqlCommand(arenaNcgQuery, connection);
                arenaNcgCommand.CommandTimeout = 3600;
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
                eventTicketNcgCommand.CommandTimeout = 3600;
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

            try
            {
                start = DateTimeOffset.Now;
                Console.WriteLine("Starting all ranking computations asynchronously...");

                // Define tasks for each ranking computation
                var abilityRankingTask = Task.Run(async () =>
                {
                    Console.WriteLine("Fetching and processing Ability Rankings...");
                    await ProcessAbilityRankingsAsync();
                    Console.WriteLine("Ability Rankings processed successfully.");
                });

                var craftRankingTask = Task.Run(async () =>
                {
                    Console.WriteLine("Fetching and processing Craft Rankings...");
                    await ProcessCraftRankingsAsync();
                    Console.WriteLine("Craft Rankings processed successfully.");
                });

                var stageRankingTask = Task.Run(async () =>
                {
                    Console.WriteLine("Fetching and processing Stage Rankings...");
                    await ProcessStageRankingsAsync();
                    Console.WriteLine("Stage Rankings processed successfully.");
                });

                var equipmentRankingTask = Task.Run(async () =>
                {
                    Console.WriteLine("Fetching and processing Equipment Rankings...");
                    await ProcessAllEquipmentRankingsAsync();
                    Console.WriteLine("Equipment Rankings processed successfully.");
                });

                // Await all tasks to run in parallel
                await Task.WhenAll(craftRankingTask, stageRankingTask, equipmentRankingTask, abilityRankingTask);

                var end = DateTimeOffset.Now;
                Console.WriteLine("All rankings processed successfully. Time Elapsed: {0}", end - start);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing rankings procedures: {ex.Message}");
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
                };

                loader.Load();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);
            }
        }

        private async Task InsertCraftRankingsBulkAsync(List<CraftRankingModel> rankings, string tableName)
        {
            // Temporary file to store data for bulk insert
            string tempFilePath = Path.GetTempFileName();

            try
            {
                DateTimeOffset start = DateTimeOffset.Now;
                Console.WriteLine("Write rankings to the temporary file asynchronously.");

                // Step 1: Write rankings to the temporary file
                using (var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
                {
                    foreach (var ranking in rankings)
                    {
                        await writer.WriteLineAsync($"{ranking.AvatarAddress};{ranking.AgentAddress};{ranking.BlockIndex};{ranking.CraftCount};{ranking.Ranking};" +
                                                    $"{ranking.ArmorId};{ranking.AvatarLevel};{ranking.Cp};{ranking.Name};{ranking.TitleId}");
                    }
                }

                Console.WriteLine("Write rankings to the temporary file complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);

                // Truncate the target table asynchronously
                using (var newConnection = new MySqlConnection(_connectionString))
                {
                    await newConnection.OpenAsync();
                    var truncateQuery = $"TRUNCATE TABLE {tableName};";
                    await newConnection.ExecuteAsync(truncateQuery);
                }

                // Step 2: Use BulkInsert method to load data into the database
                await Task.Run(() => BulkInsert(tableName, tempFilePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk insert: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Cleanup: Delete the temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private async Task InsertStageRankingsBulkAsync(List<StageRankingModel> rankings, string tableName)
        {
            // Temporary file to store data for bulk insert
            string tempFilePath = Path.GetTempFileName();

            try
            {
                DateTimeOffset start = DateTimeOffset.Now;
                Console.WriteLine("Write stage rankings to the temporary file asynchronously.");

                // Step 1: Write rankings to the temporary file
                using (var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
                {
                    await writer.WriteLineAsync(string.Empty);
                    foreach (var ranking in rankings)
                    {
                        await writer.WriteLineAsync($"{ranking.Ranking.ToString()};{ranking.ClearedStageId};{ranking.AvatarAddress};{ranking.AgentAddress};" +
                                                    $"{ranking.Name};{ranking.AvatarLevel};{ranking.TitleId};{ranking.ArmorId};" +
                                                    $"{ranking.Cp};{ranking.BlockIndex}");
                    }
                }

                Console.WriteLine("Write stage rankings to the temporary file complete.");
                DateTimeOffset end = DateTimeOffset.Now;
                Console.WriteLine("Time elapsed: {0}", end - start);

                // Truncate the target table asynchronously
                using (var newConnection = new MySqlConnection(_connectionString))
                {
                    await newConnection.OpenAsync();
                    var truncateQuery = $"TRUNCATE TABLE {tableName};";
                    await newConnection.ExecuteAsync(truncateQuery);
                }

                // Step 2: Use BulkInsert method to load data into the database
                await Task.Run(() => BulkInsert(tableName, tempFilePath, 1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk insert: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Cleanup: Delete the temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private async Task ProcessAbilityRankingsAsync()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                try
                {
                    Console.WriteLine("Fetching Ability Rankings data...");

                    // Fetch data without ordering
                    var abilityData = (await connection.QueryAsync<AbilityRankingData>(@"
                        SELECT 
                            Address,
                            AgentAddress,
                            Name,
                            TitleId,
                            AvatarLevel,
                            ArmorId,
                            Cp
                        FROM Avatars")).ToList();

                    Console.WriteLine($"Fetched {abilityData.Count} records for Ability Rankings.");

                    // Sanitize data: Remove ZWNBSP and trim strings
                    foreach (var item in abilityData)
                    {
                        item.Address = RemoveZeroWidthSpaces(item.Address?.Trim());
                    }

                    // Perform in-memory sorting by Cp descending
                    var sortedData = abilityData
                        .OrderByDescending(item => item.Cp) // Sort by Cp in descending order
                        .ToList();

                    // Compute rankings based on sorted order
                    var rankings = sortedData
                        .Select((item, index) => new AbilityRankingModel
                        {
                            AvatarAddress = item.Address,
                            AgentAddress = item.AgentAddress,
                            Name = item.Name,
                            TitleId = item.TitleId,
                            AvatarLevel = item.AvatarLevel,
                            ArmorId = item.ArmorId,
                            Cp = item.Cp,
                            Ranking = index + 1 // Assign rank based on descending order of Cp
                        })
                        .ToList();

                    // Perform bulk insert into AbilityRanking table
                    Console.WriteLine("Inserting Ability Rankings into the database...");
                    await InsertAbilityRankingsBulkAsync(rankings, "AbilityRanking");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing Ability Rankings: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private async Task ProcessCraftRankingsAsync()
        {
            var consumablesData = new List<CraftData>();
            var equipmentsData = new List<CraftData>();
            var enhancementsData = new List<CraftData>();

            try
            {
                // Use separate connections for each query
                var consumablesTask = Task.Run(async () =>
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        consumablesData = (await connection.QueryAsync<CraftData>(@"
                            SELECT c.AvatarAddress, c.AgentAddress, c.BlockIndex, 1 AS CraftCount,
                                   a.ArmorId, a.AvatarLevel, a.Cp, a.Name, a.TitleId
                            FROM CombinationConsumables c
                            LEFT JOIN Avatars a ON c.AvatarAddress = a.Address")).ToList();
                    }
                });

                var equipmentsTask = Task.Run(async () =>
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        equipmentsData = (await connection.QueryAsync<CraftData>(@"
                            SELECT c.AvatarAddress, c.AgentAddress, c.BlockIndex, 1 AS CraftCount,
                                   a.ArmorId, a.AvatarLevel, a.Cp, a.Name, a.TitleId
                            FROM CombinationEquipments c
                            LEFT JOIN Avatars a ON c.AvatarAddress = a.Address")).ToList();
                    }
                });

                var enhancementsTask = Task.Run(async () =>
                {
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        enhancementsData = (await connection.QueryAsync<CraftData>(@"
                            SELECT c.AvatarAddress, c.AgentAddress, c.BlockIndex, 1 AS CraftCount,
                                   a.ArmorId, a.AvatarLevel, a.Cp, a.Name, a.TitleId
                            FROM ItemEnhancements c
                            LEFT JOIN Avatars a ON c.AvatarAddress = a.Address")).ToList();
                    }
                });

                // Wait for all tasks to complete
                await Task.WhenAll(consumablesTask, equipmentsTask, enhancementsTask);

                var combinedData = consumablesData.Concat(equipmentsData).Concat(enhancementsData);

                // Process rankings
                var rankings = combinedData
                    .GroupBy(data => data.AvatarAddress)
                    .Select((group, index) => new CraftRankingModel
                    {
                        AvatarAddress = group.Key,
                        AgentAddress = group.First().AgentAddress,
                        ArmorId = group.First().ArmorId,
                        AvatarLevel = group.First().AvatarLevel,
                        Cp = group.First().Cp,
                        Name = group.First().Name,
                        TitleId = group.First().TitleId,
                        BlockIndex = group.Max(x => x.BlockIndex),
                        CraftCount = group.Count(),
                        Ranking = index + 1
                    })
                    .ToList();

                await InsertCraftRankingsBulkAsync(rankings, "CraftRankings");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Craft Rankings: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task ProcessStageRankingsAsync()
        {
            using (var newConnection = new MySqlConnection(_connectionString))
            {
                await newConnection.OpenAsync();

                // Fetch raw data
                var stageData = (await newConnection.QueryAsync<StageData>(@"
                    SELECT hs.AvatarAddress, hs.AgentAddress, hs.StageId, hs.BlockIndex,
                           a.Name, a.AvatarLevel, a.TitleId, a.ArmorId, a.Cp
                    FROM HackAndSlashes hs
                    LEFT JOIN Avatars a ON hs.AvatarAddress = a.Address
                    WHERE hs.Mimisbrunnr = 0 AND hs.Cleared = 1")).ToList();

                // Process rankings
                var rankings = stageData
                    .GroupBy(data => data.AvatarAddress)
                    .Select((group, index) => new StageRankingModel
                    {
                        AvatarAddress = group.Key,
                        AgentAddress = group.First().AgentAddress,
                        Name = group.First().Name,
                        AvatarLevel = group.First().AvatarLevel,
                        TitleId = group.First().TitleId,
                        ArmorId = group.First().ArmorId,
                        Cp = group.First().Cp,
                        ClearedStageId = group.Max(x => x.StageId),
                        BlockIndex = group.Min(x => x.BlockIndex),
                        Ranking = index + 1
                    })
                    .ToList();

                await InsertStageRankingsBulkAsync(rankings, "StageRanking");
            }
        }

        private async Task ProcessAllEquipmentRankingsAsync()
        {
            using (var newConnection = new MySqlConnection(_connectionString))
            {
                await newConnection.OpenAsync();

                var equipmentData = (await newConnection.QueryAsync<EquipmentData>(@"
                    SELECT e.ItemId, e.AvatarAddress, e.AgentAddress, e.EquipmentId, e.Cp,
                           e.Level AS Level, e.ItemSubType, 
                           a.Name, a.AvatarLevel, a.TitleId, a.ArmorId
                    FROM Equipments e
                    LEFT JOIN Avatars a ON e.AvatarAddress = a.Address
                    ORDER BY e.Cp DESC, e.Level DESC")).ToList();

                // Process rankings asynchronously for all subtypes
                var tasks = new List<Task>
                {
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData, "EquipmentRanking")),
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Armor").ToList(), "EquipmentRankingArmor")),
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Ring").ToList(), "EquipmentRankingRing")),
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Belt").ToList(), "EquipmentRankingBelt")),
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Necklace").ToList(), "EquipmentRankingNecklace")),
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Weapon").ToList(), "EquipmentRankingWeapon")),
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Aura").ToList(), "EquipmentRankingAura")),
                    Task.Run(() => ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Grimoire").ToList(), "EquipmentRankingGrimoire"))
                };

                await Task.WhenAll(tasks);
            }
        }

        // Updated method to process rankings asynchronously
        private async Task ProcessEquipmentRankingsAsync(List<EquipmentData> equipmentData, string tableName)
        {
            if (equipmentData.Count == 0)
            {
                Console.WriteLine($"No data to process for {tableName}.");
                return;
            }

            try
            {
                // Compute rankings
                var rankings = equipmentData
                    .Select((item, index) => new EquipmentRankingModel
                    {
                        AvatarAddress = item.AvatarAddress,
                        AgentAddress = item.AgentAddress,
                        EquipmentId = item.EquipmentId,
                        ItemId = item.ItemId,
                        Cp = item.Cp,
                        Level = item.Level,
                        ItemSubType = item.ItemSubType,
                        Name = item.Name,
                        AvatarLevel = item.AvatarLevel,
                        TitleId = item.TitleId,
                        ArmorId = item.ArmorId,
                        Ranking = index + 1
                    })
                    .ToList();

                // Truncate the target table asynchronously
                using (var newConnection = new MySqlConnection(_connectionString))
                {
                    await newConnection.OpenAsync();
                    var truncateQuery = $"TRUNCATE TABLE {tableName};";
                    await newConnection.ExecuteAsync(truncateQuery);
                }

                Console.WriteLine($"Inserting rankings into {tableName}...");
                await InsertEquipmentRankingsBulkAsync(rankings, tableName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing rankings for {tableName}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        // Updated method to handle asynchronous bulk insertion
        private async Task InsertEquipmentRankingsBulkAsync(List<EquipmentRankingModel> rankings, string tableName)
        {
            string tempFilePath = Path.GetTempFileName();

            try
            {
                // Write rankings to the temporary file
                using (var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
                {
                    foreach (var ranking in rankings)
                    {
                        writer.WriteLine($"{ranking.ItemId};{ranking.AgentAddress};{ranking.AvatarAddress};{ranking.EquipmentId};" +
                                         $"{ranking.Cp};{ranking.Level};{ranking.ItemSubType};{ranking.Name};" +
                                         $"{ranking.AvatarLevel};{ranking.TitleId};{ranking.ArmorId};{ranking.Ranking}");
                    }
                }

                // Perform the bulk insert
                await Task.Run(() => BulkInsert(tableName, tempFilePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk insert into {tableName}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        // Method to perform bulk insert for Ability Rankings
        private async Task InsertAbilityRankingsBulkAsync(List<AbilityRankingModel> rankings, string tableName)
        {
            string tempFilePath = Path.GetTempFileName();

            try
            {
                // Write rankings to the temporary file
                using (var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
                {
                    await writer.WriteLineAsync(string.Empty);
                    foreach (var ranking in rankings)
                    {
                        await writer.WriteLineAsync($"{ranking.AvatarAddress};{ranking.AgentAddress};{ranking.Name};" +
                                                    $"{ranking.AvatarLevel};{ranking.TitleId};{ranking.ArmorId};" +
                                                    $"{ranking.Cp};{ranking.Ranking}");
                    }
                }

                // Truncate the target table asynchronously
                using (var newConnection = new MySqlConnection(_connectionString))
                {
                    await newConnection.OpenAsync();
                    var truncateQuery = $"TRUNCATE TABLE {tableName};";
                    await newConnection.ExecuteAsync(truncateQuery);
                }

                // Perform the bulk insert
                await Task.Run(() => BulkInsert(tableName, tempFilePath, 1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk insert into {tableName}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private string RemoveZeroWidthSpaces(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (input.StartsWith(bom, StringComparison.Ordinal))
            {
                input = input.Substring(bom.Length);
            }

            return input;
        }

        // Data class for raw stage data
        private class StageData
        {
            public string AvatarAddress { get; set; } = string.Empty;

            public string AgentAddress { get; set; } = string.Empty;

            public int StageId { get; set; } = 0;

            public int BlockIndex { get; set; } = 0;

            public string Name { get; set; } = string.Empty;

            public int AvatarLevel { get; set; } = 0;

            public int TitleId { get; set; } = 0;

            public int ArmorId { get; set; } = 0;

            public int Cp { get; set; } = 0;
        }

        // Data class for craft data
        private class CraftData
        {
            public string AvatarAddress { get; set; } = string.Empty;

            public string AgentAddress { get; set; } = string.Empty;

            public int BlockIndex { get; set; } = 0;

            public int CraftCount { get; set; } = 0;

            public int ArmorId { get; set; } = 0;

            public int AvatarLevel { get; set; } = 0;

            public int Cp { get; set; } = 0;

            public int TitleId { get; set; } = 0;

            public string Name { get; set; } = string.Empty;
        }

        // Data class for equipment data
        private class EquipmentData
        {
            public string ItemId { get; set; } = string.Empty;

            public string AgentAddress { get; set; } = string.Empty;

            public string AvatarAddress { get; set; } = string.Empty;

            public int EquipmentId { get; set; } = 0;

            public int Cp { get; set; } = 0;

            public int Level { get; set; } = 0;

            public string ItemSubType { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public int AvatarLevel { get; set; } = 0;

            public int TitleId { get; set; } = 0;

            public int ArmorId { get; set; } = 0;
        }

        private class AbilityRankingData
        {
            public string Address { get; set; } = string.Empty;

            public string AgentAddress { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public int TitleId { get; set; } = 0;

            public int AvatarLevel { get; set; } = 0;

            public int ArmorId { get; set; } = 0;

            public int Cp { get; set; } = 0;
        }

        private class AbilityRankingModel
        {
            public string AvatarAddress { get; set; } = string.Empty;

            public string AgentAddress { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public int TitleId { get; set; } = 0;

            public int AvatarLevel { get; set; } = 0;

            public int ArmorId { get; set; } = 0;

            public int Cp { get; set; } = 0;

            public int Ranking { get; set; } = 0;
        }
    }
}
