namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;

    public static class RuneEnhancementData
    {
        public static RuneEnhancementModel GetRuneEnhancementInfo(
            IAccount previousStates,
            IAccount outputStates,
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
            var prevNCGBalance = previousStates.GetBalance(
                signer,
                ncgCurrency);
            var outputNCGBalance = outputStates.GetBalance(
                signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var runeStateAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            RuneState runeState;
            if (outputStates.TryGetState(runeStateAddress, out List rawState))
            {
                runeState = new RuneState(rawState);
            }
            else
            {
                runeState = new RuneState(runeId);
            }

            RuneState previousRuneState;
            if (previousStates.TryGetState(runeStateAddress, out List prevRawState))
            {
                previousRuneState = new RuneState(prevRawState);
            }
            else
            {
                previousRuneState = new RuneState(runeId);
            }

            var sheets = outputStates.GetSheets(
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
                avatarAddress,
                runeCurrency);
            var outputRuneBalance = outputStates.GetBalance(
                avatarAddress,
                runeCurrency);
            var burntRune = prevRuneBalance - outputRuneBalance;

            var runeEnhancementModel = new RuneEnhancementModel()
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
            };

            return runeEnhancementModel;
        }
    }
}
