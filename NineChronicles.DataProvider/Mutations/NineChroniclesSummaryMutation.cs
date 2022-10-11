namespace NineChronicles.DataProvider.Mutations
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using GraphQL;
    using GraphQL.Types;
    using Libplanet;
    using Libplanet.Explorer.GraphTypes;
    using Nekoyume;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless;
    using NineChronicles.Headless.GraphTypes.States;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    internal class NineChroniclesSummaryMutation : ObjectGraphType
    {
        public NineChroniclesSummaryMutation(MySqlStore store, StandaloneContext standaloneContext, StateContext stateContext)
        {
            Field<BooleanGraphType>(
                name: "updateRaiders",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "raidId",
                        Description = "world boss season id.",
                    }
                ),
                resolve: context =>
                {
                    var raidId = context.GetArgument<int>("raidId");
                    var worldBossListSheetAddress = Addresses.GetSheetAddress<WorldBossListSheet>();
                    if (!(stateContext.GetState(worldBossListSheetAddress) is Text text))
                    {
                        return false;
                    }

                    var sheet = new WorldBossListSheet();
                    sheet.Set(text);
                    var bossRow = sheet.OrderedList.First(r => r.Id == raidId);
                    if (store.GetTip() < bossRow.EndedBlockIndex)
                    {
                        return false;
                    }

                    var raiderListAddress = Addresses.GetRaiderListAddress(raidId);
                    if (!(stateContext.GetState(raiderListAddress) is List raiderList))
                    {
                        return false;
                    }

                    var avatarAddresses = raiderList.ToList(StateExtensions.ToAddress);
                    var updateList = new List<RaiderModel>();
                    var storeRaiders = store.GetRaiderList();
                    UpdateRaider(stateContext, avatarAddresses, raidId, storeRaiders, updateList);

                    store.StoreRaiderList(updateList);
                    return true;
                });
        }

        private void UpdateRaider(StateContext stateContext, List<Address> avatarAddresses, int raidId, List<RaiderModel> storeRaiders, List<RaiderModel> updateList)
        {
            var raiderAddresses = avatarAddresses
                .Select(avatarAddress => Addresses.GetRaiderAddress(avatarAddress, raidId)).ToList();
            var raiderStates = new Dictionary<Address, RaiderState>();
            var result = stateContext.GetStates(raiderAddresses);
            foreach (var value in result)
            {
                if (value is List list)
                {
                    var raiderState = new RaiderState(list);
                    raiderStates.Add(raiderState.AvatarAddress, raiderState);
                }
            }

            var idx = storeRaiders.Count + 1;
            foreach (var avatarAddress in avatarAddresses)
            {
                if (!raiderStates.ContainsKey(avatarAddress))
                {
                    continue;
                }

                var raiderState = raiderStates[avatarAddress];
                var raider = storeRaiders.FirstOrDefault(x => x.Address == avatarAddress.ToHex());
                if (raider == null)
                {
                    updateList.Add(new RaiderModel(idx, raidId, raiderState));
                    idx++;
                }
                else
                {
                    if (raider.TotalScore != raiderState.TotalScore)
                    {
                        updateList.Add(new RaiderModel(raider.Id, raidId, raiderState));
                    }
                }
            }
        }
    }
}
