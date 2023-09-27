namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationConsumableData
    {
        public static CombinationConsumableModel GetCombinationConsumableInfo(
            IAccount previousStates,
            IAccount outputStates,
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
