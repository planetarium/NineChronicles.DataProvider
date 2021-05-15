namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class StageRankingType : ObjectGraphType<StageRankingModel>
    {
        public StageRankingType()
        {
            Field(x => x.ClearedStageId);
            Field(x => x.Name);

            Name = "StageRanking";
        }
    }
}
