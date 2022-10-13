namespace NineChronicles.DataProvider.GraphTypes
{
    using System.Collections.Generic;
    using GraphQL.Types;
    using Libplanet.Assets;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless.GraphTypes;

    public class WorldBossRankingRewardType : ObjectGraphType<(WorldBossRankingModel, List<FungibleAssetValue>)>
    {
        public WorldBossRankingRewardType()
        {
            Field<NonNullGraphType<WorldBossRankingType>>(
                "raider",
                resolve: context => context.Source.Item1
            );
            Field<NonNullGraphType<ListGraphType<FungibleAssetValueWithCurrencyType>>>(
                "rewards",
                resolve: context => context.Source.Item2
            );
        }
    }
}
