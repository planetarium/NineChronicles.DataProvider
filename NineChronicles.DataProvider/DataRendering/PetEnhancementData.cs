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
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;

    public static class PetEnhancementData
    {
        public static PetEnhancementModel GetPetEnhancementInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int petId,
            int targetLevel,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            Currency ncg = outputStates.GetGoldCurrency();
            var prevNcgBalance = previousStates.GetBalance(signer, ncg);
            var outputNcgBalance = outputStates.GetBalance(signer, ncg);
            var burntNcg = prevNcgBalance - outputNcgBalance;

            var petStateAddress = PetState.DeriveAddress(avatarAddress, petId);
            PetState petState = outputStates.TryGetState(petStateAddress, out List rawState)
                ? new PetState(rawState)
                : new PetState(petId);
            PetState prevPetState = previousStates.TryGetState(petStateAddress, out List prevRawState)
                ? new PetState(prevRawState)
                : new PetState(petId);

            var sheets = outputStates.GetSheets(
                sheetTypes: new[]
                {
                    typeof(PetSheet),
                    typeof(PetCostSheet),
                });
            var petSheet = sheets.GetSheet<PetSheet>();
            petSheet.TryGetValue(petState.PetId, out var petRow);
#pragma warning disable CS0618
            var soulStone = Currency.Legacy(petRow!.SoulStoneTicker, 0, null);
#pragma warning restore CS0618
            var prevSoulStoneBalance = previousStates.GetBalance(avatarAddress, soulStone);
            var outputSoulStoneBalance = outputStates.GetBalance(avatarAddress, soulStone);
            var burntSoulStone = prevSoulStoneBalance - outputSoulStoneBalance;

            var petEnhancementModel = new PetEnhancementModel
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                PetId = petId,
                PreviousPetLevel = prevPetState.Level,
                TargetLevel = targetLevel,
                OutputPetLevel = petState.Level,
                ChangedLevel = petState.Level - prevPetState.Level,
                BurntNCG = Convert.ToDecimal(burntNcg.GetQuantityString()),
                BurntSoulStone = Convert.ToDecimal(burntSoulStone.GetQuantityString()),
            };
            return petEnhancementModel;
        }
    }
}
