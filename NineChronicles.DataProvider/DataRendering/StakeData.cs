namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;
    using static Lib9c.SerializeKeys;

    public static class StakeData
    {
        public static StakeModel GetStakeInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            outputStates.TryGetStakeState(signer, out StakeState stakeState);
            var prevStakeStartBlockIndex =
                !previousStates.TryGetStakeState(signer, out StakeState prevStakeState)
                    ? 0 : prevStakeState.StartedBlockIndex;
            var newStakeStartBlockIndex = stakeState.StartedBlockIndex;
            var currency = outputStates.GetGoldCurrency();
            var balance = outputStates.GetBalance(signer, currency);
            var stakeStateAddress = StakeState.DeriveAddress(signer);
            var previousAmount = previousStates.GetBalance(stakeStateAddress, currency);
            var newAmount = outputStates.GetBalance(stakeStateAddress, currency);

            var stakeModel = new StakeModel()
            {
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                PreviousAmount = Convert.ToDecimal(previousAmount.GetQuantityString()),
                NewAmount = Convert.ToDecimal(newAmount.GetQuantityString()),
                RemainingNCG = Convert.ToDecimal(balance.GetQuantityString()),
                PrevStakeStartBlockIndex = prevStakeStartBlockIndex,
                NewStakeStartBlockIndex = newStakeStartBlockIndex,
                TimeStamp = blockTime,
            };

            return stakeModel;
        }
    }
}
