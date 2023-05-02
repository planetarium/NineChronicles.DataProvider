namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume.Helper;
    using NineChronicles.DataProvider.Store.Models;

    public static class RunesAcquiredData
    {
        public static RunesAcquiredModel GetRunesAcquiredInfo(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            long blockIndex,
            string actionType,
            string tickerType,
            FungibleAssetValue acquiredRune,
            DateTimeOffset blockTime
        )
        {
            var runesAcquiredModel = new RunesAcquiredModel()
            {
                Id = id.ToString(),
                ActionType = actionType,
                TickerType = tickerType,
                BlockIndex = blockIndex,
                AgentAddress = agentAddress.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                AcquiredRune = Convert.ToDecimal(acquiredRune.GetQuantityString()),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return runesAcquiredModel;
        }
    }
}
