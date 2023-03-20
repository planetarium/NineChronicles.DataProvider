namespace NineChronicles.DataProvider.DataRendering
{
    using Libplanet;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class EquipmentData
    {
        public static EquipmentModel GetEquipmentInfo(
            Address agentAddress,
            Address avatarAddress,
            Equipment equipment
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
            };

            return equipmentModel;
        }
    }
}
