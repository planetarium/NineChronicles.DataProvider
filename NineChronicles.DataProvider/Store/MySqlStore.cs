namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;
    using MySqlConnector;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;
    using SqlKata.Compilers;
    using SqlKata.Execution;

    public class MySqlStore
    {
        private const string AgentsDbName = "agent";
        private const string AvatarsDbName = "avatar";
        private const string HackAndSlashDbName = "hack_and_slash";

        private readonly MySqlCompiler _compiler;
        private readonly string _connectionString;

        public MySqlStore(IOptions<Configuration> option)
            : this(option.Value.MySqlConnectionString)
        {
        }

        public MySqlStore(string connectionString)
        {
            _connectionString = connectionString;
            _compiler = new MySqlCompiler();
        }

        public void StoreAvatar(
            string address,
            string agentAddress,
            string name)
        {
            Insert(AvatarsDbName, new Dictionary<string, object>
            {
                ["address"] = address,
                ["agent_address"] = agentAddress,
                ["name"] = name,
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
            string agentAddress,
            string avatarAddress,
            int stageId,
            bool cleared,
            string blockHash)
        {
            Insert(HackAndSlashDbName, new Dictionary<string, object>
            {
                ["agent_address"] = agentAddress,
                ["avatar_address"] = avatarAddress,
                ["stage_id"] = stageId,
                ["cleared"] = cleared,
                ["block_hash"] = blockHash,
            });
        }

        public void DeleteHackAndSlash(
            string agentAddress,
            string avatarAddress,
            int stageId,
            bool cleared)
        {
            Delete(HackAndSlashDbName, new Dictionary<string, object>
            {
                ["agent_address"] = agentAddress,
                ["avatar_address"] = avatarAddress,
                ["stage_id"] = stageId,
                ["cleared"] = cleared,
            });
        }

        public IEnumerable<HackAndSlashModel> GetHackAndSlash(
            string? agentAddress = null,
            int? limit = null)
        {
            using QueryFactory db = OpenDb();
            var query = db.Query(HackAndSlashDbName);
            if (agentAddress is { })
            {
                query = query.Where("agent_address", agentAddress);
            }

            if (limit is int limitNotNull)
            {
                query = query.Limit(limitNotNull);
            }

            return query.Get<HackAndSlashModel>();
        }

        public IEnumerable<StageRankingModel> GetStageRanking(
            int? limit = null)
        {
            try
            {
                using QueryFactory db = OpenDb();
                var query = db.Query(HackAndSlashDbName)
                    .Select("avatar_address as AvatarAddress")
                    .SelectRaw($"(select name from {AvatarsDbName} where address = avatar_address) as Name")
                    .SelectRaw("Max(stage_id) as ClearedStageId")
                    .Where("cleared", true)
                    .GroupBy("avatar_address")
                    .OrderByDesc("stage_id");

                if (limit is int limitNotNull)
                {
                    query = query.Limit(limitNotNull);
                }

                var stageRankingList = query.Get<StageRankingModel>();
                return stageRankingList;
            }
            catch (MySqlException e)
            {
                Log.Debug("MySql Error: {0}", e.Message);
                throw;
            }
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
                Log.Debug("MySql Error: {0}", e.Message);
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

        private void Delete(string tableName, IReadOnlyDictionary<string, object> data)
        {
            using QueryFactory db = OpenDb();
            var query = db.Query(tableName);
            try
            {
                foreach (KeyValuePair<string, object> pair in data)
                {
                    query = query.Where(pair.Key, pair.Value);
                }

                query.Delete();
            }
            catch (MySqlException e)
            {
                Log.Debug(e.Message);
                throw;
            }
        }
    }
}
