namespace NineChronicles.DataProvider.DataRendering
{
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationConsumableData
    {
        public static CombinationConsumableModel GetCombinationConsumableInfo(
            ActionBase.ActionEvaluation<CombinationConsumable> ev,
            CombinationConsumable combinationConsumable
        )
        {
            var combinationConsumableModel = new CombinationConsumableModel()
            {
                Id = combinationConsumable.Id.ToString(),
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = combinationConsumable.avatarAddress.ToString(),
                RecipeId = combinationConsumable.recipeId,
                SlotIndex = combinationConsumable.slotIndex,
                BlockIndex = ev.BlockIndex,
            };

            return combinationConsumableModel;
        }
    }
}
