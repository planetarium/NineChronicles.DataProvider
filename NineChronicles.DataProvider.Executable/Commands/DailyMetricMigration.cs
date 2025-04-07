namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Cocona;
    using Dapper;
    using MySqlConnector;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.DataProvider.Store.Models.Ranking;

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

            // Process rankings one-by-one with delay to minimize memory
            await ProcessRankingsInSequenceWithDelay();
        }

        private async Task ProcessRankingsInSequenceWithDelay()
        {
            DateTimeOffset start = DateTimeOffset.Now;
            Console.WriteLine("Starting all ranking computations sequentially with delay...");

            var rankingTasks = new List<Func<Task>>
            {
                async () =>
                {
                    await ProcessAbilityRankingsAsync();
                    GC.Collect();
                },
                async () =>
                {
                    await ProcessCraftRankingsAsync();
                    GC.Collect();
                },
                async () =>
                {
                    await ProcessStageRankingsAsync();
                    GC.Collect();
                },
                async () =>
                {
                    await ProcessAllEquipmentRankingsAsync();
                    GC.Collect();
                },
            };

            foreach (var task in rankingTasks)
            {
                await task();
                Console.WriteLine("Waiting 5 minutes before next process...");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }

            DateTimeOffset end = DateTimeOffset.Now;
            Console.WriteLine("All rankings processed. Time Elapsed: {0}", end - start);
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
                await Task.Run(() => BulkInsertWithSwapAsync(tableName, tempFilePath, null));
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
                await Task.Run(() => BulkInsertWithSwapAsync(tableName, tempFilePath, 1));
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
                    DateTimeOffset start = DateTimeOffset.UtcNow;
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
                    DateTimeOffset end = DateTimeOffset.UtcNow;
                    Console.WriteLine("Inserting Ability Rankings into the database completed. Time Elapsed: {0}", end - start);
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
                DateTimeOffset start = DateTimeOffset.UtcNow;

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
                    .Select(group => new CraftRankingModel
                    {
                        AvatarAddress = group.Key,
                        AgentAddress = group.First().AgentAddress,
                        ArmorId = group.First().ArmorId,
                        AvatarLevel = group.First().AvatarLevel,
                        Cp = group.First().Cp,
                        Name = group.First().Name,
                        TitleId = group.First().TitleId,
                        BlockIndex = group.Max(x => x.BlockIndex),
                        CraftCount = group.Count()
                    })
                    .OrderByDescending(ranking => ranking.CraftCount) // Order by CraftCount in descending order
                    .ThenBy(ranking => ranking.BlockIndex) // Optional: Secondary sorting, if needed
                    .Select((ranking, index) =>
                    {
                        ranking.Ranking = index + 1;                 // Assign ranking after ordering
                        return ranking;
                    })
                    .ToList();

                Console.WriteLine("Inserting Craft Rankings into the database...");
                await InsertCraftRankingsBulkAsync(rankings, "CraftRankings");
                DateTimeOffset end = DateTimeOffset.UtcNow;
                Console.WriteLine("Inserting Craft Rankings into the database completed. Time Elapsed: {0}", end - start);
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
                DateTimeOffset start = DateTimeOffset.UtcNow;
                await newConnection.OpenAsync();

                // Fetch raw data
                var stageData = (await newConnection.QueryAsync<StageData>(@"
                    SELECT hs.AvatarAddress, hs.AgentAddress, hs.StageId, hs.BlockIndex,
                           a.Name, a.AvatarLevel, a.TitleId, a.ArmorId, a.Cp
                    FROM HackAndSlashes hs
                    LEFT JOIN Avatars a ON hs.AvatarAddress = a.Address
                    WHERE hs.Mimisbrunnr = 0 AND hs.Cleared = 1")).ToList(); // Fetch all data first

                stageData = stageData.OrderByDescending(data => data.StageId) // Sort by StageId in descending order
                    .ThenBy(data => data.BlockIndex) // Then sort by BlockIndex in ascending order
                    .ToList();

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

                Console.WriteLine("Inserting Stage Rankings into the database...");
                await InsertStageRankingsBulkAsync(rankings, "StageRanking");
                DateTimeOffset end = DateTimeOffset.UtcNow;
                Console.WriteLine("Inserting Stage Rankings into the database completed. Time Elapsed: {0}", end - start);
            }
        }

        private async Task ProcessAllEquipmentRankingsAsync()
        {
            using (var newConnection = new MySqlConnection(_connectionString))
            {
                DateTimeOffset start = DateTimeOffset.UtcNow;
                await newConnection.OpenAsync();

                var equipmentData = (await newConnection.QueryAsync<EquipmentData>(@"
                    SELECT e.ItemId, e.AvatarAddress, e.AgentAddress, e.EquipmentId, e.Cp,
                           e.Level AS Level, e.ItemSubType, 
                           a.Name, a.AvatarLevel, a.TitleId, a.ArmorId
                    FROM Equipments e
                    LEFT JOIN Avatars a ON e.AvatarAddress = a.Address
                    ORDER BY e.Cp DESC, e.Level DESC")).ToList();

                Console.WriteLine("Inserting Equipment Rankings into the database...");

                // Process each equipment ranking type sequentially
                await ProcessEquipmentRankingsAsync(equipmentData, "EquipmentRanking"); // All Equipment
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes
                await ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Armor").ToList(), "EquipmentRankingArmor");
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes
                await ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Ring").ToList(), "EquipmentRankingRing");
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes
                await ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Belt").ToList(), "EquipmentRankingBelt");
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes
                await ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Necklace").ToList(), "EquipmentRankingNecklace");
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes
                await ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Weapon").ToList(), "EquipmentRankingWeapon");
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes
                await ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Aura").ToList(), "EquipmentRankingAura");
                await Task.Delay(TimeSpan.FromMinutes(5)); // Wait 5 minutes
                await ProcessEquipmentRankingsAsync(equipmentData.Where(e => e.ItemSubType == "Grimoire").ToList(), "EquipmentRankingGrimoire");

                DateTimeOffset end = DateTimeOffset.UtcNow;
                Console.WriteLine("Inserting Equipment Rankings into the database completed. Time Elapsed: {0}", end - start);
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
                await Task.Run(() => BulkInsertWithSwapAsync(tableName, tempFilePath, null));
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
                await Task.Run(() => BulkInsertWithSwapAsync(tableName, tempFilePath, 1));
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

        private async Task BulkInsertWithSwapAsync(string originalTableName, string filePath, int? linesToSkip)
        {
            string tempTableName = $"{originalTableName}_Temp";
            string backupTableName = $"{originalTableName}_Backup";

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // Step 1: Create a temporary table
                    string createTempTable = $"CREATE TABLE IF NOT EXISTS `{tempTableName}` LIKE `{originalTableName}`;";
                    using (var cmd = new MySqlCommand(createTempTable, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Step 2: Bulk load data into the temporary table
                    Console.WriteLine($"Start bulk insert to {tempTableName}.");
                    MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                    {
                        TableName = tempTableName,
                        FileName = filePath,
                        Timeout = 0,
                        LineTerminator = "\n",
                        FieldTerminator = ";",
                        Local = true,
                        ConflictOption = MySqlBulkLoaderConflictOption.Ignore,
                        NumberOfLinesToSkip = linesToSkip ?? 0,
                    };
                    await Task.Run(() => loader.Load());
                    Console.WriteLine($"Bulk load to {tempTableName} complete.");

                    // Step 3: Rename tables to swap
                    string swapTables = $@"
                        RENAME TABLE `{originalTableName}` TO `{backupTableName}`,
                                     `{tempTableName}` TO `{originalTableName}`;
                    ";
                    using (var cmd = new MySqlCommand(swapTables, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine($"Swapped {originalTableName} with {tempTableName}.");

                    // Step 4: Drop the backup table
                    string dropBackupTable = $"DROP TABLE `{backupTableName}`;";
                    using (var cmd = new MySqlCommand(dropBackupTable, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine($"Dropped backup table {backupTableName}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during BulkInsertWithSwapAsync: {ex.Message}");
                    throw;
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
