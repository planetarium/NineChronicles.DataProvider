namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
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
            Field(x => x.AvatarLevel, true);
            Field(x => x.TitleId, true);
            Field(x => x.ArmorId, true);
            Field(x => x.Cp, true);
            Field(x => x.BlockIndex);

            Name = "StageRanking";
        }
    }
}
