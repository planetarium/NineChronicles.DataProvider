namespace NineChronicles.DataProvider.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using GraphQL;
    using GraphQL.Types;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
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
                name: "migrateActivateCollections",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "signers",
                    }
                ),
                resolve: context =>
                {
                    var signers = context.GetArgument<List<string>>("signers");
                    var collectionSheet = StateContext.WorldState.GetSheet<CollectionSheet>();
                    var blockIndex = Store.GetTip();
                    var result = new Dictionary<string, (string, int, int)>();
                    foreach (var signer in signers)
                    {
                        var avatars = Store.GetAvatarsFromSigner(signer);
                        foreach (var avatar in avatars)
                        {
                            try
                            {
                                var collectionState = stateContext.WorldState.GetCollectionState(new Address(avatar.Address!));
                                var previous = RenderSubscriber.MigrateActivateCollections(Store, collectionSheet, blockIndex, collectionState, avatar, "Migrate from worker");
                                result[avatar.Address!] = (signer, previous, avatar.ActivateCollections.Count);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, "[MigrateActivateCollections] Unexpected exception occurred during MocaWorker: {Exc}", e);
                            }
                        }
                    }

                    if (result.Any())
                    {
                        StringBuilder sb = new StringBuilder("[MigrateActivateCollections]migration result");
                        sb.AppendLine("signer,avatar,previous,migrated");
                        foreach (var kv in result)
                        {
                            var value = kv.Value;
                            var log = $"{value.Item1},{kv.Key},{value.Item2},{kv.Value.Item3}";
                            sb.AppendLine(log);
                        }

                        return sb.ToString();
                    }

                    return "[MigrateActivateCollections]no required migrations";
                }
            );
        }

        private MySqlStore Store { get; }

        private StateContext StateContext { get; }
    }
}
