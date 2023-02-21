namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;

    public static class BattleArenaData
    {
        public static BattleArenaModel GetBattleArenaInfo(
            BattleArena battleArena,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(battleArena.myAvatarAddress);
            var myArenaScoreAdr =
                ArenaScore.DeriveAddress(battleArena.myAvatarAddress, battleArena.championshipId, battleArena.round);
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
            int ticketCount = battleArena.ticket;
            var sheets = previousStates.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(MaterialItemSheet),
                    typeof(CharacterSheet),
                    typeof(CostumeStatSheet),
                    typeof(RuneListSheet),
                    typeof(RuneOptionSheet),
                });
            var arenaSheet = outputStates.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(blockIndex);
            var arenaInformationAdr =
                ArenaInformation.DeriveAddress(battleArena.myAvatarAddress, battleArena.championshipId, battleArena.round);
            previousStates.TryGetArenaInformation(arenaInformationAdr, out var previousArenaInformation);
            outputStates.TryGetArenaInformation(arenaInformationAdr, out var currentArenaInformation);
            var winCount = currentArenaInformation.Win - previousArenaInformation.Win;
            var medalCount = 0;
            if (arenaData.ArenaType != ArenaType.OffSeason &&
                winCount > 0)
            {
                var materialSheet = sheets.GetSheet<MaterialItemSheet>();
                var medal = ArenaHelper.GetMedal(battleArena.championshipId, battleArena.round, materialSheet);
                if (medal != null)
                {
                    medalCount += winCount;
                }
            }

            var battleArenaModel = new BattleArenaModel()
            {
                Id = battleArena.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = battleArena.myAvatarAddress.ToString(),
                AvatarLevel = avatarState.level,
                EnemyAvatarAddress = battleArena.enemyAvatarAddress.ToString(),
                ChampionshipId = battleArena.championshipId,
                Round = battleArena.round,
                TicketCount = ticketCount,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                Victory = currentArenaScore.Score > previousArenaScore.Score,
                MedalCount = medalCount,
                TimeStamp = blockTime,
            };

            return battleArenaModel;
        }
    }
}
