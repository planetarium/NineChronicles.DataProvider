namespace NineChronicles.DataProvider.DataRendering
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Blocks;
    using Libplanet.Tx;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless;

    public static class TransactionData
    {
        public static TransactionModel GetTransactionInfo(
            Block block,
            Transaction transaction
        )
        {
            var actionType = NCActionUtils.ToAction(transaction.Actions.FirstOrDefault()!)
                .ToString()!.Split('.').LastOrDefault()!.Replace(">", string.Empty);
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
