namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Lib9c;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockRuneSlotData
    {
        public static UnlockRuneSlotModel GetUnlockRuneSlotInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int slotIndex,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            decimal burntNCG = 0;
            decimal burntCrystal = 0;
            if (slotIndex >= 6)
            {
                var prevBalance = previousStates.GetBalance(
                    signer,
                    Currencies.Crystal);
                var outputBalance = outputStates.GetBalance(
                    signer,
                    Currencies.Crystal);
                burntCrystal = Convert.ToDecimal((prevBalance - outputBalance).GetQuantityString());
            }
            else
            {
                Currency ncgCurrency = outputStates.GetGoldCurrency();
                var prevNCGBalance = previousStates.GetBalance(
                    signer,
                    ncgCurrency);
                var outputNCGBalance = outputStates.GetBalance(
                    signer,
                    ncgCurrency);
                burntNCG = Convert.ToDecimal((prevNCGBalance - outputNCGBalance).GetQuantityString());
            }

            var unlockRuneSlotModel = new UnlockRuneSlotModel()
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                SlotIndex = slotIndex,
                BurntNCG = burntNCG,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
                BurntCRYSTAL = burntCrystal,
            };

            return unlockRuneSlotModel;
        }
    }
}
