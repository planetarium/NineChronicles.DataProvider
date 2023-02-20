namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryCostumeData
    {
        public static ShopHistoryCostumeModel GetShopHistoryCostumeInfo(
            ActionBase.ActionEvaluation<Buy> ev,
            Buy buy,
            PurchaseInfo purchaseInfo,
            Costume costume,
            int itemCount,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryCostumeModel = new ShopHistoryCostumeModel()
            {
                OrderId = purchaseInfo.OrderId.ToString(),
                TxId = string.Empty,
                BlockIndex = ev.BlockIndex,
                BlockHash = string.Empty,
                ItemId = costume.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = costume.ItemType.ToString(),
                ItemSubType = costume.ItemSubType.ToString(),
                Id = costume.Id,
                ElementalType = costume.ElementalType.ToString(),
                Grade = costume.Grade,
                Equipped = costume.Equipped,
                SpineResourcePath = costume.SpineResourcePath,
                RequiredBlockIndex = costume.RequiredBlockIndex,
                NonFungibleId = costume.NonFungibleId.ToString(),
                TradableId = costume.TradableId.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryCostumeModel;
        }
    }
}
