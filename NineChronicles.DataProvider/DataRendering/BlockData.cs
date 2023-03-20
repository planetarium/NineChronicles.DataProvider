namespace NineChronicles.DataProvider.DataRendering
{
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Blocks;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class BlockData
    {
        public static BlockModel GetBlockInfo(
            Block<PolymorphicAction<ActionBase>> block)
        {
            var blockModel = new BlockModel()
            {
                Index = block.Index,
                Hash = block.Hash.ToString(),
                Miner = block.Miner.ToString(),
                Difficulty = block.Difficulty,
                Nonce = block.Nonce.ToString(),
                PreviousHash = block.PreviousHash.ToString(),
                ProtocolVersion = block.ProtocolVersion,
                PublicKey = block.PublicKey!.ToString(),
                StateRootHash = block.StateRootHash.ToString(),
                TotalDifficulty = (long)block.TotalDifficulty,
                TxCount = block.Transactions.Count(),
                TxHash = block.TxHash.ToString(),
                TimeStamp = block.Timestamp.UtcDateTime,
            };

            return blockModel;
        }
    }
}
