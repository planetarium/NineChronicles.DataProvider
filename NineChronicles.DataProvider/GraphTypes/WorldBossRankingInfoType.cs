namespace NineChronicles.DataProvider.GraphTypes
{
    using System.Collections.Generic;
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class WorldBossRankingInfoType : ObjectGraphType<(long blockIndex, List<WorldBossRankingModel> worldBossRankingModels)>
    {
        public WorldBossRankingInfoType()
        {
            Field<NonNullGraphType<LongGraphType>>(
                "blockIndex",
                resolve: context => context.Source.blockIndex
            );
            Field<NonNullGraphType<ListGraphType<WorldBossRankingType>>>(
                "rankingInfo",
                resolve: context => context.Source.worldBossRankingModels
            );
        }
    }
}
