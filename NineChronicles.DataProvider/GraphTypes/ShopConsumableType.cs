namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class ShopConsumableType : ObjectGraphType<ShopConsumableModel>
    {
        public ShopConsumableType()
        {
            Field(x => x.BlockIndex);
            Field(x => x.ItemId);
            Field(x => x.SellerAgentAddress);
            Field(x => x.SellerAvatarAddress);
            Field(x => x.ItemType);
            Field(x => x.ItemSubType);
            Field(x => x.Id);
            Field(x => x.BuffSkillCount);
            Field(x => x.ElementalType);
            Field(x => x.Grade);
            Field(x => x.SkillsCount);
            Field(x => x.RequiredBlockIndex);
            Field(x => x.NonFungibleId);
            Field(x => x.TradableId);
            Field(x => x.MainStat);
            Field(x => x.Price);
            Field(x => x.OrderId);
            Field(x => x.CombatPoint);
            Field(x => x.ItemCount);
            Field(x => x.SellExpiredBlockIndex);
            Field(x => x.SellStartedBlockIndex);
            Field(x => x.TimeStamp);

            Name = "ShopConsumable";
        }
    }
}
