using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Microsoft.Extensions.DependencyInjection;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using NineChronicles.DataProvider.DataRendering;
using NineChronicles.DataProvider.Store;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class ItemEnhancementDataTest : TestBase
{
    private Address _signer = new PrivateKey().Address;
    private Currency _currency = Currency.Uncapped("NCG", 2, null);

    [Fact]
    public void GetItemEnhancementInfo()
    {
        var inputState = new World(MockWorldState.CreateModern());
        IWorld outputState = new World(GetMockState());
        var blockIndex = 1L;
        var blockTimeOffset = DateTimeOffset.UtcNow;
        var totalHammerCount = 10;
        var totalHammerExp = 2000;
        var worldSheet = new WorldSheet();
        worldSheet.Set(@"id,name,stage_begin,stage_end
1,Yggdrasil,1,50
2,Alfheim,51,100
3,Svartalfheim,101,150
4,Asgard,151,200
10001,Mimisbrunnr,10000001,10000020
5,Muspelheim,201,250
6,Jotunheim,251,300
7,Niflheim,301,350
8,Hel,351,400");
        var avatarName = "test";
        var avatarSheets = new AvatarSheets(worldSheet, new QuestSheet(), new QuestRewardSheet(),
            new QuestItemRewardSheet(), new EquipmentItemRecipeSheet(), new EquipmentItemSubRecipeSheet());
        var avatarAddress = new PrivateKey().Address;
        var worldInformation = new WorldInformation(blockIndex, avatarSheets.WorldSheet,
            GameConfig.IsEditor, avatarName);
        var questList = new QuestList(
            avatarSheets.QuestSheet,
            avatarSheets.QuestRewardSheet,
            avatarSheets.QuestItemRewardSheet,
            avatarSheets.EquipmentItemRecipeSheet,
            avatarSheets.EquipmentItemSubRecipeSheet
        );
        var avatarState = new AvatarState(avatarAddress, _signer, 1L, questList, worldInformation, default, avatarName);
        var equipmentRow = new EquipmentItemSheet.Row();
        equipmentRow.Set("10100000,Weapon,0,Normal,0,ATK,1,2,10100000,10".Split(","));
        var itemId = Guid.NewGuid();
        var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, itemId, 0L);
        avatarState.inventory.AddItem(equipment);
        outputState = outputState.SetAvatarState(avatarAddress, avatarState);

        var itemEnhancement = new ItemEnhancement
        {
            itemId = itemId,
            slotIndex = 1,
            materialIds = new List<Guid>
            {
                new()
            },
            avatarAddress = avatarAddress,
            hammers = new Dictionary<int, int>()
        };

        var provider = Services.BuildServiceProvider();
        var store = provider.GetRequiredService<MySqlStore>();
        store.StoreAgent(_signer);
        store.StoreAvatar(avatarAddress, _signer, avatarState.name, DateTimeOffset.UtcNow, 1, null, null, 0);
        var data = ItemEnhancementData.GetItemEnhancementInfo(
            inputState,
            outputState,
            _signer,
            itemEnhancement.avatarAddress,
            itemEnhancement.slotIndex,
            Guid.Empty,
            itemEnhancement.materialIds,
            itemEnhancement.itemId,
            itemEnhancement.Id,
            blockIndex,
            blockTimeOffset,
            totalHammerCount,
            totalHammerExp);
        Context.ItemEnhancements!.Add(data);
        Context.SaveChanges();

        var model = Context.ItemEnhancements.First();
        Assert.Equal(blockIndex, model.BlockIndex);
        Assert.Equal(equipment.Exp, model.Exp);
        Assert.Equal(totalHammerCount, model.HammerCount);
        Assert.Equal(totalHammerExp, model.HammerExp);
    }

    protected override IWorldState GetMockState()
    {
        var mockWorldState = MockWorldState.CreateModern();
        var goldCurrencyState = new GoldCurrencyState(_currency);
        mockWorldState = mockWorldState.SetAccount(
            ReservedAddresses.LegacyAccount,
            new Account(mockWorldState.GetAccountState(ReservedAddresses.LegacyAccount))
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
        ).SetBalance(_signer, 1 * _currency);
        return mockWorldState;
    }
}
