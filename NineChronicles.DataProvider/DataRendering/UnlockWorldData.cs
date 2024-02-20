namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockWorldData
    {
        public static List<UnlockWorldModel> GetUnlockWorldInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            List<int> worldIds,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var unlockWorldList = new List<UnlockWorldModel>();
            foreach (var worldId in worldIds)
            {
                unlockWorldList.Add(new UnlockWorldModel()
                {
                    Id = actionId.ToString(),
                    BlockIndex = blockIndex,
                    AgentAddress = signer.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    UnlockWorldId = worldId,
                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                    Date = DateOnly.FromDateTime(blockTime.DateTime),
                    TimeStamp = blockTime,
                });
            }

            return unlockWorldList;
        }
    }
}
