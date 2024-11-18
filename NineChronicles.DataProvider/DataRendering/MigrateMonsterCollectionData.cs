namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class MigrateMonsterCollectionData
    {
        public static MigrateMonsterCollectionModel GetMigrateMonsterCollectionInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            outputStates.TryGetStakeState(signer, out StakeState stakeState);
            var agentState = previousStates.GetAgentState(signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state failed to load.");
            }

            Address collectionAddress = MonsterCollectionState.DeriveAddress(signer, agentState.MonsterCollectionRound);
            previousStates.TryGetLegacyState(collectionAddress, out Dictionary stateDict);
            var monsterCollectionState = new MonsterCollectionState(stateDict);
            var currency = outputStates.GetGoldCurrency();
            var migrationAmount = previousStates.GetBalance(monsterCollectionState.address, currency);
            var migrationStartBlockIndex = blockIndex;
            var stakeStartBlockIndex = stakeState.StartedBlockIndex;

            var migrateMonsterCollectionModel = new MigrateMonsterCollectionModel()
            {
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                MigrationAmount = Convert.ToDecimal(migrationAmount.GetQuantityString()),
                MigrationStartBlockIndex = migrationStartBlockIndex,
                StakeStartBlockIndex = stakeStartBlockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return migrateMonsterCollectionModel;
        }
    }
}
