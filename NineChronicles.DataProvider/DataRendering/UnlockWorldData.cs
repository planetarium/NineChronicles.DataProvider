namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockWorldData
    {
        public static List<UnlockWorldModel> GetUnlockWorldInfo(
            ActionBase.ActionEvaluation<UnlockWorld> ev,
            UnlockWorld unlockWorld,
            DateTimeOffset blockTime
        )
        {
            var previousStates = ev.PreviousStates;
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var outputCrystalBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var unlockWorldList = new List<UnlockWorldModel>();
            foreach (var worldId in unlockWorld.WorldIds)
            {
                unlockWorldList.Add(new UnlockWorldModel()
                {
                    Id = unlockWorld.Id.ToString(),
                    BlockIndex = ev.BlockIndex,
                    AgentAddress = ev.Signer.ToString(),
                    AvatarAddress = unlockWorld.AvatarAddress.ToString(),
                    UnlockWorldId = worldId,
                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                    TimeStamp = blockTime,
                });
            }

            return unlockWorldList;
        }
    }
}
