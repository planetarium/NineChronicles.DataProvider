namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL;
    using global::GraphQL.Types;
    using Libplanet;
    using NineChronicles.DataProvider.Store;

    internal class NineChroniclesSummaryQuery : ObjectGraphType
    {
        public NineChroniclesSummaryQuery(MySqlStore store)
        {
            Store = store;
            Field<StringGraphType>(
                name: "test",
                resolve: context => "Should be done.");
            Field<ListGraphType<HackAndSlashType>>(
                name: "HackAndSlash",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "agentAddress" },
                    new QueryArgument<IntGraphType> { Name = "limit" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("agentAddress", null);
                    Address? agentAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    int? limit = context.GetArgument<int?>("limit", null);
                    return Store.GetHackAndSlash(agentAddress, limit);
                });
            Field<ListGraphType<StageRankingType>>(
                name: "StageRanking",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" },
                    new QueryArgument<IntGraphType> { Name = "limit" },
                    new QueryArgument<BooleanGraphType> { Name = "mimisbrunnr" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? avatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    int? limit = context.GetArgument<int?>("limit", null);
                    bool isMimisbrunnr = context.GetArgument<bool>("mimisbrunnr", false);
                    return Store.GetStageRanking(avatarAddress, limit, isMimisbrunnr);
                });
            Field<ListGraphType<CraftRankingType>>(
                name: "CraftRanking",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" },
                    new QueryArgument<IntGraphType> { Name = "limit" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? avatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    int? limit = context.GetArgument<int?>("limit", null);
                    return Store.GetCraftRanking(avatarAddress, limit);
                });
            Field<ListGraphType<EquipmentRankingType>>(
                name: "EquipmentRanking",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" },
                    new QueryArgument<StringGraphType> { Name = "itemSubType" },
                    new QueryArgument<IntGraphType> { Name = "limit" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? avatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    string? itemSubType = context.GetArgument<string?>("itemSubType", null);
                    int? limit = context.GetArgument<int?>("limit", null);
                    return Store.GetEquipmentRanking(avatarAddress, itemSubType, limit);
                });
            Field<ListGraphType<AbilityRankingType>>(
                name: "AbilityRanking",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" },
                    new QueryArgument<IntGraphType> { Name = "limit" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? avatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    int? limit = context.GetArgument<int?>("limit", null);
                    return Store.GetAbilityRanking(avatarAddress, limit);
                });
        }

        private MySqlStore Store { get; }
    }
}
