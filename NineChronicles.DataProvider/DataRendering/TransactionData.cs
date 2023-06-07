namespace NineChronicles.DataProvider.DataRendering
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Blocks;
    using Libplanet.Tx;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class TransactionData
    {
        public static TransactionModel GetTransactionInfo(
            Block block,
            Transaction transaction
        )
        {
            var actionType = transaction.Actions.Actions.FirstOrDefault()!.Inspect(true).Split('"')[3];
            var transactionModel = new TransactionModel
            {
                BlockIndex = block.Index,
                BlockHash = block.Hash.ToString(),
                TxId = transaction.Id.ToString(),
                Signer = transaction.Signer.ToString(),
                ActionType = actionType,
                Nonce = transaction.Nonce,
                PublicKey = transaction.PublicKey.ToString(),
                UpdatedAddressesCount = transaction.UpdatedAddresses.Count(),
                Date = transaction.Timestamp.UtcDateTime,
                TimeStamp = transaction.Timestamp.UtcDateTime,
            };

            return transactionModel;
        }
    }
}
