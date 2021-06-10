namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class CraftRankingType : ObjectGraphType<CraftRankingModel>
    {
        public CraftRankingType()
        {
            Field(x => x.AvatarAddress);
            Field(x => x.AgentAddress);
            Field(x => x.Ranking);
            Field(x => x.CraftCount);
            Field(x => x.BlockIndex);

            Name = "CraftRanking";
        }
    }
}
