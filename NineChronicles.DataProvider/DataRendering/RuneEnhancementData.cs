namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Rune;
    using NineChronicles.DataProvider.Store.Models;

    public static class RuneEnhancementData
    {
        public static RuneEnhancementModel GetRuneEnhancementInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int runeId,
            int tryCount,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            Currency ncgCurrency = outputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(signer, ncgCurrency);
            var outputNCGBalance = outputStates.GetBalance(signer, ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;

            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(signer, crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(signer, crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;

            var runeStates = outputStates.GetRuneState(avatarAddress, out _);
            var runeState = runeStates.TryGetRuneState(runeId, out var rs) ? rs : new RuneState(runeId);

            var prevRuneStates = previousStates.GetRuneState(avatarAddress, out _);
            var previousRuneState = prevRuneStates.TryGetRuneState(runeId, out var prs) ? prs : new RuneState(runeId);

            var sheets = outputStates.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(RuneSheet),
                    typeof(RuneListSheet),
                    typeof(RuneCostSheet),
                    typeof(RuneLevelBonusSheet),
                });
            var runeSheet = sheets.GetSheet<RuneSheet>();
            runeSheet.TryGetValue(runeState.RuneId, out var runeRow);
#pragma warning disable CS0618
            var runeCurrency = Currency.Legacy(runeRow!.Ticker, 0, minters: null);
#pragma warning restore CS0618
            var prevRuneBalance = previousStates.GetBalance(
                avatarAddress,
                runeCurrency);
            var outputRuneBalance = outputStates.GetBalance(
                avatarAddress,
                runeCurrency);
            var burntRune = prevRuneBalance - outputRuneBalance;
            var prevRuneLevelBonus =
                RuneHelper.CalculateRuneLevelBonus(
                    prevRuneStates,
                    sheets.GetSheet<RuneListSheet>(),
                    sheets.GetSheet<RuneLevelBonusSheet>()
                );
            var outputRuneLevelBonus =
                RuneHelper.CalculateRuneLevelBonus(
                    runeStates,
                    sheets.GetSheet<RuneListSheet>(),
                    sheets.GetSheet<RuneLevelBonusSheet>()
                );

            var runeEnhancementModel = new RuneEnhancementModel
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                PreviousRuneLevel = previousRuneState.Level,
                OutputRuneLevel = runeState.Level,
                RuneId = runeId,
                TryCount = tryCount,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                BurntRune = Convert.ToDecimal(burntRune.GetQuantityString()),
                Date = blockTime,
                TimeStamp = blockTime,
                PreviousRuneLevelBonus = prevRuneLevelBonus,
                OutputRuneLevelBonus = outputRuneLevelBonus,
            };

            return runeEnhancementModel;
        }
    }
}
