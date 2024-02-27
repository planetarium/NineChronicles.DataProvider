using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nekoyume.Model.Stat;
using NineChronicles.DataProvider.Store;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class ActivateCollectionTest : TestBase
{

    [Fact]
    public void RelationShip()
    {
        var avatarAddress = new PrivateKey().Address;
        var provider = Services.BuildServiceProvider();
        var store = provider.GetRequiredService<MySqlStore>();
        store.StoreAgent(avatarAddress);
        var now = DateTimeOffset.UtcNow;
        store.StoreAvatar(avatarAddress, avatarAddress, "name", now, 1, null, null, 0);
        var avatar = store.GetAvatar(avatarAddress, true);
        var actionId = Guid.NewGuid().ToString();
        for (int i = 0; i < 2; i++)
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.HP, StatModifier.OperationType.Add, 1L),
                new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, 1L)
            };
            var options = new List<CollectionOptionModel>();
            var collection = new ActivateCollectionModel
            {
                Avatar = avatar,
                CollectionId = 1 + i,
                BlockIndex = 1L + i,
                ActionId = actionId,
                Options = options,
            };
            foreach (var modifier in modifiers)
            {
                options.Add(new CollectionOptionModel
                {
                    Value = modifier.Value,
                    OperationType = modifier.Operation.ToString(),
                    StatType = modifier.StatType.ToString(),
                });
            }
            avatar.ActivateCollections.Add(collection);
        }

        store.UpdateAvatar(avatar);
        Assert.Equal(2, Context.Avatars!.Include(avatarModel => avatarModel.ActivateCollections).First().ActivateCollections.Count);
        Assert.Equal(2, Context.ActivateCollections.Count(p => p.ActionId == actionId));
    }

    public void Dispose()
    {
        CleanUp();
    }

    protected override IWorldState GetMockState()
    {
        return new MockWorldState();
    }
}
