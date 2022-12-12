using System.Globalization;

namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Cocona;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.RocksDBStore;
    using Libplanet.Store;
    using MySqlConnector;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Serilog.Events;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class MySqlMigration
    {
        private string USDbName = "BattleGrandFinaleRanking";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _usBulkFile;
        private List<string> _usFiles;

        [Command(Description = "Migrate action data in rocksdb store to mysql db.")]
        public void Migration(
            [Option('o', Description = "Rocksdb path to migrate.")]
            string storePath,
            [Option(
                "rocksdb-storetype",
                Description = "Store type of RocksDb (new or mono).")]
            string rocksdbStoreType,
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
                "slack-token",
                Description = "slack token.")]
            string slackToken,
            [Option(
                "slack-channel",
                Description = "slack channel.")]
            string slackChannel,
            [Option(
                "offset",
                Description = "offset of block index (no entry will migrate from the genesis block).")]
            int? offset = null,
            [Option(
                "limit",
                Description = "limit of block count (no entry will migrate to the chain tip).")]
            int? limit = null,
            [Option(
                "tipIndex",
                Description = "tipIndex of chain.")]
            long? tipIndex = null
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

            Console.WriteLine("Setting up RocksDBStore...");
            if (rocksdbStoreType == "new")
            {
                _baseStore = new RocksDBStore(
                    storePath,
                    dbConnectionCacheSize: 10000);
            }
            else
            {
                throw new CommandExitedException("Invalid rocksdb-storetype. Please enter 'new' or 'mono'", -1);
            }

            long totalLength = _baseStore.CountBlocks();

            if (totalLength == 0)
            {
                throw new CommandExitedException("Invalid rocksdb-store. Please enter a valid store path", -1);
            }

            if (!(_baseStore.GetCanonicalChainId() is Guid chainId))
            {
                Console.Error.WriteLine("There is no canonical chain: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            if (!(_baseStore.IndexBlockHash(chainId, 0) is { } gHash))
            {
                Console.Error.WriteLine("There is no genesis block: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            // Setup base store
            RocksDBKeyValueStore baseStateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            TrieStateStore baseStateStore =
                new TrieStateStore(baseStateKeyValueStore);

            // Setup block policy
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();
            LogEventLevel logLevel = LogEventLevel.Debug;
            var blockPolicySource = new BlockPolicySource(Log.Logger, logLevel);
            IBlockPolicy<NCAction> blockPolicy = blockPolicySource.GetPolicy();

            // Setup base chain & new chain
            Block<NCAction> genesis = _baseStore.GetBlock<NCAction>(gHash);
            _baseChain = new BlockChain<NCAction>(blockPolicy, stagePolicy, _baseStore, baseStateStore, genesis);

            // Prepare block hashes to append to new chain
            long height = _baseChain.Tip.Index;
            if (offset + limit > (int)height)
            {
                Console.Error.WriteLine(
                    "The sum of the offset and limit is greater than the chain tip index: {0}",
                    height);
                Environment.Exit(1);
                return;
            }

            Console.WriteLine("Start migration.");

            // files to store bulk file paths (new file created every 10000 blocks for bulk load performance)
            _usFiles = new List<string>();

            CreateBulkFiles();

            using MySqlConnection connection = new MySqlConnection(_connectionString);
            connection.Open();

            var stm = "SELECT `Address` from Avatars";
            var cmd = new MySqlCommand(stm, connection);

            var rdr = cmd.ExecuteReader();
            List<string> avatars = new List<string> { "5Ea5755eD86631a4D086CC4Fae41740C8985F1B4", "e647582faBF2D5904520b2587084b9d87E350EF0", "01A0b412721b00bFb5D619378F8ab4E4a97646Ca", "4Eee42F9d7B9A7cA49c3C63B8A422313C42346ab", "7d58F6271297f66Ee2E68EFaF094bDE796d01BC7", "074F9A35ea4f47deB1dAC246Ce438053FaDCfe83", "1a155C59a73D024a70Ca9fe280ba719359072170", "a5DED46B4465593d804D2b35a3DFB311B6470afC", "aB44635462880666dAa7f2Be5a21c71C1590fF2B", "39a3609566Fa4888cf3Db68a4972A204884a656b", "F15D23B414dd00Bac8C14A9d943B11E8A8a06EbB", "77390AD2518De1D7Bb6e1BF3b6Dcec3a2cfa65e2", "A222Ac97A3dac14E743dd9fEDF3f4a7E28e15011", "7386c20482D68B52DD6A7003a3f58c1F3F4161Aa", "93B36E9DB49E289F80483c336a0dC3b4bf2cb41d", "bded05BFE1A606ba9F84013925601e80E0c0F68D", "FD3Ac1723aF2AE1d8147c4b13b24C718A20693Cd", "27D09b1898E0A6f22D04Ec656D8d811b4d14a81b", "5b65f5D0e23383FA18d74A62FbEa383c7D11F29d", "c596C30D82813DA5Fbfcc22Da8F18Dd381b81E03", "B260C59Ee2F14f0212AfFCc454196133E6622a4a", "b2eB00B1d3613b630ACb81EA81a7198c5CCC6BE5", "2DB02d98129DaAcBB9730E5147f3FE10505f8D65", "D8a8322ac3ec1DB363225E3a0e0e1F956D65eeF3", "046390987a424292bd44Ba7b92cB2CdDFEc7213e", "0e9D5F7429F588086b6135669E85d26b5d889D12", "68292FDf511FC419913CaAa3237D6A431ec0e6EF", "dB17600c9eAc7962b746Aa93E900BA1b7D2ad51E", "Dc61990d14ed397dE08901004d5FC11f83366275", "76cC2746ac35EADcc16B08183ce7194EED277947", "3CCF8781c0c55aE2f882f429aDC990C9BcE30E9f", "0B3BB3150B70317E51B5Ae6AaF478A614e2db46d", "1F8d5e0D201B7232cE3BC8d630d09E3F9107CceE", "549c2B6806d94a510E74A01051948205A007BeE3", "d0981D215AFCbc3130076989821DA118a1b134C1", "00fEaD3878CE724deac3286e6EF3960feA83E370", "3b7a47daAece48807Fc00a310B05BD9f5D26736e", "b01f6De457098901535214FBCcD3748C0d2708ed", "d0190D103ecC827F6145C9aa79d9BB8CCBEeE875", "2Dc7bF735E754069287d0c7093bf64915B8678Dc", "f3B82E4571476C70A836B3a217FC1D34E4e6C725", "C61b1b06D215aA369Df320B9907E90fa9aAe1539", "9e4405DFd2b25d31a491Fe693f08229cd6490804", "Dc0D99002300a41739993cF91181A5A25F10bb5c", "017f5Dd20ca19Ff3E3670E4f20497F8dD26aF212", "6dbFFeEC4d103f5aDa4c7050249FF877C0cb20B3", "958a5F1AA335F015bFc9982A99Fe6a802a876819", "bD4Af00c622420b2C506C3401A68BCC282df3eAB", "d4DB6b284434fc37568DFb78eDc4f11331F9ec74", "3E99CeAed6a963895d8354D015D4fef3fD5EB4e7", "Fe624725cAc2eebb70c2634fC7a2eaabACC8f71C", "6eB870B6dd1ecf8693442f9dA0FbaE14D731d365", "7C571a11f7cCBd54934596a6E3fbd18EF64a812d", "211eeeA14d0a1Bb7DB6bccCe57c676F301695Ee6", "c70BE7465318a4ec1E547E0D7b5e2758C868c017", "B564A8391f2b2B7f1ffFd532353d25a7Dc3D6432", "D2Fd1371B2DFc6aEBEC8813bf96FA5D0Fe6e5ED0", "d80BAa7C672D0F473BceF19aed95d89D0427803d", "15D001bBA3F3bd8D9FBb28F3847f78760B37A17D", "e97fb5c0Dd8C63acDadBcD51fF8dFe9c80eE1B5c", "CFFA68cc0249849eAeab555753e262c240dFCb6a", "83D1Ce98Cc3821766b0Cb3963513F50200A84b0E", "928c09EaB804daDe4C8180945DF87b36d8d93dDE", "4eAd446eAF8b3f406C7b03539f9Ccef4721f25b7", "727208F580Dca7c5af9b375532411f1d4e59c2e2", "014decb10fB3f3F37F242851b6CBd41111b711c8", "017c1a566E21094091ceb6C2866E9B533F4c1f90", "6D58077485Af46Cd9C19c2D17c72db0312C33B55", "732a63542e63Ad6c35C44959272c4094F49f9871", "D8dB2304665b932C636498215F6f4df003C73ea1", "682Cb11142AD298Fd875F4D4881078D1b6CF28db", "6ACEeE8FF0f86880480D0c012Eef816818e96bAa", "829a8a8C2A140AE1f303370Ef9E09DE44D13bef3", "5c925B7A9e756a1a26913A277A7CB757286a3726", "Edf9da3e82e38271b7c84Cf04f2A052C53DA92e3", "f12881C77740f38a01000A20B7c934955e17452A", "5b05a3cDF7e53845e5B9939C7668dd3Af6248Fef", "f86881a2A01fc155772083ec8C55b56A710B53AB", "259e026771BFC59fB5BC17c20A2e6512f3c26c3A", "88eC7e7fDBFf202b024C108c0E205C0A7a6b5DDb", "514708D19753a3589548ec05bfA5787D66eBab37", "2cbFCB6fECbCaF3D6E3dfe9290Cd393a143eb23B", "c38cB6Df217F7409ce6BB010C2e48C29CB3718a3", "DD21DCc1A9d393550A49999363e383c8409Ee1A2", "9481CbFE326e323Faf3Dd22FB8Ca043659A43662", "0278B2cA3a7970fE27bfd0506d01d0d1d20cB3a6", "b56e5726F37681F4d2D6D72B37403f47a5379c55", "C7Cf617516c01d3bb6AE20707dE52B81113FC790", "267C74fb329E3E6d5CDB0E222D083900CCc0FeEC", "f5C647ca1e7AcE9748FA90d4e4fd9D34c6799A78", "B25640eB9DC8b30e0F8Dd8460A8C73c953A40a61", "ca9725C9b4c7C47C3F319cC5db8E849c908Fc719", "BBF95e33aAC9E7e225Cc5137Dd0039D43ba9fcB8", "315D78E0F8882212524646dE4632e38B29f42066", "995B5D1E1259fA75b7D8DC2cb741819e742C1488", "EFF48b0F457034Ec7D929c025cc8c36F3E587dD4", "5D233feD43d094502642F792ee9b383A952727bF", "1c6B8583a13658DB93E75E08d0256a716A223331", "d0e3E2902f5c71d36Ce88195fB4f170D3A0a48F8", "17e21ea1d7B3963B93b7c33d722033D377a8748d" };

            connection.Close();
            int shopOrderCount = 0;
            _usBulkFile.WriteLine(
                     "BlockIndex;" +
                     "AgentAddress;" +
                     "AvatarAddress;" +
                     "AvatarLevel;" +
                     "AvatarCp;" +
                     "MaxGrandFinaleScore"
                 );

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, tipIndex ?? _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock<NCAction>((BlockHash)tipHash);
                var exec = _baseChain.ExecuteActions(tip);
                var ev = exec.Last();
                var avatarCount = 0;
                AvatarState avatarState;
                int interval = 1000000;
                int intervalCount = 0;
                bool checkBARankingTable = false;

                foreach (var avatar in avatars)
                {
                    try
                    {
                        intervalCount++;
                        avatarCount++;
                        Console.WriteLine("Interval Count {0}", intervalCount);
                        Console.WriteLine("Migrating {0}/{1}", avatarCount, avatars.Count);
                        var avatarAddress = new Address(avatar);
                        try
                        {
                            avatarState = ev.OutputStates.GetAvatarStateV2(avatarAddress);
                        }
                        catch (Exception ex)
                        {
                            avatarState = ev.OutputStates.GetAvatarState(avatarAddress);
                        }

                        if (!checkBARankingTable)
                        {
                            USDbName = $"{USDbName}_{tip.Index}";
                            var stm33 =
                            $@"CREATE TABLE IF NOT EXISTS `data_provider`.`{USDbName}` (
                              `BlockIndex` bigint NOT NULL,
                              `AgentAddress` varchar(100) NOT NULL,
                              `AvatarAddress` varchar(100) NOT NULL,
                              `AvatarLevel` int NOT NULL,
                              `AvatarCp` int NOT NULL,
                              `MaxGrandFinaleScore` int NOT NULL,
                              `Timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                            var cmd33 = new MySqlCommand(stm33, connection);
                            connection.Open();
                            cmd33.CommandTimeout = 300;
                            cmd33.ExecuteScalar();
                            connection.Close();
                            checkBARankingTable = true;
                        }

                        var scoreAddress = avatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, BattleGrandFinale.ScoreDeriveKey, 1));
                        ev.OutputStates.TryGetState(scoreAddress, out Integer outputGrandFinaleScore);
                        if (outputGrandFinaleScore.Value > 0)
                        {
                            var characterSheet = ev.OutputStates.GetSheet<CharacterSheet>();
                            var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                            _usBulkFile.WriteLine(
                                $"{tip.Index};" +
                                $"{avatarState.agentAddress.ToString()};" +
                                $"{avatarAddress.ToString()};" +
                                $"{avatarState.level};" +
                                $"{avatarCp};" +
                                $"{outputGrandFinaleScore.Value.ToString()}"
                            );
                        }

                        Console.WriteLine("Migrating Complete {0}/{1}", avatarCount, avatars.Count);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);

                var stm11 = $"DROP TABLE IF EXISTS {USDbName}_Dump;";
                var cmd11 = new MySqlCommand(stm11, connection);
                connection.Open();
                cmd11.CommandTimeout = 300;
                cmd11.ExecuteScalar();
                connection.Close();

                var stm12 = $"RENAME TABLE {USDbName} TO {USDbName}_Dump; CREATE TABLE {USDbName} LIKE {USDbName}_Dump;";
                var cmd12 = new MySqlCommand(stm12, connection);
                var startMove = DateTimeOffset.Now;
                connection.Open();
                cmd12.CommandTimeout = 300;
                cmd12.ExecuteScalar();
                connection.Close();
                var endMove = DateTimeOffset.Now;
                Console.WriteLine("Move BattleGrandFinaleRanking Complete! Time Elapsed: {0}", endMove - startMove);
                var i = 1;
                foreach (var path in _usFiles)
                {
                    string oldFilePath = path;
                    string newFilePath = Path.Combine(Path.GetTempPath(), $"BattleGrandFinaleRanking{tip.Index}#{i}.csv");
                    if (File.Exists(newFilePath))
                    {
                        File.Delete(newFilePath);
                    }

                    File.Copy(oldFilePath, newFilePath);
                    UploadFileAsync(
                        slackToken,
                        newFilePath,
                        slackChannel
                    ).Wait();

                    BulkInsert(USDbName, path);
                    i += 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Restoring previous tables due to error...");
                var stm17 = $"DROP TABLE {USDbName}; RENAME TABLE {USDbName}_Dump TO {USDbName};";
                var cmd17 = new MySqlCommand(stm17, connection);
                var startRestore = DateTimeOffset.Now;
                connection.Open();
                cmd17.CommandTimeout = 300;
                cmd17.ExecuteScalar();
                connection.Close();
                var endRestore = DateTimeOffset.Now;
                Console.WriteLine("Restore BattleGrandFinaleRanking Complete! Time Elapsed: {0}", endRestore - startRestore);
            }

            var stm18 = $"DROP TABLE {USDbName}_Dump;";
            var cmd18 = new MySqlCommand(stm18, connection);
            var startDelete = DateTimeOffset.Now;
            connection.Open();
            cmd18.CommandTimeout = 300;
            cmd18.ExecuteScalar();
            connection.Close();
            var endDelete = DateTimeOffset.Now;
            Console.WriteLine("Delete BattleGrandFinaleRanking_Dump Complete! Time Elapsed: {0}", endDelete - startDelete);

            DateTimeOffset end = DateTimeOffset.UtcNow;
            Console.WriteLine("Migration Complete! Time Elapsed: {0}", end - start);
            Console.WriteLine("Shop Count for {0} avatars: {1}", avatars.Count, shopOrderCount);
        }

        private void FlushBulkFiles()
        {
            _usBulkFile.Flush();
            _usBulkFile.Close();
        }

        private void CreateBulkFiles()
        {

            string usFilePath = Path.GetTempFileName().Replace(".tmp", ".csv");;
            _usBulkFile = new StreamWriter(usFilePath);
            _usFiles.Add(usFilePath);
        }

        public static async Task UploadFileAsync(string token, string path, string channels)
        {
            HttpClient client = new HttpClient();
            // we need to send a request with multipart/form-data
            var multiForm = new MultipartFormDataContent();

            // add API method parameters
            multiForm.Add(new StringContent(token), "token");
            multiForm.Add(new StringContent(channels), "channels");

            // add file and directly upload it
            FileStream fs = File.OpenRead(path);
            multiForm.Add(new StreamContent(fs), "file", Path.GetFileName(path));

            // send request to API
            var url = "https://slack.com/api/files.upload";
            var response = await client.PostAsync(url, multiForm);

            // fetch response from API
            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseJson);
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
                    NumberOfLinesToSkip = 1,
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
                    NumberOfLinesToSkip = 1,
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
