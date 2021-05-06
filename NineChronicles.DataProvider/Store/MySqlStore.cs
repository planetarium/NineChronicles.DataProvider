namespace NineChronicles.DataProvider.Store
{
    using System.Collections.Generic;
    using MySqlConnector;
    using Serilog;
    using SqlKata.Compilers;
    using SqlKata.Execution;

    public class MySqlStore
    {
        private const string AgentsDbName = "agents";
        private const string AvatarsDbName = "avatars";
        private const string HackAndSlashDbName = "hack_and_slash";

        private readonly MySqlCompiler _compiler;
        private readonly string _connectionString;

        public MySqlStore(MySqlStoreOptions options)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Database = options.Database,
                UserID = options.Username,
                Password = options.Password,
                Server = options.Server,
                Port = options.Port,
            };

            _connectionString = builder.ConnectionString;
            _compiler = new MySqlCompiler();
        }

        public void StoreAvatar(string address)
        {
            Insert(AvatarsDbName, new Dictionary<string, object>
            {
                ["address"] = address,
            });
        }

        public void StoreAgent(string address)
        {
            Insert(AgentsDbName, new Dictionary<string, object>
            {
                ["address"] = address,
            });
        }

        public void StoreHackAndSlash(
            string avatarAddress,
            string agentAddress,
            string stageId,
            bool cleared)
        {
            Insert(HackAndSlashDbName, new Dictionary<string, object>
            {
                ["avatar_address"] = avatarAddress,
                ["agent_address"] = agentAddress,
                ["staged_id"] = stageId,
                ["cleared"] = cleared,
            });
        }

        private QueryFactory OpenDb() =>
            new QueryFactory(new MySqlConnection(_connectionString), _compiler);

        private void Insert(string tableName, IReadOnlyDictionary<string, object> data)
        {
            using QueryFactory db = OpenDb();
            try
            {
                db.Query(tableName).Insert(data);
            }
            catch (MySqlException e)
            {
                Log.Debug(e.ErrorCode.ToString());
                if (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                {
                    Log.Debug("Ignore DuplicateKeyEntry");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
