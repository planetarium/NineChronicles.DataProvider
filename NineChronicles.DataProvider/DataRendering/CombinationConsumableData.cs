namespace NineChronicles.DataProvider.DataRendering
{
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationConsumableData
    {
        public static CombinationConsumableModel GetCombinationConsumableInfo(
            CombinationConsumable combinationConsumable,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex
        )
        {
            var combinationConsumableModel = new CombinationConsumableModel()
            {
                Id = combinationConsumable.Id.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = combinationConsumable.avatarAddress.ToString(),
                RecipeId = combinationConsumable.recipeId,
                SlotIndex = combinationConsumable.slotIndex,
                BlockIndex = blockIndex,
            };

            return combinationConsumableModel;
        }
    }
}
