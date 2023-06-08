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
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public static class TransactionData
    {
        public static TransactionModel GetTransactionInfo(
            Block block,
            Transaction transaction
        )
        {
            var actionType = ToAction(transaction.Actions.FirstOrDefault()!)
                .ToString().Split('.').LastOrDefault()!.Replace(">", string.Empty);
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

        public static NCAction ToAction(IValue plainValue)
        {
    #pragma warning disable CS0612
            var action = new NCAction();
    #pragma warning restore CS0612
            action.LoadPlainValue(plainValue);
            return action;
        }
    }
}
