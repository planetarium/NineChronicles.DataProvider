using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Stat;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class ActivateCollectionTest : TestBase
{

    [Fact]
    public async Task RelationShip()
    {
        var avatarAddress = new PrivateKey().Address;
        var now = DateTimeOffset.UtcNow;
        var agent = new AgentModel
        {
            Address = avatarAddress.ToString()
        };
        var avatar = new AvatarModel
        {
            Address = avatarAddress.ToString(),
            AgentAddress = avatarAddress.ToString(),
            Name = "name",
            AvatarLevel = 1,
            TitleId = null,
            ArmorId = null,
            Cp = 0,
            Timestamp = now
        };
        await Context.Agents.AddAsync(agent);
        await Context.Avatars.AddAsync(avatar);
        await Context.SaveChangesAsync();

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

            await Context.ActivateCollections.AddAsync(collection);
            Assert.Equal(avatar.Address, collection.AvatarAddress);
            Assert.Equal(2, collection.Options.Count);
        }
        await Context.SaveChangesAsync();
        Assert.Equal(2, avatar.ActivateCollections.Count);
        Assert.Equal(2, Context.ActivateCollections.Count(p => p.ActionId == actionId));
    }
    protected override IWorldState GetMockState()
    {
        return new MockWorldState();
    }
}
