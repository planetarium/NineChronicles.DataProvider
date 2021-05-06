namespace NineChronicles.DataProvider.Store
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Blocks;
    using Libplanet.Tx;
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

        public void PutTransaction<T>(Transaction<T> tx)
            where T : IAction, new()
        {
            InsertMany(
                "updated_address_references",
                new[] { "updated_address", "tx_id", "tx_nonce" },
                tx.UpdatedAddresses.Select(
                    addr => new object[]
                    {
                        addr.ToByteArray(), tx.Id.ToByteArray(), tx.Nonce,
                    }));
        }

        public void StoreTxReferences(TxId txId, in BlockHash blockHash,  long txNonce)
        {
            Insert("tx_references", new Dictionary<string, object>
            {
                ["tx_id"] = string.Empty,
                ["tx_nonce"] = string.Empty,
                ["block_hash"] = string.Empty,
            });
        }

        public void StoreSignerReferences()
        {
            Insert("signer_references", new Dictionary<string, object>
            {
                ["signer"] = string.Empty,
                ["tx_id"] = string.Empty,
                ["tx_nonce"] = string.Empty,
            });
        }

        public void StoreUpdatedAddressReferences()
        {
            Insert("updated_address_references", new Dictionary<string, object>
            {
                ["updated_address"] = string.Empty,
                ["tx_id"] = string.Empty,
                ["tx_nonce"] = string.Empty,
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

        private void InsertMany(
            string tableName,
            string[] columns,
            IEnumerable<object[]> data)
        {
            using QueryFactory db = OpenDb();
            try
            {
                db.Query(tableName).Insert(columns, data);
            }
            catch (MySqlException e)
            {
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
