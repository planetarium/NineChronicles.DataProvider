namespace NineChronicles.DataProvider.GraphQL.Types
{
    using System.Linq;
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
            Field<IntGraphType>(
                name: "AgentCount",
                resolve: context =>
                {
                    var agentCount = Store.GetAgentCount();
                    return agentCount;
                });
            Field<ListGraphType<AgentType>>(
                name: "Agents",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "agentAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("agentAddress", null);
                    Address? agentAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetAgents(agentAddress);
                });
            Field<IntGraphType>(
                name: "AvatarCount",
                resolve: context =>
                {
                    var avatarCount = Store.GetAvatarCount();
                    return avatarCount;
                });
            Field<ListGraphType<AvatarType>>(
                name: "Avatars",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? avatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetAvatars(avatarAddress);
                });
            Field<IntGraphType>(
                name: "ShopEquipmentCount",
                resolve: context =>
                {
                    var shopEquipmentCount = Store.GetShopEquipmentCount();
                    return shopEquipmentCount;
                });
            Field<ListGraphType<ShopEquipmentType>>(
                name: "ShopEquipments",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopEquipments(sellerAvatarAddress);
                });
            Field<IntGraphType>(
                name: "ShopConsumableCount",
                resolve: context =>
                {
                    var shopConsumableCount = Store.GetShopConsumableCount();
                    return shopConsumableCount;
                });
            Field<ListGraphType<ShopConsumableType>>(
                name: "ShopConsumables",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopConsumables(sellerAvatarAddress);
                });
            Field<IntGraphType>(
                name: "ShopCostumeCount",
                resolve: context =>
                {
                    var shopCostumeCount = Store.GetShopCostumeCount();
                    return shopCostumeCount;
                });
            Field<ListGraphType<ShopCostumeType>>(
                name: "ShopCostumes",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopCostumes(sellerAvatarAddress);
                });
            Field<IntGraphType>(
                name: "ShopMaterialCount",
                resolve: context =>
                {
                    var shopMaterialCount = Store.GetShopMaterialCount();
                    return shopMaterialCount;
                });
            Field<ListGraphType<ShopMaterialType>>(
                name: "ShopMaterials",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopMaterials(sellerAvatarAddress);
                });
            Field<ListGraphType<BattleArenaRankingType>>(
                name: "BattleArenaRanking",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "championshipId" },
                    new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "round" },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "rankingType",
                        DefaultValue = "score",
                        Description = "Input \"Score\" or \"Medal\"",
                    },
                    new QueryArgument<IntGraphType> { Name = "limit" },
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" }
                ),
                resolve: context =>
                {
                    int championshipId = context.GetArgument<int>("championshipId" );
                    int round = context.GetArgument<int>("round" );
                    string rankingType = context.GetArgument<string>("rankingType", "Score");
                    int? limit = context.GetArgument<int?>("limit", null );
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? avatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetBattleArenaRanking(championshipId, round, rankingType, limit, avatarAddress);
                });
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
