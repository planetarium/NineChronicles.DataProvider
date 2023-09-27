namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
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
            IAccount previousStates,
            IAccount outputStates,
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
            AvatarState avatarState = outputStates.GetAvatarStateV2(myAvatarAddress);
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
            int ticketCount = ticket;
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
                ArenaInformation.DeriveAddress(myAvatarAddress, championshipId, round);
            previousStates.TryGetArenaInformation(arenaInformationAdr, out var previousArenaInformation);
            outputStates.TryGetArenaInformation(arenaInformationAdr, out var currentArenaInformation);
            var winCount = currentArenaInformation.Win - previousArenaInformation.Win;
            var medalCount = 0;
            if (arenaData.ArenaType != ArenaType.OffSeason &&
                winCount > 0)
            {
                var materialSheet = sheets.GetSheet<MaterialItemSheet>();
                var medal = ArenaHelper.GetMedal(championshipId, round, materialSheet);
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
