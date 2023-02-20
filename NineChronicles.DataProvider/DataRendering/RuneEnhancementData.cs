namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Bencodex.Types;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;

    public static class RuneEnhancementData
    {
        public static RuneEnhancementModel GetRuneEnhancementInfo(
            ActionBase.ActionEvaluation<RuneEnhancement> ev,
            RuneEnhancement runeEnhancement,
            DateTimeOffset blockTime
        )
        {
            var previousStates = ev.PreviousStates;
            Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var outputNCGBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var outputCrystalBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var runeStateAddress = RuneState.DeriveAddress(runeEnhancement.AvatarAddress, runeEnhancement.RuneId);
            RuneState runeState;
            if (ev.OutputStates.TryGetState(runeStateAddress, out List rawState))
            {
                runeState = new RuneState(rawState);
            }
            else
            {
                runeState = new RuneState(runeEnhancement.RuneId);
            }

            RuneState previousRuneState;
            if (ev.PreviousStates.TryGetState(runeStateAddress, out List prevRawState))
            {
                previousRuneState = new RuneState(prevRawState);
            }
            else
            {
                previousRuneState = new RuneState(runeEnhancement.RuneId);
            }

            var sheets = ev.OutputStates.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(RuneSheet),
                    typeof(RuneListSheet),
                    typeof(RuneCostSheet),
                });
            var runeSheet = sheets.GetSheet<RuneSheet>();
            runeSheet.TryGetValue(runeState.RuneId, out var runeRow);
#pragma warning disable CS0618
            var runeCurrency = Currency.Legacy(runeRow!.Ticker, 0, minters: null);
#pragma warning restore CS0618
            var prevRuneBalance = previousStates.GetBalance(
                runeEnhancement.AvatarAddress,
                runeCurrency);
            var outputRuneBalance = ev.OutputStates.GetBalance(
                runeEnhancement.AvatarAddress,
                runeCurrency);
            var burntRune = prevRuneBalance - outputRuneBalance;

            var runeEnhancementModel = new RuneEnhancementModel()
            {
                Id = runeEnhancement.Id.ToString(),
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = runeEnhancement.AvatarAddress.ToString(),
                PreviousRuneLevel = previousRuneState.Level,
                OutputRuneLevel = runeState.Level,
                RuneId = runeEnhancement.RuneId,
                TryCount = runeEnhancement.TryCount,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                BurntRune = Convert.ToDecimal(burntRune.GetQuantityString()),
                Date = blockTime,
                TimeStamp = blockTime,
            };

            return runeEnhancementModel;
        }
    }
}
