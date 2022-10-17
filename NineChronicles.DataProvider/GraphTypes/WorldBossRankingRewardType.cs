namespace NineChronicles.DataProvider.GraphTypes
{
    using System.Collections.Generic;
    using GraphQL.Types;
    using Libplanet.Assets;
    using NineChronicles.Headless.GraphTypes;

    public class WorldBossRankingRewardType : ObjectGraphType<(int, List<FungibleAssetValue>)>
    {
        public WorldBossRankingRewardType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                "ranking",
                resolve: context => context.Source.Item1
            );
            Field<ListGraphType<FungibleAssetValueWithCurrencyType>>(
                "rewards",
                resolve: context => context.Source.Item2
            );
        }
    }
}
