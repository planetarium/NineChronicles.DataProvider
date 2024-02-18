namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class HackAndSlashRandomBuffData
    {
        public static HasRandomBuffModel GetHasRandomBuffInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            bool advancedGacha,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState prevAvatarState = previousStates.GetAvatarState(avatarAddress);
            prevAvatarState.worldInformation.TryGetLastClearedStageId(out var currentStageId);
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var hasRandomBuffModel = new HasRandomBuffModel()
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                HasStageId = currentStageId,
                GachaCount = !advancedGacha ? 5 : 10,
                BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return hasRandomBuffModel;
        }
    }
}
