namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;

    public static class BattleArenaData
    {
        public static BattleArenaModel GetBattleArenaInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address myAvatarAddress,
            Address enemyAvatarAddress,
            int round,
            int championshipId,
            int ticket,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarState(myAvatarAddress);
            var myArenaScoreAdr =
                ArenaScore.DeriveAddress(myAvatarAddress, championshipId, round);
            previousStates.TryGetArenaScore(myArenaScoreAdr, out var previousArenaScore);
            outputStates.TryGetArenaScore(myArenaScoreAdr, out var currentArenaScore);
            Currency ncgCurrency = outputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                signer,
                ncgCurrency);
            var outputNCGBalance = outputStates.GetBalance(
                signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;
            var arenaSheet = outputStates.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(blockIndex);
            var arenaInformationAdr =
                ArenaInformation.DeriveAddress(myAvatarAddress, championshipId, round);
            previousStates.TryGetArenaInformation(arenaInformationAdr, out var previousArenaInformation);
            outputStates.TryGetArenaInformation(arenaInformationAdr, out var currentArenaInformation);
            var winCount = currentArenaInformation.Win - previousArenaInformation.Win;
            var medalCount = 0;
            if (arenaData.ArenaType != ArenaType.OffSeason && winCount > 0)
            {
                medalCount += winCount;
                var materialSheet = outputStates.GetSheet<MaterialItemSheet>();
                var medal = ItemFactory.CreateMaterial(materialSheet, arenaData.MedalId);
                if (medal != null)
                {
                    medalCount += winCount;
                }
            }

            var battleArenaModel = new BattleArenaModel()
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = myAvatarAddress.ToString(),
                AvatarLevel = avatarState.level,
                EnemyAvatarAddress = enemyAvatarAddress.ToString(),
                ChampionshipId = championshipId,
                Round = round,
                TicketCount = ticket,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                Victory = currentArenaScore.Score > previousArenaScore.Score,
                MedalCount = medalCount,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return battleArenaModel;
        }
    }
}
