namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class AbilityRankingType : ObjectGraphType<AbilityRankingModel>
    {
        public AbilityRankingType()
        {
            Field(x => x.AvatarAddress);
            Field(x => x.AgentAddress);
            Field(x => x.Name);
            Field(x => x.AvatarLevel, true);
            Field(x => x.TitleId, true);
            Field(x => x.ArmorId, true);
            Field(x => x.Cp, true);
            Field(x => x.Ranking);

            Name = "AbilityRanking";
        }
    }
}
