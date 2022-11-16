namespace NineChronicles.DataProvider.Queries
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using GraphQL;
    using GraphQL.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Explorer.GraphTypes;
    using Nekoyume;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.GraphTypes;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless;
    using NineChronicles.Headless.GraphTypes.States;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    internal class NineChroniclesSummaryQuery : ObjectGraphType
    {
        public NineChroniclesSummaryQuery(MySqlStore store, StandaloneContext standaloneContext, StateContext stateContext)
        {
            Store = store;
            StandaloneContext = standaloneContext;
            StateContext = stateContext;
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
                    IValue state = StateContext.GetState(arenaSheetAddress)!;
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
            Field<WorldBossRankingInfoType>(
                name: "worldBossRanking",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "raidId",
                        Description = "world boss season id.",
                    },
                    new QueryArgument<AddressType>
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
                    var avatarAddress = context.GetArgument<Address?>("avatarAddress");
                    var limit = context.GetArgument<int>("limit");
                    var raiders = Store.GetWorldBossRanking(raidId, null, null);
                    var result = raiders
                        .Take(limit)
                        .ToList();
                    if (!(avatarAddress is null) && result.All(r => r.Address != avatarAddress.Value.ToHex()))
                    {
                        var myAvatar = raiders.FirstOrDefault(r => r.Address == avatarAddress.Value.ToHex());
                        if (!(myAvatar is null))
                        {
                            result.Add(myAvatar);
                        }
                    }

                    // Use database block tip because sync db & store delay.
                    return (Store.GetTip(), result);
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

            Field<WorldBossRankingRewardType>(
                "worldBossRankingReward",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "raidId",
                        Description = "world boss season id.",
                    },
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "avatarAddress",
                        Description = "address of avatar state.",
                    }
                ),
                resolve: context =>
                {
                    var raidId = context.GetArgument<int>("raidId");
                    var avatarAddress = context.GetArgument<Address>("avatarAddress");

                    // Use database block tip because sync db & store delay.
                    var blockIndex = Store.GetTip();
                    var worldBossListSheetAddress = Addresses.GetSheetAddress<WorldBossListSheet>();
                    var runeSheetAddress = Addresses.GetSheetAddress<RuneSheet>();
                    var rewardSheetAddress = Addresses.GetSheetAddress<WorldBossRankingRewardSheet>();
                    var values = stateContext.GetStates(new[] { worldBossListSheetAddress, runeSheetAddress, rewardSheetAddress });
                    if (values[0] is Text wbs && values[1] is Text rs && values[2] is Text wrs)
                    {
                        var sheet = new WorldBossListSheet();
                        sheet.Set(wbs);
                        var runeSheet = new RuneSheet();
                        runeSheet.Set(rs);
                        var rankingRewardSheet = new WorldBossRankingRewardSheet();
                        rankingRewardSheet.Set(wrs);
                        var bossRow = sheet.OrderedList!.First(r => r.Id == raidId);
                        if (bossRow.EndedBlockIndex <= blockIndex && Store.MigrationExists(raidId))
                        {
                            // Check ranking.
                            var raiders = Store.GetWorldBossRanking(raidId, null, null);
                            var raider = raiders.First(r => r.Address == avatarAddress.ToHex());
                            var ranking = raider.Ranking;

                            // backward compatibility for season 1. because season 1 reward already distributed.
                            var rate = raidId == 1
                                ? ranking / raiders.Count * 100
                                : ranking * 100 / raiders.Count;

                            // calculate rewards.
                            var row = rankingRewardSheet.FindRow(ranking, rate);
                            return (raider, row.GetRewards(runeSheet));
                        }
                    }

                    throw new ExecutionError("can't receive");
                }
            );

            Field<ListGraphType<WorldBossRankingRewardType>>(
                "worldBossRankingRewards",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "raidId",
                        Description = "world boss season id.",
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "limit",
                        Description = "query limit.",
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "offset",
                        Description = "query offset.",
                    }
                ),
                resolve: context =>
                {
                    var raidId = context.GetArgument<int>("raidId");
                    int limit = context.GetArgument<int>("limit" );
                    int offset = context.GetArgument<int>("offset");

                    // Check calculate state end.
                    // Use database block tip because sync db & store delay.
                    var blockIndex = Store.GetTip();
                    var worldBossListSheetAddress = Addresses.GetSheetAddress<WorldBossListSheet>();
                    var runeSheetAddress = Addresses.GetSheetAddress<RuneSheet>();
                    var rewardSheetAddress = Addresses.GetSheetAddress<WorldBossRankingRewardSheet>();
                    var values = stateContext.GetStates(new[] { worldBossListSheetAddress, runeSheetAddress, rewardSheetAddress });
                    if (values[0] is Text wbs && values[1] is Text rs && values[2] is Text wrs)
                    {
                        var sheet = new WorldBossListSheet();
                        sheet.Set(wbs);
                        var runeSheet = new RuneSheet();
                        runeSheet.Set(rs);
                        var rankingRewardSheet = new WorldBossRankingRewardSheet();
                        rankingRewardSheet.Set(wrs);
                        var bossRow = sheet.OrderedList!.First(r => r.Id == raidId);
                        if (bossRow.EndedBlockIndex <= blockIndex)
                        {
                            // Check ranking.
                            var raiders = Store.GetWorldBossRanking(raidId, offset, limit);
                            int totalCount = Store.GetTotalRaiders(raidId);
                            var result = new List<(WorldBossRankingModel, List<FungibleAssetValue>)>();
                            foreach (var raider in raiders)
                            {
                                var ranking = raider.Ranking;

                                // backward compatibility for season 1. because season 1 reward already distributed.
                                var rate = raidId == 1
                                    ? ranking / totalCount * 100
                                    : ranking * 100 / totalCount;
                                var row = rankingRewardSheet.FindRow(ranking, rate);
                                result.Add((raider, row.GetRewards(runeSheet)));
                            }

                            return result;
                        }
                    }

                    throw new ExecutionError("can't receive");
                }
            );
        }

        private MySqlStore Store { get; }

        private StandaloneContext StandaloneContext { get; }

        private StateContext StateContext { get; }
    }
}
