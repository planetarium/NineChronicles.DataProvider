namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Libplanet.Action.State;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Summon;
    using NineChronicles.DataProvider.DataRendering;
    using NineChronicles.DataProvider.DataRendering.Summon;
    using NineChronicles.DataProvider.Store.Models.Summon;
    using Serilog;

    public partial class RenderSubscriber
    {
        private readonly List<AuraSummonModel> _auraSummonList = new ();
        private readonly List<AuraSummonFailModel> _auraSummonFailList = new ();
        private readonly List<RuneSummonModel> _runeSummonList = new ();
        private readonly List<RuneSummonFailModel> _runeSummonFailList = new ();
        private readonly List<CostumeSummonModel> _costumeSummonList = new ();

        public void StoreSummonList()
        {
            try
            {
                var tasks = new List<Task>();

                Log.Debug("[DataProvider] Store costume summon list");
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[AuraSummon] {_auraSummonList.Count} AuraSummon");
                    await MySqlStore.StoreAuraSummonList(_auraSummonList);
                }));
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[AuraSummonFail] {_auraSummonFailList.Count} failed AuraSummons");
                    await MySqlStore.StoreAuraSummonFailList(_auraSummonFailList);
                }));
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[RuneSummon] {_runeSummonList.Count} RuneSummons");
                    await MySqlStore.StoreRuneSummonList(_runeSummonList);
                }));
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[RuneSummonFail] {_runeSummonFailList.Count} failed RuneSummons");
                    await MySqlStore.StoreRuneSummonFailList(_runeSummonFailList);
                }));
                tasks.Add(Task.Run(async () =>
                {
                    Log.Debug($"[CostumeSummon] {_costumeSummonList.Count} CostumeSummons");
                    await MySqlStore.StoreCostumeSummonList(_costumeSummonList);
                }));

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        partial void SubscribeAuraSummon(ActionEvaluation<AuraSummon> evt)
        {
            try
            {
                if (evt.Action is { } auraSummon)
                {
                    var start = DateTimeOffset.UtcNow;
                    var inputState = new World(_blockChainStates.GetWorldState(evt.PreviousState));
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = auraSummon.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress))
                    {
                        _avatars.Add(avatarAddress);
                        _avatarList.Add(AvatarData.GetAvatarInfo(
                            outputState,
                            evt.Signer,
                            avatarAddress,
                            _blockTimeOffset,
                            BattleType.Adventure
                        ));
                    }

                    if (evt.Exception is null)
                    {
                        _auraSummonList.Add(AuraSummonData
                            .GetAuraSummonInfo(
                                inputState,
                                outputState,
                                evt.Signer,
                                auraSummon.AvatarAddress,
                                auraSummon.GroupId,
                                auraSummon.SummonCount,
                                auraSummon.Id,
                                evt.BlockIndex,
                                _blockTimeOffset
                            ));
                    }
                    else
                    {
                        _auraSummonFailList.Add(
                            AuraSummonData.GetAuraSummonFailInfo(
                                inputState,
                                outputState,
                                evt.Signer,
                                auraSummon.AvatarAddress,
                                auraSummon.GroupId,
                                auraSummon.SummonCount,
                                auraSummon.Id,
                                evt.BlockIndex,
                                evt.Exception,
                                _blockTimeOffset
                            ));
                    }

                    var end = DateTimeOffset.UtcNow;
                    Log.Debug(
                        "[DataProvider] Stored AuraSummon action in block #{BlockIndex}. Time taken: {Time} ms",
                        evt.BlockIndex,
                        (end - start).Milliseconds
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[DataProvider] RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    ex.Message,
                    ex.StackTrace
                );
            }
        }

        partial void SubscribeRuneSummon(ActionEvaluation<RuneSummon> evt)
        {
            try
            {
                if (evt.Action is { } runeSummon)
                {
                    var start = DateTimeOffset.UtcNow;
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = runeSummon.AvatarAddress;
                    if (!_avatars.Contains(avatarAddress))
                    {
                        _avatars.Add(avatarAddress);
                        _avatarList.Add(
                            AvatarData.GetAvatarInfo(
                                outputState,
                                evt.Signer,
                                avatarAddress,
                                _blockTimeOffset,
                                BattleType.Adventure
                            ));
                    }

                    if (evt.Exception is null)
                    {
                        var sheets = outputState.GetSheets(
                            sheetTypes: new[]
                            {
                                typeof(RuneSheet),
                                typeof(RuneSummonSheet),
                            });
                        var runeSheet = sheets.GetSheet<RuneSheet>();
                        var summonSheet = sheets.GetSheet<RuneSummonSheet>();
                        _runeSummonList.Add(RuneSummonData
                            .GetRuneSummonInfo(
                                evt.Signer,
                                runeSummon.AvatarAddress,
                                runeSummon.GroupId,
                                runeSummon.SummonCount,
                                runeSummon.Id,
                                evt.BlockIndex,
                                runeSheet,
                                summonSheet,
                                new ReplayRandom(evt.RandomSeed),
                                _blockTimeOffset
                            ));
                    }
                    else
                    {
                        _runeSummonFailList.Add(
                            RuneSummonData.GetRuneSummonFailInfo(
                                evt.Signer,
                                runeSummon.AvatarAddress,
                                runeSummon.GroupId,
                                runeSummon.SummonCount,
                                runeSummon.Id,
                                evt.BlockIndex,
                                evt.Exception,
                                _blockTimeOffset
                            ));
                    }

                    var end = DateTimeOffset.UtcNow;
                    Log.Debug(
                        "[DataProvider] Stored RuneSummon action in block #{BlockIndex}. Time taken: {Time} ms",
                        evt.BlockIndex,
                        (end - start).Milliseconds
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[DataProvider] RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    ex.Message,
                    ex.StackTrace
                );
            }
        }

        partial void SubscribeCostumeSummon(ActionEvaluation<CostumeSummon> evt)
        {
            try
            {
                if (evt.Exception is null && evt.Action is { } costumeSummon)
                {
                    var start = System.DateTimeOffset.UtcNow;
                    var outputState = new World(_blockChainStates.GetWorldState(evt.OutputState));
                    var avatarAddress = costumeSummon.AvatarAddress;

                    if (!_avatars.Contains(avatarAddress))
                    {
                        _avatars.Add(avatarAddress);
                        _avatarList.Add(AvatarData.GetAvatarInfo(
                            outputState,
                            evt.Signer,
                            avatarAddress,
                            _blockTimeOffset,
                            Nekoyume.Model.EnumType.BattleType.Adventure
                        ));
                    }

                    var sheets = outputState.GetSheets(
                        sheetTypes: new[]
                        {
                            typeof(CostumeItemSheet),
                            typeof(CostumeSummonSheet),
                        });
                    var costumeSheet = sheets.GetSheet<CostumeItemSheet>();
                    var summonSheet = sheets.GetSheet<CostumeSummonSheet>();
                    var summonRow = summonSheet.OrderedList!.FirstOrDefault(row => row.GroupId == evt.Action.GroupId);

                    _costumeSummonList.Add(CostumeSummonData.GetSummonInfo(
                        evt.Signer,
                        evt.BlockIndex,
                        _blockTimeOffset,
                        costumeSheet,
                        summonRow!,
                        new ReplayRandom(evt.RandomSeed),
                        costumeSummon
                    ));

                    Log.Debug(
                        $"[DataProvider] Stored CostumeSummon action in block #{evt.BlockIndex}. Time taken: {(DateTimeOffset.UtcNow - start).Milliseconds} ms"
                    );
                }
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "[DataProvider] RenderSubscriber Error: {ErrorMessage}, StackTrace: {StackTrace}",
                    e.Message,
                    e.StackTrace
                );
            }
        }

        private void ClearSummonList()
        {
            Log.Debug("[Summon] Clear summon list");
            _auraSummonList.Clear();
            _auraSummonFailList.Clear();
            _runeSummonList.Clear();
            _runeSummonFailList.Clear();
            _costumeSummonList.Clear();
        }
    }
}
