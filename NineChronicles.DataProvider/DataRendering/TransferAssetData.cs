namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Blocks;
    using Libplanet.Tx;
    using NineChronicles.DataProvider.Store.Models;

    public static class TransferAssetData
    {
        public static TransferAssetModel GetTransferAssetInfo(
            Guid id,
            TxId txId,
            long blockIndex,
            string blockHash,
            Address sender,
            Address recipient,
            string tickerType,
            FungibleAssetValue amount,
            DateTimeOffset blockTime
        )
        {
            var transferAssetModel = new TransferAssetModel()
            {
                Id = id.ToString(),
                TxId = txId.ToString(),
                BlockIndex = blockIndex,
                BlockHash = blockHash,
                Sender = sender.ToString(),
                Recipient = recipient.ToString(),
                Amount = Convert.ToDecimal(amount.GetQuantityString()),
                TickerType = tickerType,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return transferAssetModel;
        }
    }
}
