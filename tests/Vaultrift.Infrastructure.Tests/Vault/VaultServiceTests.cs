using Vaultrift.Engine.Gameplay;
using Vaultrift.Infrastructure.Accounts;
using Vaultrift.Infrastructure.Content;
using Vaultrift.Infrastructure.Persistence;
using Vaultrift.Infrastructure.Realtime;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Infrastructure.Tests.Vault;

public sealed class VaultServiceTests
{
    private readonly MemoryVaultRepository _vault = new();
    private readonly MemoryAccountRepository _accounts = new();
    private readonly MemorySaveRepository _saves = new();
    private readonly Guid _account = Guid.NewGuid();

    private VaultService Service() => new(_vault, _accounts, _saves);

    private static ClassDefinition Class(string id) => ClassCatalog.Find(id)!;

    private GameSession TurnBasedGame(Difficulty difficulty, string name = "Casti", bool lost = false)
    {
        var session = GameSession.Start(Guid.NewGuid(), SaveCode.Generate(), Class("mage"), name, 11, difficulty);
        var record = session.ToRecord();

        if (lost)
        {
            var save = session.ToSave();
            record = record with { Data = (save with { Game = save.Game with { IsLost = true } }).ToJson() };
        }

        _saves.Upsert(record);
        _accounts.Link(_account, session.Id);
        return session;
    }

    [Fact]
    public void HeroesAndWorldsAreSeparateThingsOfTheAccount()
    {
        var service = Service();

        var hero = service.CreateHero(_account, Class("knight"), "Casti")!;
        var world = service.CreateWorld(_account, Difficulty.Normal, "knight")!;
        var contents = service.Contents(_account);

        Assert.Equal(hero.Id, Assert.Single(contents.Heroes).Id);
        Assert.Equal(world.Id, Assert.Single(contents.Worlds).Id);
        Assert.Equal(Difficulty.Normal, contents.Worlds[0].Difficulty);
        Assert.Empty(service.Contents(Guid.NewGuid()).Heroes);
    }

    [Fact]
    public void AHeroKeepsWhatItEarnedAcrossWorlds()
    {
        var service = Service();
        var hero = service.CreateHero(_account, Class("knight"), "Casti")!;
        hero.MarkPlayed(Difficulty.Hard);
        service.Save(hero);

        var again = service.FindHero(_account, hero.Id)!;

        Assert.Equal([Difficulty.Hard], again.Played);
        Assert.Null(service.FindHero(Guid.NewGuid(), hero.Id));
    }

    [Fact]
    public void WorldsRememberTheirFloor()
    {
        var service = Service();
        var world = service.CreateWorld(_account, Difficulty.Hard, "ranger")!;
        world.Reach(3);
        service.Save(world);

        var card = Assert.Single(service.Contents(_account).Worlds);

        Assert.Equal(3, card.Floor);
        Assert.Equal(3, card.DeepestFloor);
        Assert.Equal(3, service.FindWorld(_account, world.Id)!.Floor);
    }

    [Fact]
    public void ALostWorldDisappears()
    {
        var service = Service();
        var world = service.CreateWorld(_account, Difficulty.Hardcore, "knight")!;

        service.Lose(world);

        Assert.Empty(service.Contents(_account).Worlds);
    }

    [Fact]
    public void TurnBasedSavesBecomeAHeroAndAWorld()
    {
        var game = TurnBasedGame(Difficulty.Hard, "Elisa");
        var service = Service();

        var contents = service.Contents(_account);

        var hero = Assert.Single(contents.Heroes);
        Assert.Equal(game.Id, hero.Id);
        Assert.Equal("Elisa", hero.Name);
        Assert.Equal([Difficulty.Hard], hero.Played);
        var world = Assert.Single(contents.Worlds);
        Assert.Equal(VaultService.WorldIdFor(game.Id), world.Id);
        Assert.Equal(Difficulty.Hard, world.Difficulty);
        Assert.Equal(game.Game.FloorNumber, world.Floor);
        Assert.Single(Service().Contents(_account).Heroes);
    }

    [Fact]
    public void ASaveThatCannotBeReadIsSkippedWithoutHidingTheOthers()
    {
        var good = TurnBasedGame(Difficulty.Normal, "Elisa");
        var broken = TurnBasedGame(Difficulty.Hard, "Casti");
        var record = _saves.FindById(broken.Id)!;
        _saves.Upsert(record with { Data = record.Data.Replace("\"ClassId\":\"mage\"", "\"ClassId\":\"jester\"") });
        var skipped = new List<Guid>();

        var contents = new VaultService(_vault, _accounts, _saves, onUnreadable: (id, _) => skipped.Add(id)).Contents(_account);

        Assert.Equal(good.Id, Assert.Single(contents.Heroes).Id);
        Assert.Equal([broken.Id], skipped);
    }

    [Fact]
    public void AMigratedWorldKeepsItsProgressTotemsAndGrave()
    {
        var game = TurnBasedGame(Difficulty.Hard);
        var record = _saves.FindById(game.Id)!;
        var save = SaveFile.FromJson(record.Data);
        var ring = save.Game.Player.Equipment.First();
        var progressed = save with
        {
            Game = save.Game with
            {
                DeepestFloor = 6,
                ClearedFloors = [1, 2, 3],
                TotemUses = 2,
                Grave = new Engine.Persistence.GraveSnapshot(new Engine.World.Position(3, 3), [new Engine.Persistence.GraveItemSnapshot(ring.Key, ring.Value)], 2)
            }
        };
        _saves.Upsert(record with { Data = progressed.ToJson() });

        var world = Service().FindWorld(_account, VaultService.WorldIdFor(game.Id))!;

        Assert.Equal(6, world.DeepestFloor);
        Assert.Equal([1, 2, 3], world.ClearedFloors);
        Assert.Equal(2, world.TotemUses);
        var grave = Assert.Single(world.Graves);
        Assert.Equal(game.Id, grave.Owner);
        Assert.Equal(2, grave.Floor);
    }

    [Fact]
    public void AWorldRemembersClearedFloorsConquestAndGraves()
    {
        var service = Service();
        var world = service.CreateWorld(_account, Difficulty.Hard, "knight")!;
        var owner = Guid.NewGuid();
        world.MarkCleared(4);
        world.UseTotem();
        world.Conquer();
        world.SetGraves(4, [new GraveSave(owner, "Casti", 4, [])]);
        world.SetGraves(7, [new GraveSave(owner, "Casti", 7, [])]);
        service.Save(world);

        var again = service.FindWorld(_account, world.Id)!;

        Assert.True(again.IsCleared(4));
        Assert.Equal(1, again.TotemUses);
        Assert.True(again.IsConquered);
        Assert.Equal(7, Assert.Single(again.Graves).Floor);
        Assert.True(Assert.Single(service.Contents(_account).Worlds).IsConquered);
    }

    [Fact]
    public void AFallenHardcoreSaveBecomesAHeroWithoutAWorld()
    {
        TurnBasedGame(Difficulty.Hardcore, lost: true);

        var contents = Service().Contents(_account);

        Assert.Single(contents.Heroes);
        Assert.Empty(contents.Worlds);
    }

    [Fact]
    public void DeletingAMigratedHeroDoesNotBringItBack()
    {
        var game = TurnBasedGame(Difficulty.Normal);
        var service = Service();
        service.Contents(_account);

        Assert.True(service.DeleteHero(_account, game.Id));

        Assert.Empty(Service().Contents(_account).Heroes);
        Assert.Single(Service().Contents(_account).Worlds);
    }

    [Fact]
    public void NewHeroesStartWithTheirClassKitOnly()
    {
        var hero = Service().CreateHero(_account, Class("knight"), "Casti")!;

        Assert.DoesNotContain(hero.Player.Inventory.Entries, entry => entry.Item.Rarity == Engine.Items.Rarity.Platinum);
    }

    [Fact]
    public void ItemsMoveThroughTheDepositFromOneHeroToAnother()
    {
        var service = Service();
        var ranger = service.CreateHero(_account, Class("ranger"), "Arwen")!;
        var knight = service.CreateHero(_account, Class("knight"), "Casti")!;
        var bow = DragonReward.For("ranger", Difficulty.Hard, new Engine.Randomness.SeededRandom(3))[0];
        knight.Player.Inventory.TryAddBound(bow);
        service.Save(knight);
        var entry = service.FindHero(_account, knight.Id)!.Player.Inventory.Entries.First(item => item.Item.Id == bow.Id);

        Assert.Equal(DepositResult.Done, service.Store(_account, knight.Id, entry.Id));
        Assert.DoesNotContain(service.FindHero(_account, knight.Id)!.Player.Inventory.Entries, item => item.Item.Id == bow.Id);
        Assert.Equal(bow.Id, Assert.Single(service.Deposit(_account)).Item.Id);

        Assert.Equal(DepositResult.Done, service.Take(_account, ranger.Id, 0));
        Assert.Contains(service.FindHero(_account, ranger.Id)!.Player.Inventory.Entries, item => item.Item.Id == bow.Id);
        Assert.Empty(service.Deposit(_account));
    }

    [Fact]
    public void TheDepositRefusesWhatCannotFit()
    {
        var service = Service();
        var hero = service.CreateHero(_account, Class("knight"), "Casti")!;
        var bow = DragonReward.For("ranger", Difficulty.Normal, new Engine.Randomness.SeededRandom(1))[0];

        Assert.Equal(DepositResult.ItemNotFound, service.Store(_account, hero.Id, 999));
        Assert.Equal(DepositResult.ItemNotFound, service.Take(_account, hero.Id, 0));
        Assert.Equal(DepositResult.HeroNotFound, service.Take(_account, Guid.NewGuid(), 0));

        for (var index = 0; index < VaultService.DepositCapacity; index++)
            service.Keep(_account, bow);

        var full = service.FindHero(_account, hero.Id)!;
        full.Player.Inventory.TryAdd(new Engine.Items.ItemInstance(ItemCatalog.Get("sword-common")));
        service.Save(full);
        Assert.Equal(DepositResult.DepositFull, service.Store(_account, hero.Id, full.Player.Inventory.Entries[^1].Id));

        while (!full.Player.Inventory.IsFull)
            full.Player.Inventory.TryAdd(new Engine.Items.ItemInstance(ItemCatalog.Get("sword-common")));
        service.Save(full);

        Assert.Equal(DepositResult.BackpackFull, service.Take(_account, hero.Id, 0));
        Assert.Equal(VaultService.DepositCapacity, service.Deposit(_account).Count);
    }

    [Fact]
    public void HeroesChangeAndStoreTheirGearBetweenWorlds()
    {
        var service = Service();
        var hero = service.CreateHero(_account, Class("knight"), "Casti")!;
        var slot = hero.Player.Equipment.Keys.First();
        var worn = hero.Player.Equipment[slot].Item.Id;

        Assert.Equal(DepositResult.Done, service.StoreEquipped(_account, hero.Id, slot));
        Assert.False(service.FindHero(_account, hero.Id)!.Player.Equipment.ContainsKey(slot));
        Assert.Equal(worn, Assert.Single(service.Deposit(_account)).Item.Id);
        Assert.Equal(DepositResult.ItemNotFound, service.StoreEquipped(_account, hero.Id, slot));

        Assert.Equal(DepositResult.Done, service.Take(_account, hero.Id, 0));
        var back = service.FindHero(_account, hero.Id)!.Player.Inventory.Entries.First(entry => entry.Item.Id == worn);
        Assert.Equal(Engine.Gameplay.EquipResult.Equipped, service.Equip(_account, hero.Id, back.Id));
        Assert.True(service.FindHero(_account, hero.Id)!.Player.Equipment.ContainsKey(slot));

        Assert.Equal(Engine.Gameplay.UnequipResult.Unequipped, service.Unequip(_account, hero.Id, slot));
        Assert.Null(service.Equip(_account, Guid.NewGuid(), back.Id));
    }

    [Fact]
    public void TheCustodeLeavesTheWeaponOfTheVictorsClass()
    {
        var random = new Engine.Randomness.SeededRandom(5);

        Assert.Equal(["dragon-bow-2"], DragonReward.For("ranger", Difficulty.Hard, random).Select(item => item.Id));
        Assert.Equal(["dragon-blade-1", "dragon-aegis-1"], DragonReward.For("knight", Difficulty.Normal, random).Select(item => item.Id));
        Assert.Equal(["dragon-fangs-3"], DragonReward.For("rogue", Difficulty.Hardcore, random).Select(item => item.Id));
    }

    [Fact]
    public void AnAccountCannotHoardHeroesOrWorlds()
    {
        var service = Service();

        for (var index = 0; index < VaultService.MaxHeroes; index++)
            Assert.NotNull(service.CreateHero(_account, Class("knight"), null));

        for (var index = 0; index < VaultService.MaxWorlds; index++)
            Assert.NotNull(service.CreateWorld(_account, Difficulty.Normal, "knight"));

        Assert.Null(service.CreateHero(_account, Class("knight"), null));
        Assert.Null(service.CreateWorld(_account, Difficulty.Normal, "knight"));
    }

    [Fact]
    public void MigratedWorldsNeverStartBeyondTheLastLiveFloor()
    {
        Assert.True(LiveArena.LastFloor >= 1);
        var game = TurnBasedGame(Difficulty.Normal);

        var world = Service().FindWorld(_account, VaultService.WorldIdFor(game.Id))!;

        Assert.InRange(world.Floor, 1, LiveArena.LastFloor);
    }
}
