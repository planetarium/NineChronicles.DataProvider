﻿namespace NineChronicles.DataProvider.Queries
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using GraphQL;
    using GraphQL.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Libplanet.Explorer.GraphTypes;
    using Nekoyume;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.GraphTypes;
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
            Field<StringGraphType>(
                name: "test",
                resolve: context => "Should be done.");
            Field<BattleArenaInfoType>(
                name: "BattleArenaInfo",
                arguments: new QueryArguments(
                    new QueryArgument<LongGraphType> { Name = "index" }
                ),
                resolve: context =>
                {
                    long index = context.GetArgument<long>("index", StandaloneContext.BlockChain!.Tip.Index);
                    var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
                    IValue state = StandaloneContext.BlockChain!.GetState(arenaSheetAddress);
                    ArenaSheet arenaSheet = new ArenaSheet();
                    arenaSheet.Set((Bencodex.Types.Text)state);
                    var arenaData = arenaSheet!.GetRoundByBlockIndex(index);
                    var battleArenaInfo = new BattleArenaInfoModel()
                    {
                        ChampionshipId = arenaData.ChampionshipId,
                        Round = arenaData.Round,
                        ArenaType = arenaData.ArenaType.ToString(),
                        StartBlockIndex = arenaData.StartBlockIndex,
                        EndBlockIndex = arenaData.EndBlockIndex,
                        RequiredMedalCount = arenaData.RequiredMedalCount,
                        EntranceFee = arenaData.EntranceFee,
                        TicketPrice = arenaData.TicketPrice,
                        AdditionalTicketPrice = arenaData.AdditionalTicketPrice,
                        QueryBlockIndex = index,
                        StoreTipBlockIndex = StandaloneContext.BlockChain!.Tip.Index,
                    };
                    return battleArenaInfo;
                });
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
            Field<NonNullGraphType<ShopQuery>>(
                name: "shopQuery",
                resolve: context => new ShopQuery(store)
            );
            Field<ListGraphType<BattleArenaRankingType>>(
                name: "BattleArenaRanking",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "championshipId" },
                    new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "round" },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "rankingType",
                        DefaultValue = "Score",
                        Description = "Input \"Score\" or \"Medal\"",
                    },
                    new QueryArgument<IntGraphType> { Name = "limit" },
                    new QueryArgument<IntGraphType> { Name = "offset" },
                    new QueryArgument<StringGraphType> { Name = "avatarAddress" }
                ),
                resolve: context =>
                {
                    int championshipId = context.GetArgument<int>("championshipId" );
                    int round = context.GetArgument<int>("round" );
                    string rankingType = context.GetArgument<string>("rankingType", "Score");
                    int? limit = context.GetArgument<int?>("limit", null );
                    int? offset = context.GetArgument<int?>("offset", null );
                    string? address = context.GetArgument<string?>("avatarAddress", null);
                    Address? avatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetBattleArenaRanking(championshipId, round, rankingType, limit, offset, avatarAddress);
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
            Field<NonNullGraphType<DauQuery>>(
                name: "dauQuery",
                resolve: context => new DauQuery(store)
            );
            Field<ListGraphType<WorldBossRankingType>>(
                name: "worldBossRanking",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "raidId",
                        Description = "world boss season id.",
                    },
                    new QueryArgument<StringGraphType>
                    {
                        Name = "avatarAddress",
                        Description = "Hex encoded avatar address",
                    },
                    new QueryArgument<IntGraphType>
                    {
                        Name = "limit",
                        Description = "query limit.",
                        DefaultValue = 100,
                    }
                ),
                resolve: context =>
                {
                    var raidId = context.GetArgument<int>("raidId");
                    var avatarAddress = context.GetArgument<string?>("avatarAddress");
                    var limit = context.GetArgument<int>("limit");
                    var raiders = Store.GetWorldBossRanking(raidId);
                    var result = raiders
                        .Take(limit)
                        .ToList();
                    if (!(avatarAddress is null) && result.All(r => r.Address != avatarAddress))
                    {
                        var myAvatar = result.FirstOrDefault(r => r.Address == avatarAddress);
                        if (!(myAvatar is null))
                        {
                            result.Add(myAvatar);
                        }
                    }

                    return result;
                });
            Field<IntGraphType>(
                name: "worldBossTotalUsers",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "raidId",
                        Description = "world boss season id.",
                    }
                ),
                resolve: context =>
                {
                    var raidId = context.GetArgument<int>("raidId");
                    return Store.GetTotalRaiders(raidId);
                });

            // mutation for test.
            Field<BooleanGraphType>(
                name: "insertRanking",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "raidId",
                        Description = "world boss season id.",
                    }
                ),
                resolve: context =>
                {
                    var raidId = context.GetArgument<int>("raidId");
                    try
                    {
                        for (int i = 0; i < 200; i++)
                        {
                            var model = new RaiderModel(
                                raidId,
                                i.ToString(),
                                i,
                                i + 1,
                                i + 2,
                                GameConfig.DefaultAvatarArmorId,
                                i,
                                new PrivateKey().ToAddress().ToHex()
                            );
                            Store.StoreRaider(model);
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return false;
                    }
                }
            );
        }

        private MySqlStore Store { get; }

        private StandaloneContext StandaloneContext { get; }
    }
}
