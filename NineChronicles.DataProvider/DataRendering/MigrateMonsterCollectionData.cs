namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class MigrateMonsterCollectionData
    {
        public static MigrateMonsterCollectionModel GetMigrateMonsterCollectionInfo(
            ActionBase.ActionEvaluation<MigrateMonsterCollection> ev,
            DateTimeOffset blockTime
        )
        {
            ev.OutputStates.TryGetStakeState(ev.Signer, out StakeState stakeState);
            var agentState = ev.PreviousStates.GetAgentState(ev.Signer);
            Address collectionAddress = MonsterCollectionState.DeriveAddress(ev.Signer, agentState.MonsterCollectionRound);
            ev.PreviousStates.TryGetState(collectionAddress, out Dictionary stateDict);
            var monsterCollectionState = new MonsterCollectionState(stateDict);
            var currency = ev.OutputStates.GetGoldCurrency();
            var migrationAmount = ev.PreviousStates.GetBalance(monsterCollectionState.address, currency);
            var migrationStartBlockIndex = ev.BlockIndex;
            var stakeStartBlockIndex = stakeState.StartedBlockIndex;

            var migrateMonsterCollectionModel = new MigrateMonsterCollectionModel()
            {
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
                MigrationAmount = Convert.ToDecimal(migrationAmount.GetQuantityString()),
                MigrationStartBlockIndex = migrationStartBlockIndex,
                StakeStartBlockIndex = stakeStartBlockIndex,
                TimeStamp = blockTime,
            };

            return migrateMonsterCollectionModel;
        }
    }
}
