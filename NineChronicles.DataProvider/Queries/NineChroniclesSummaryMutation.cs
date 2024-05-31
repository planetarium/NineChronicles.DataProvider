namespace NineChronicles.DataProvider.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

            Field<NonNullGraphType<BooleanGraphType>>(
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
                    MigrateMoca(mocas, Store, collectionSheet, blockIndex, StateContext);
                    return true;
                }
            );
        }

        private MySqlStore Store { get; }

        private StateContext StateContext { get; }

        public static void MigrateMoca(ICollection<MocaIntegrationModel> mocas, MySqlStore mySqlStore, CollectionSheet collectionSheet, long blockIndex, StateContext stateContext)
        {
            foreach (var moca in mocas)
            {
                var avatars = mySqlStore.GetAvatarsFromSigner(moca.Signer);
                var migrated = true;
                foreach (var avatar in avatars)
                {
                    try
                    {
                        var collectionState = stateContext.WorldState.GetCollectionState(new Address(avatar.Address!));
                        var existIds = avatar.ActivateCollections.Select(i => i.CollectionId);
                        var targetIds = collectionState.Ids.Except(existIds).ToList();
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
                        }
                    }
                    catch (Exception e)
                    {
                        migrated = false;
                        Log.Error(e, "Unexpected exception occurred during MocaWorker: {Exc}", e);
                    }
                }

                if (migrated)
                {
                    moca.Migrated = true;
                    mySqlStore.UpdateMoca(moca.Signer);
                }
            }
        }
    }
}
