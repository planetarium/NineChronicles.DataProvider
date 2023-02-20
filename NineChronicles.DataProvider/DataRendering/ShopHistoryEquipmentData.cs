namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryEquipmentData
    {
        public static ShopHistoryEquipmentModel GetShopHistoryEquipmentInfo(
            ActionBase.ActionEvaluation<Buy> ev,
            Buy buy,
            PurchaseInfo purchaseInfo,
            Equipment equipment,
            int itemCount,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryEquipmentModel = new ShopHistoryEquipmentModel()
            {
                OrderId = purchaseInfo.OrderId.ToString(),
                TxId = string.Empty,
                BlockIndex = ev.BlockIndex,
                BlockHash = string.Empty,
                ItemId = equipment.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = equipment.ItemType.ToString(),
                ItemSubType = equipment.ItemSubType.ToString(),
                Id = equipment.Id,
                BuffSkillCount = equipment.BuffSkills.Count,
                ElementalType = equipment.ElementalType.ToString(),
                Grade = equipment.Grade,
                SetId = equipment.SetId,
                SkillsCount = equipment.Skills.Count,
                SpineResourcePath = equipment.SpineResourcePath,
                RequiredBlockIndex = equipment.RequiredBlockIndex,
                NonFungibleId = equipment.NonFungibleId.ToString(),
                TradableId = equipment.TradableId.ToString(),
                UniqueStatType = equipment.UniqueStatType.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryEquipmentModel;
        }
    }
}
