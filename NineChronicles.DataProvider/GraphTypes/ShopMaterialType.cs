namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models;

    public class ShopMaterialType : ObjectGraphType<ShopMaterialModel>
    {
        public ShopMaterialType()
        {
            Field(x => x.BlockIndex);
            Field(x => x.ItemId);
            Field(x => x.SellerAgentAddress);
            Field(x => x.SellerAvatarAddress);
            Field(x => x.ItemType);
            Field(x => x.ItemSubType);
            Field(x => x.Id);
            Field(x => x.ElementalType);
            Field(x => x.Grade);
            Field(x => x.TradableId);
            Field(x => x.Price);
            Field(x => x.OrderId);
            Field(x => x.CombatPoint);
            Field(x => x.ItemCount);
            Field(x => x.SellExpiredBlockIndex);
            Field(x => x.SellStartedBlockIndex);
            Field(x => x.TimeStamp);

            Name = "ShopMaterial";
        }
    }
}
