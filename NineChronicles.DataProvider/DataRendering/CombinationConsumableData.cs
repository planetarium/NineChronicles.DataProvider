namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationConsumableData
    {
        public static CombinationConsumableModel GetCombinationConsumableInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int recipeId,
            int slotIndex,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
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
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime.UtcDateTime,
            };

            return combinationConsumableModel;
        }
    }
}
