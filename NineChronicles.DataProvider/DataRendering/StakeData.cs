namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Stake;
    using NineChronicles.DataProvider.Store.Models;

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
            var currency = outputStates.GetGoldCurrency();
            var stakeAddress = StakeStateV2.DeriveAddress(signer);
            FungibleAssetValue previousAmount;
            FungibleAssetValue newAmount;
            long prevStakeStartBlockIndex;
            long newStakeStartBlockIndex;
            if (previousStates.TryGetStakeStateV2(signer, out var prevStakeStateV2))
            {
                previousAmount = previousStates.GetBalance(stakeAddress, currency);
                prevStakeStartBlockIndex = prevStakeStateV2.StartedBlockIndex;
            }
            else
            {
                previousAmount = new FungibleAssetValue(currency, 0, 0);
                prevStakeStartBlockIndex = 0;
            }

            if (outputStates.TryGetStakeStateV2(signer, out var stakeStateV2))
            {
                newAmount = outputStates.GetBalance(stakeAddress, currency);
                newStakeStartBlockIndex = stakeStateV2.StartedBlockIndex;
            }
            else
            {
                newAmount = new FungibleAssetValue(currency, 0, 0);
                newStakeStartBlockIndex = 0;
            }

            var balance = outputStates.GetBalance(signer, currency);
            var stakeModel = new StakeModel
            {
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                PreviousAmount = Convert.ToDecimal(previousAmount.GetQuantityString()),
                NewAmount = Convert.ToDecimal(newAmount.GetQuantityString()),
                RemainingNCG = Convert.ToDecimal(balance.GetQuantityString()),
                PrevStakeStartBlockIndex = prevStakeStartBlockIndex,
                NewStakeStartBlockIndex = newStakeStartBlockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return stakeModel;
        }
    }
}
