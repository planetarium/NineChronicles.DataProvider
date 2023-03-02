namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationConsumableData
    {
        public static CombinationConsumableModel GetCombinationConsumableInfo(
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            Address avatarAddress,
            int recipeId,
            int slotIndex,
            Guid actionId,
            long blockIndex
        )
        {
            var combinationConsumableModel = new CombinationConsumableModel()
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                RecipeId = recipeId,
                SlotIndex = slotIndex,
                BlockIndex = blockIndex,
            };

            return combinationConsumableModel;
        }
    }
}
