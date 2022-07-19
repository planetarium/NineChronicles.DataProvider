namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Lib9c.Renderer;
    using Libplanet;
    using Libplanet.Assets;
    using Microsoft.Extensions.Hosting;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless;
    using Serilog;
    using static Lib9c.SerializeKeys;

    public class RenderSubscriber : BackgroundService
    {
        private const int InsertInterval = 500;
        private readonly BlockRenderer _blockRenderer;
        private readonly ActionRenderer _actionRenderer;
        private readonly ExceptionRenderer _exceptionRenderer;
        private readonly NodeStatusRenderer _nodeStatusRenderer;
        private readonly List<AgentModel> _agentList = new List<AgentModel>();
        private readonly List<AvatarModel> _avatarList = new List<AvatarModel>();
        private readonly List<WorldBossModel> _worldBossList = new List<WorldBossModel>();
        private int _renderCount = 0;

        public RenderSubscriber(
            NineChroniclesNodeService nodeService,
            MySqlStore mySqlStore
        )
        {
            _blockRenderer = nodeService.BlockRenderer;
            _actionRenderer = nodeService.ActionRenderer;
            _exceptionRenderer = nodeService.ExceptionRenderer;
            _nodeStatusRenderer = nodeService.NodeStatusRenderer;
            MySqlStore = mySqlStore;
        }

        internal MySqlStore MySqlStore { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _actionRenderer.EveryRender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        try
                        {
                            if (ev.Exception != null)
                            {
                                return;
                            }

                            /*
                             *    <통상적인 액션 추가작업 프로세스>
                             * 1. DB에 저장할 테이블 필드를 정하고 `NineChronicles.DataProvider/Store/Models/WorldBossModel.cs`처럼
                             *    데이터를 모델 추가한다.
                             * 2. 추가된 데이터 모델을 바탕으로 `NineChronicles.DataProvider/GraphQL/Types/WorldBossType.cs`처럼
                             *    GQL 타입을 추가한다.
                             * 3. `NineChronicles.DataProvider/Store/NineChroniclesContext.cs`에 DBSet을 추가한다.
                             * 4. `NineChronicles.DataProvider/Store/MySqlStore.cs`에 Insert, Delete, Select 함수를 추가한다.
                             * 5. `NineChronicles.DataProvider/RenderSubscriber.cs`에 액션 render 또는 unrender 시 저장/삭제하고 싶은 데이터를 가공하고
                             *     `MySqlStore.cs`에 추가한 Insert/Delete 함수를 사용한다.
                             * 6. `NineChronicles.DataProvider/GraphQL/Types/NineChroniclesSummaryQuery.cs`에서 GQL 쿼리를 추가하고
                             *    `MySqlStore.cs`에 추가한 Select 함수를 사용한다.
                             */

                            // renderCount가 InsertInterval까지 차면 리스트에 저장된 데이터를 한번에 bulk insert한다.
                            if (_renderCount == InsertInterval)
                            {
                                var start = DateTimeOffset.Now;
                                Log.Debug("Storing Data");

                                MySqlStore.StoreAgentList(_agentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                                MySqlStore.StoreAvatarList(_avatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                                MySqlStore.StoreWorldBossList(_worldBossList);
                                _renderCount = 0;
                                _agentList.Clear();
                                _avatarList.Clear();
                                _worldBossList.Clear();
                                var end = DateTimeOffset.Now;
                                Log.Debug($"Storing Data Complete. Time Taken: {(end - start).Milliseconds} ms.");
                            }

                            // WorldBoss 액션 render 시, 해당 데이터 리스트에 추가
                            if (ev.Action is WorldBoss worldBoss)
                            {
                                /*
                                 * 필요한 데이터 가공
                                 */
                                var start = DateTimeOffset.Now;
                                _agentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _avatarList.Add(new AvatarModel()
                                {
                                    /*
                                     * 데이터 추가
                                     * Address = worldBoss.AvatarAddress.ToString(),
                                     * AgentAddress = ev.Signer.ToString(),
                                     * Name = avatarName,
                                     * AvatarLevel = avatarLevel,
                                     * TitleId = avatarTitleId,
                                     * ArmorId = avatarArmorId,
                                     * Cp = avatarCp,
                                     */
                                });
                                _worldBossList.Add(new WorldBossModel()
                                {
                                    /*
                                     * 데이터 추가
                                     * Id = worldBoss.Id.ToString(),
                                     * BlockIndex = ev.BlockIndex,
                                     * AgentAddress = ev.Signer.ToString(),
                                     * AvatarAddress = battleArena.myAvatarAddress.ToString(),
                                     * AvatarLevel = avatarLevel,
                                     * TimeStamp = DateTimeOffset.Now,
                                     */
                                });

                                var end = DateTimeOffset.Now;
                                Log.Debug("Stored BattleArena action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RenderSubscriber: {message}", ex.Message);
                        }
                    });

            _actionRenderer.EveryUnrender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        try
                        {
                            if (ev.Exception != null)
                            {
                                return;
                            }

                            // WorldBoss 액션 unrender 시, 해당 데이터 삭제 (1 마이너 체제에선 아예 안 쓰임)
                            if (ev.Action is WorldBoss worldBoss)
                            {
                                MySqlStore.DeleteWorldBoss(worldBoss.Id);
                                Log.Debug("Deleted WorldBoss action in block #{index}", ev.BlockIndex);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RenderSubscriber: {message}", ex.Message);
                        }
                    });
            return Task.CompletedTask;
        }
    }
}
