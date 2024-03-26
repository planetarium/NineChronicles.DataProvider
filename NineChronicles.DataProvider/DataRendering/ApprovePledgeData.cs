namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class ApprovePledgeData
    {
        public static ApprovePledgeModel GetApprovePledgeInfo(
            string txId,
            long blockIndex,
            string blockHash,
            Address txSigner,
            Address patronAddress,
            DateTimeOffset blockTime
        )
        {
            var requestPledgeModel = new ApprovePledgeModel()
            {
                TxId = txId,
                BlockIndex = blockIndex,
                BlockHash = blockHash,
                Signer = txSigner.ToString(),
                PatronAddress = patronAddress.ToString(),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return requestPledgeModel;
        }
    }
}
