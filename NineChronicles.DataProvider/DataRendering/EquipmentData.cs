namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class EquipmentData
    {
        public static EquipmentModel GetEquipmentInfo(
            Address agentAddress,
            Address avatarAddress,
            Equipment equipment,
            DateTimeOffset blockTime
        )
        {
            var cp = CPHelper.GetCP(equipment);
            var equipmentModel = new EquipmentModel()
            {
                ItemId = equipment.ItemId.ToString(),
                AgentAddress = agentAddress.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                EquipmentId = equipment.Id,
                Cp = cp,
                Level = equipment.level,
                ItemSubType = equipment.ItemSubType.ToString(),
                TimeStamp = blockTime.UtcDateTime,
            };

            return equipmentModel;
        }
    }
}
