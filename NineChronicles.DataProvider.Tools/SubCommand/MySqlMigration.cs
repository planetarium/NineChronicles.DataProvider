namespace NineChronicles.DataProvider.Tools.SubCommand
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
        private const string RSDbName = "Raiders";
        private string _connectionString;
        private IStore _baseStore;
        private BlockChain<NCAction> _baseChain;
        private StreamWriter _usBulkFile;
        private List<string> _agentList;
        private List<string> _avatarList;
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
                "offset",
                Description = "offset of block index (no entry will migrate from the genesis block).")]
            int? offset = null,
            [Option(
                "limit",
                Description = "limit of block count (no entry will migrate to the chain tip).")]
            int? limit = null
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
            if (offset + limit > (int) height)
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

            // lists to keep track of inserted addresses to minimize duplicates
            _agentList = new List<string>();
            _avatarList = new List<string>();

            CreateBulkFiles();

            string[] avatars =
            {
                "885d2Df2b1fbC8AF590507f032f1FbBFa66a9D5C", "5d471c044A77EC9b095c08505f763e52ce5B525C",
                "f74cf292E000d1b3cc03eF7cd67E22b1288c1b19", "FAec6f5Ac4d864b1A30A8589E93f1581d956F3C9",
                "B33F83e510cB3976902D556386c77B49B9E6a3C0", "b4e58b8c83fC78aE90a6011745b69AFe098f0AeD",
                "3a4e4814ba1043c8F1AB7DBCc47ae904F3CaD304", "fc32F14C6b657A995262ff8617d34efc6c5443EB",
                "Ba72a2C328604300F704C6576705c9866Ea7b925", "108a6383b6D8183C0636d19b3e65Cf0a98CcCB54",
                "5662bac1c2C0d862d3d8dd8cbd5fCf19ab2DD13E", "fCAc74eB215868D1Bb72540435Bf72eF39F4C46c",
                "e494f523502bD209c3Bf262FE494eA3E0d19CB5c", "981329A882Ead7F59676818A0C9051f850BEDBBb",
                "9b2F2063cA925A2b57B7c6A91C1A75332e0008e4", "dDAf237dE3563E6896080E67cB9baE15a231a52D",
                "A49C03c9F1A6998d6EF4052B5E8e60d7690d15AB", "098F58cf89F2888AFe3BE163C6311C9356650084",
                "31A3034ad896F921A14D7B1948c85A384087Ac82", "b79462d5370A76687f3eAa83370410Ba701c5443",
                "06df6c22a02d6D3F805649CCf7f91c09e3CD1b72", "41275720f56695Bfc46B82BbC60de669d7d4c087",
                "3B354DaE8Ff952E130C35b8Bcd98C6fDf5aD38bb", "8C4C27cE914CCFFc63e414C8c92FDDF35498CA02",
                "a23c86BD9A397b46635146383933B24169c8B978", "EAeE628BBaba2b04f52B33433cB069CbFBB6b50a",
                "1F8d5e0D201B7232cE3BC8d630d09E3F9107CceE", "99c8db1f9Cc8B32955b8f44788FF70e383e2E13d",
                "4b0D006e612D58D5fEF74b17833372768C89589d", "527723409B155868133E6FceB933bF1c2208704A",
                "Ad48A5904C76469a10a5cd1E87006Cf455AfaA64", "5F0c5A7b6BeeeD457A04658f7F1d76Ec27559524",
                "cb6c504d80bAF555Fe7FB58200432269c7a9AFD0", "31AF6a594657422c8224b4cB1CeCF7E87393329c",
                "d7db1051fDb992f6Ac31F071f07457B85C38e909", "cB1466d1DF765fcd8E4C8D64eE52A7F53EbeD7E5",
                "E5c884810427117140952CD555e44368665a40a4", "5C806D156e2d19730293E2Ece8b88F7B860c4be2",
                "3767e8E4BE572E8267a9a6b88209c513E7d74EE1", "A5CE19ab0cc35D53E85A5C428FD109F39987D674",
                "CFAf499Ba7cF15abaC0849e9917bc902427Ff7A8", "1387513990B14CE160C7018244cfe07d0345Db29",
                "28aC828cCA4870819086E65A2bADf0f98ac35d30", "DD898B663d52DeE134C4c355570bF7D420752c08",
                "26380C6A7Fa8c6Ff5D8ca178579Bd20FEccc0F72", "0BDb2F0884f04632cD727C49bC05D65CA612fA7f",
                "7328d7C711a80b0F531a74A53c496991D0250278", "57eC93588BcD360E1E7C0A7063A85636BFa0b1B5",
                "48BacAEC6B0c067bA430Cc0381DB21582d540Ef8", "B3C8C3AC133b1d25F67A6B3f4290D4C870998338",
                "3D958a3919cAa61a3f3b9a83763d103730e9f926", "C41c6854FfA040BC240cBEf45aaf34687727cF18",
                "f775558f78DB268A1cD220ad7e929635047326Ca", "78db93320885a64e779fffcB0d7e1169891Dab76",
                "3E82d09d4243a558d8e17F37173a5C2a74568c72", "FD99Bdef5CD93793454d5b880A18cC19b0e4151d",
                "79C395B6f7bF32c2F1303a4E86f1f32C1d615aA5", "7977c2d6AC600857b7e17C61222017Dcb0972257",
                "4725C9b8b5f5d79f52c11A6eB030a990652f8aAE", "d7115d1640feF9297ACd44de5C6A98E965B54D13",
                "431F4EAE644c9a0B161F35Fe413E2792D8fF8818", "2874137efB58668e65335e2Ee4FE7E00703cF3FC",
                "490B425cC4b4A98cc8B47C25fb83C26937F14e12", "3bFE97FF76D650a97079ef87aBafde0dc506FA34",
                "FbECCcdAC80Abca9426647b4791D9165284C592C", "9908fFCD85eaf633DB9B82a752a23081C841aB8b",
                "58f8D5Da9E2f9f04C3a1eABcAeb78214FAcA4026", "d1559d4b078fB0A38D7Ba1602dEB56d12e95F691",
                "dF5f80b2340FA988BCbeb19817617AF6ceB727AA", "f9aeF589ed168a0D0Eb88f8549852d610D783e07",
                "504D6459f4Cad06a22A87CCb8b8208B05F6cA905", "8dc38a54A57CB29e68657cdbAa3624C6C38e1C15",
                "C8531C7B17Fd5a61aFe66828b41eF3669E07183c", "2038f330451470b206903B45016102f399f2Cb0a",
                "129181f5cC503e4B52be14FD8d972789dC2D3278", "8de26c0e51CC170f1eC29aF4583E66b866Ac7fEC",
                "259D3179beD313DB24105C796Cc121DB49E9EE4e", "2b18133c9D3907D338BC6c3d5DcA35917ed3389F",
                "6c072fe7Ad919C7a93e248Df6d38f8E476300e66", "9eBe859e6E5C13dab963F4ACB914Ff12F3808C4E",
                "1fB143746426b1DAEe4953b2359c1fA48b43c1AC", "953Bd0911D8a1Ea2715B123ec36F39cE15e6326d",
                "0970ee7bd43D4419103f07677421687d62849745", "0965014F464ead9fCA7F67fDE2C23c9C68bE0023",
                "e6ac8403702eFDE6bEE3DFA22e36928b1ba39A94",
            };

            try
            {
                var tipHash = _baseStore.IndexBlockHash(_baseChain.Id, _baseChain.Tip.Index);
                var tip = _baseStore.GetBlock<NCAction>((BlockHash) tipHash);
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
                        Console.WriteLine("Migrating {0}/{1}", avatarCount, avatars.Length);
                        var avatarAddress = new Address(avatar);
                        RaiderState raiderState =
                            ev.OutputStates.GetRaiderState(new Address(avatar), 1);

                        Console.WriteLine("Migrating Complete {0}/{1}", avatarCount, avatars.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                FlushBulkFiles();
                DateTimeOffset postDataPrep = DateTimeOffset.Now;
                Console.WriteLine("Data Preparation Complete! Time Elapsed: {0}", postDataPrep - start);

                Console.WriteLine("Move Raiders Complete!");
                foreach (var path in _usFiles)
                {
                    BulkInsert(RSDbName, path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void FlushBulkFiles()
        {

            _usBulkFile.Flush();
            _usBulkFile.Close();
        }

        private void CreateBulkFiles()
        {
            string usFilePath = Path.GetTempFileName();
            _usBulkFile = new StreamWriter(usFilePath);

            _usFiles.Add(usFilePath);
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
