namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryMaterialData
    {
        public static ShopHistoryMaterialModel GetShopHistoryMaterialInfo(
            Address buyerAvatarAddress,
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
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
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

        public static ShopHistoryMaterialModel GetShopHistoryMaterialInfoV2(
            Address buyerAvatarAddress,
            PurchaseInfo0 purchaseInfo,
            Material material,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryMaterialModel = new ShopHistoryMaterialModel()
            {
                OrderId = purchaseInfo.productId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = material.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.price.ToString().Split(" ").FirstOrDefault()!),
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

        public static ShopHistoryMaterialModel GetShopHistoryMaterialInfoV1(
            Address buyerAvatarAddress,
            Address sellerAvatarAddress,
            Buy7.BuyerResult buyerResult,
            FungibleAssetValue price,
            Material material,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryMaterialModel = new ShopHistoryMaterialModel()
            {
                OrderId = buyerResult.id.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = material.ItemId.ToString(),
                SellerAvatarAddress = sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(price.ToString().Split(" ").FirstOrDefault()!),
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
