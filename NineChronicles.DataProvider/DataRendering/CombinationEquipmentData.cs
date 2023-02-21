namespace NineChronicles.DataProvider.DataRendering
{
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationEquipmentData
    {
        public static CombinationEquipmentModel GetCombinationEquipmentInfo(
            CombinationEquipment combinationEquipment,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex
        )
        {
            var combinationEquipmentModel = new CombinationEquipmentModel()
            {
                Id = combinationEquipment.Id.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = combinationEquipment.avatarAddress.ToString(),
                RecipeId = combinationEquipment.recipeId,
                SlotIndex = combinationEquipment.slotIndex,
                SubRecipeId = combinationEquipment.subRecipeId ?? 0,
                BlockIndex = blockIndex,
            };

            return combinationEquipmentModel;
        }
    }
}
