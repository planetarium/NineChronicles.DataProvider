namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryMaterialData
    {
        public static ShopHistoryMaterialModel GetShopHistoryMaterialInfo(
            Buy buy,
            PurchaseInfo purchaseInfo,
            Material material,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryMaterialModel = new ShopHistoryMaterialModel()
            {
                OrderId = purchaseInfo.OrderId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = material.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = material.ItemType.ToString(),
                ItemSubType = material.ItemSubType.ToString(),
                Id = material.Id,
                ElementalType = material.ElementalType.ToString(),
                Grade = material.Grade,
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryMaterialModel;
        }
    }
}
