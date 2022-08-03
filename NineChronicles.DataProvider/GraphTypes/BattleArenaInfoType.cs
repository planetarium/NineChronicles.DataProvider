namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class BattleArenaInfoType : ObjectGraphType<BattleArenaInfoModel>
    {
        public BattleArenaInfoType()
        {
            Field(x => x.ChampionshipId);
            Field(x => x.Round);
            Field(x => x.ArenaType);
            Field(x => x.StartBlockIndex);
            Field(x => x.EndBlockIndex);
            Field(x => x.RequiredMedalCount);
            Field(x => x.EntranceFee);
            Field(x => x.TicketPrice);
            Field(x => x.AdditionalTicketPrice);
            Field(x => x.QueryBlockIndex);
            Field(x => x.StoreTipBlockIndex);

            Name = "BattleArenaInfo";
        }
    }
}
