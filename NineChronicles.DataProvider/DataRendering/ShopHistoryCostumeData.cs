namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryCostumeData
    {
        public static ShopHistoryCostumeModel GetShopHistoryCostumeInfo(
            Address buyerAvatarAddress,
            PurchaseInfo purchaseInfo,
            Costume costume,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryCostumeModel = new ShopHistoryCostumeModel()
            {
                OrderId = purchaseInfo.OrderId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = costume.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
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

        public static ShopHistoryCostumeModel GetShopHistoryCostumeInfoV2(
            Address buyerAvatarAddress,
            PurchaseInfo0 purchaseInfo,
            Costume costume,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryCostumeModel = new ShopHistoryCostumeModel()
            {
                OrderId = purchaseInfo.productId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = costume.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.price.ToString().Split(" ").FirstOrDefault()!),
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

        public static ShopHistoryCostumeModel GetShopHistoryCostumeInfoV1(
            Address buyerAvatarAddress,
            Address sellerAvatarAddress,
            Buy7.BuyerResult buyerResult,
            FungibleAssetValue price,
            Costume costume,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryCostumeModel = new ShopHistoryCostumeModel()
            {
                OrderId = buyerResult.id.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = costume.ItemId.ToString(),
                SellerAvatarAddress = sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(price.ToString().Split(" ").FirstOrDefault()!),
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
