namespace NineChronicles.DataProvider.GraphQL.Types
{
    using Bencodex.Types;
    using global::GraphQL;
    using global::GraphQL.Types;
    using Libplanet;
    using Nekoyume;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    internal class NineChroniclesSummaryQuery : ObjectGraphType
    {
        public NineChroniclesSummaryQuery(MySqlStore store, StandaloneContext standaloneContext)
        {
            Store = store;
            StandaloneContext = standaloneContext;
            Field<ListGraphType<WorldBossType>>(
                name: "WorldBosses",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" },
                    new QueryArgument<IntGraphType> { Name = "limit" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? agentAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    int? limit = context.GetArgument<int?>("limit", null);
                    return Store.GetWorldBosses(agentAddress, limit);
                });
        }

        private MySqlStore Store { get; }

        private StandaloneContext StandaloneContext { get; }
    }
}
