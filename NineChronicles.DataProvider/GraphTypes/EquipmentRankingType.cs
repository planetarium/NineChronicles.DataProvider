namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class EquipmentRankingType : ObjectGraphType<EquipmentRankingModel>
    {
        public EquipmentRankingType()
        {
            Field(x => x.ItemId);
            Field(x => x.AgentAddress);
            Field(x => x.AvatarAddress);
            Field(x => x.EquipmentId);
            Field(x => x.Cp);
            Field(x => x.Level);
            Field(x => x.ItemSubType);
            Field(x => x.Name);
            Field(x => x.AvatarLevel, true);
            Field(x => x.TitleId, true);
            Field(x => x.ArmorId, true);
            Field(x => x.Ranking);

            Name = "EquipmentRanking";
        }
    }
}
