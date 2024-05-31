namespace NineChronicles.DataProvider.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using GraphQL;
    using GraphQL.Types;
    using Libplanet.Crypto;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless.GraphTypes.States;
    using Serilog;

    public class NineChroniclesSummaryMutation : ObjectGraphType
    {
        public NineChroniclesSummaryMutation(MySqlStore store, StateContext stateContext)
        {
            Store = store;
            StateContext = stateContext;

            Field<NonNullGraphType<StringGraphType>>(
                name: "migrateMoca",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "signers",
                    }
                ),
                resolve: context =>
                {
                    var signers = context.GetArgument<List<string>>("signers");
                    var mocas = Store.GetMocasBySigner(signers);
                    var collectionSheet = StateContext.WorldState.GetSheet<CollectionSheet>();
                    var blockIndex = Store.GetTip();
                    var result = MigrateMoca(mocas, Store, collectionSheet, blockIndex, StateContext);
                    StringBuilder sb = new StringBuilder(string.Empty);
                    foreach (var kv in result)
                    {
                        var log = $"{kv.Key},{kv.Value.Item1},{kv.Value.Item2}\n";
                        sb.Append(log);
                    }

                    return sb;
                }
            );
        }

        private MySqlStore Store { get; }

        private StateContext StateContext { get; }

        public static Dictionary<string, (int, int)> MigrateMoca(ICollection<MocaIntegrationModel> mocas, MySqlStore mySqlStore, CollectionSheet collectionSheet, long blockIndex, StateContext stateContext)
        {
            var result = new Dictionary<string, (int, int)>();
            foreach (var moca in mocas)
            {
                var avatars = mySqlStore.GetAvatarsFromSigner(moca.Signer);
                var migrated = true;
                foreach (var avatar in avatars)
                {
                    try
                    {
                        var collectionState = stateContext.WorldState.GetCollectionState(new Address(avatar.Address!));
                        var existIds = avatar.ActivateCollections.Select(i => i.CollectionId).ToList();
                        var targetIds = collectionState.Ids.Except(existIds).ToList();
                        Log.Information("[MigrateMoca] migration targets: {Address}, [{ExistIds}]/[{TargetIds}]",  avatar.Address, string.Join(",", existIds), string.Join(",", targetIds));
                        var previous = avatar.ActivateCollections.Count;
                        foreach (var collectionId in targetIds)
                        {
                            var row = collectionSheet[collectionId];
                            var options = new List<CollectionOptionModel>();
                            foreach (var modifier in row.StatModifiers)
                            {
                                var option = new CollectionOptionModel
                                {
                                    StatType = modifier.StatType.ToString(),
                                    OperationType = modifier.Operation.ToString(),
                                    Value = modifier.Value,
                                };
                                options.Add(option);
                            }

                            var collectionModel = new ActivateCollectionModel
                            {
                                ActionId = "Migrate from worker",
                                Avatar = avatar,
                                BlockIndex = blockIndex,
                                CollectionId = collectionId,
                                Options = options,
                            };
                            avatar.ActivateCollections.Add(collectionModel);
                        }

                        if (targetIds.Any())
                        {
                            mySqlStore.UpdateAvatar(avatar);
                            Log.Information("[MigrateMoca] Update Avatar: {Address}, [{Previous}]/[{New}]",  avatar.Address, previous, avatar.ActivateCollections.Count);
                        }

                        result[avatar.Address!] = (previous, avatar.ActivateCollections.Count);
                    }
                    catch (Exception e)
                    {
                        migrated = false;
                        Log.Error(e, "[MigrateMoca] Unexpected exception occurred during MocaWorker: {Exc}", e);
                    }
                }

                if (migrated)
                {
                    moca.Migrated = true;
                    mySqlStore.UpdateMoca(moca.Signer);
                }
            }

            return result;
        }
    }
}
