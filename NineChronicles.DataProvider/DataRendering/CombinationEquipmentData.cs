namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using NineChronicles.DataProvider.Store.Models;

    public static class CombinationEquipmentData
    {
        public static CombinationEquipmentModel GetCombinationEquipmentInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int recipeId,
            int slotIndex,
            int? subRecipeId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var combinationEquipmentModel = new CombinationEquipmentModel()
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                RecipeId = recipeId,
                SlotIndex = slotIndex,
                SubRecipeId = subRecipeId ?? 0,
                BlockIndex = blockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime.UtcDateTime,
            };

            return combinationEquipmentModel;
        }
    }
}
