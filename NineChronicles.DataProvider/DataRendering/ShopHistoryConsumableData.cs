namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryConsumableData
    {
        public static ShopHistoryConsumableModel GetShopHistoryConsumableInfo(
            ActionBase.ActionEvaluation<Buy> ev,
            Buy buy,
            PurchaseInfo purchaseInfo,
            Consumable consumable,
            int itemCount,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryConsumableModel = new ShopHistoryConsumableModel()
            {
                OrderId = purchaseInfo.OrderId.ToString(),
                TxId = string.Empty,
                BlockIndex = ev.BlockIndex,
                BlockHash = string.Empty,
                ItemId = consumable.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = consumable.ItemType.ToString(),
                ItemSubType = consumable.ItemSubType.ToString(),
                Id = consumable.Id,
                BuffSkillCount = consumable.BuffSkills.Count,
                ElementalType = consumable.ElementalType.ToString(),
                Grade = consumable.Grade,
                SkillsCount = consumable.Skills.Count,
                RequiredBlockIndex = consumable.RequiredBlockIndex,
                NonFungibleId = consumable.NonFungibleId.ToString(),
                TradableId = consumable.TradableId.ToString(),
                MainStat = consumable.MainStat.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryConsumableModel;
        }
    }
}
