namespace NineChronicles.DataProvider.GraphQL.Types
{
    using global::GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class StageRankingType : ObjectGraphType<StageRankingModel>
    {
        public StageRankingType()
        {
            Field(x => x.Ranking);
            Field(x => x.ClearedStageId);
            Field(x => x.AvatarAddress);
            Field(x => x.AgentAddress);
            Field(x => x.Name);
            Field(x => x.AvatarLevel, nullable: true);
            Field(x => x.TitleId, nullable: true);
            Field(x => x.ArmorId, nullable: true);
            Field(x => x.Cp, nullable: true);
            Field(x => x.BlockIndex);

            Name = "StageRanking";
        }
    }
}
