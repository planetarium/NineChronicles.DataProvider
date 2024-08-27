using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Microsoft.Extensions.DependencyInjection;
using NineChronicles.DataProvider.Store;
using NineChronicles.DataProvider.Store.Models.CustomCraft;
using Xunit;

namespace NineChronicles.DataProvider.Tests.Store;

public class CustomEquipmentCraftStoreTest : TestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpsertCraftCount(bool hasPrevData)
    {
        const string itemSubType = "Weapon";
        const int iconId = 90000001;

        var provider = Services.BuildServiceProvider();
        var store = provider.GetRequiredService<MySqlStore>();

        var now = DateTimeOffset.UtcNow;
        var avatarAddress = new PrivateKey().Address;
        store.StoreAgent(avatarAddress);
        store.StoreAvatar(avatarAddress, avatarAddress, "name", now, 1, null, null, 0);
        var prevDataCount = 0;

        if (hasPrevData)
        {
            prevDataCount = new Random().Next(1, 11);
            var cecList = new List<CustomEquipmentCraftModel>();
            var guid = Guid.NewGuid().ToString();
            for (var i = 0; i < prevDataCount; i++)
            {
                cecList.Add(new CustomEquipmentCraftModel
                {
                    Id = $"{guid}_{i}",
                    BlockIndex = 1L,
                    AvatarAddress = avatarAddress.ToString(),
                    EquipmentItemId = 10010001,
                    RecipeId = 1,
                    SlotIndex = 1,
                    ItemSubType = itemSubType,
                    IconId = iconId,
                    ElementalType = "Normal",
                    DrawingAmount = 1,
                    DrawingToolAmount = 1,
                    NcgCost = 0,
                    AdditionalCost = "",
                    Date = DateOnly.FromDateTime(now.DateTime),
                    TimeStamp = now
                });
            }

            await store.StoreCustomEquipmentCraftList(cecList);
        }

        var countList = store.GetCustomEquipmentCraftCount(itemSubType);
        if (hasPrevData)
        {
            Assert.Single(countList);
            var prevData = countList.First();
            Assert.Equal(itemSubType, prevData.ItemSubType);
            Assert.Equal(iconId, prevData.IconId);
            Assert.Equal(prevDataCount, prevData.Count);
        }
        else
        {
            Assert.Empty(countList);
        }

        // Add new data
        await store.StoreCustomEquipmentCraftList(new List<CustomEquipmentCraftModel>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                BlockIndex = 1L,
                AvatarAddress = avatarAddress.ToString(),
                EquipmentItemId = 10010001,
                RecipeId = 1,
                SlotIndex = 1,
                ItemSubType = itemSubType,
                IconId = iconId,
                ElementalType = "Normal",
                DrawingAmount = 1,
                DrawingToolAmount = 1,
                NcgCost = 0,
                AdditionalCost = "",
                Date = DateOnly.FromDateTime(now.DateTime),
                TimeStamp = now
            }
        });
        countList = store.GetCustomEquipmentCraftCount(itemSubType);

        // Test
        Assert.Single(countList);
        var data = countList.First();
        Assert.Equal(itemSubType, data.ItemSubType);
        Assert.Equal(iconId, data.IconId);
        Assert.Equal(prevDataCount + 1, data.Count);
    }

    protected override IWorldState GetMockState()
    {
        return MockWorldState.CreateModern();
    }
}
