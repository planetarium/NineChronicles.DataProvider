namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.State;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class RequestPledgeData
    {
        public static RequestPledgeModel GetRequestPledgeInfo(
            string txId,
            long blockIndex,
            string blockHash,
            Address txSigner,
            Address pledgeAgentAddress,
            int refillMead,
            DateTimeOffset blockTime
        )
        {
            var requestPledgeModel = new RequestPledgeModel()
            {
                TxId = txId,
                BlockIndex = blockIndex,
                BlockHash = blockHash,
                TxSigner = txSigner.ToString(),
                PledgeAgentAddress = pledgeAgentAddress.ToString(),
                RefillMead = refillMead,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return requestPledgeModel;
        }
    }
}
