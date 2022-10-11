namespace NineChronicles.DataProvider.Mutations
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using GraphQL;
    using GraphQL.Types;
    using Libplanet;
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
        private readonly string _webHookUrl = $"https://hooks.slack.com/services/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";

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
                    var task = SendMessageAsync(updateList);
                    return task.Result;
                });
        }

        private async Task<bool> SendMessageAsync(List<RaiderModel> raiderList)
        {
            var sb = new StringBuilder($"[UPDATE RAIDERS RESULT] user count : {raiderList.Count}\n");
            foreach (var model in raiderList)
            {
                sb.Append($"[{model.Address}] total score : {model.TotalScore}\n");
            }

            var json = System.Text.Json.JsonSerializer.Serialize(new { text = sb.ToString() });
            var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "payload", json }, });
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(_webHookUrl, content);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
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
