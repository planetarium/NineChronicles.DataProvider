namespace NineChronicles.DataProvider.DataRendering
{
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationEquipmentData
    {
        public static CombinationEquipmentModel GetCombinationEquipmentInfo(
            ActionBase.ActionEvaluation<CombinationEquipment> ev,
            CombinationEquipment combinationEquipment
        )
        {
            var combinationEquipmentModel = new CombinationEquipmentModel()
            {
                Id = combinationEquipment.Id.ToString(),
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = combinationEquipment.avatarAddress.ToString(),
                RecipeId = combinationEquipment.recipeId,
                SlotIndex = combinationEquipment.slotIndex,
                SubRecipeId = combinationEquipment.subRecipeId ?? 0,
                BlockIndex = ev.BlockIndex,
            };

            return combinationEquipmentModel;
        }
    }
}
