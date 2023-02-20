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
            Block<PolymorphicAction<ActionBase>> block,
            Transaction<PolymorphicAction<ActionBase>> transaction
        )
        {
            var actionType = transaction.CustomActions!.Select(action => action.ToString()!.Split('.')
                .LastOrDefault()?.Replace(">", string.Empty));
            var transactionModel = new TransactionModel
            {
                BlockIndex = block.Index,
                BlockHash = block.Hash.ToString(),
                TxId = transaction.Id.ToString(),
                Signer = transaction.Signer.ToString(),
                ActionType = actionType.FirstOrDefault(),
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
