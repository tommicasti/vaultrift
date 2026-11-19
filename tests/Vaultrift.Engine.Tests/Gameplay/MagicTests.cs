using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class MagicTests
{
    private static readonly SpellDefinition Bolt = new("test-bolt", "Bolt", Element.Fire, SpellShape.Bolt, power: 4, manaCost: 3);
    private static readonly SpellDefinition Fireball = new("test-fireball", "Fireball", Element.Fire, SpellShape.Burst, power: 5, manaCost: 6, size: 1);
    private static readonly SpellDefinition Squall = new("test-squall", "Squall", Element.Wind, SpellShape.Line, power: 5, manaCost: 6, size: 2);
    private static readonly SpellDefinition Chain = new("test-chain", "Chain", Element.Lightning, SpellShape.Chain, power: 5, manaCost: 6, size: 3);
    private static readonly SpellDefinition FrostNova = new("test-nova", "Frost Nova", Element.Ice, SpellShape.Nova, power: 3, manaCost: 6, size: 1, alwaysTriggers: true);
    private static readonly SpellDefinition Tempest = new("test-tempest", "Tempest", Element.Lightning, SpellShape.Storm, power: 6, manaCost: 10, size: 5);

    private static readonly WeaponDefinition Staff =
        new("test-staff", "Staff", Rarity.Common, WeaponType.Staff, [new StatModifier(StatType.Attack, 2)], FixedElement: Element.Fire) { Spell = Bolt };

    private static readonly string[] Hall =
    [
        "##############",
        "#............#",
        "#............#",
        "#............#",
        "##############"
    ];

    private static readonly CharacterClass Mage = new(
        "mage", "Mage", new Stats(maxHealth: 30, attack: 5, defense: 0, maxMana: 30),
        new WeaponMastery(WeaponType.Staff, new StatModifier(StatType.Attack, 0)), Ability: ClassAbility.Overload);

    private static Player NewPlayer(int mana = 30, bool withStaff = true, int x = 1, int y = 2)
    {
        var player = new Player(new Position(x, y), new Stats(maxHealth: 30, attack: 5, defense: 0, maxMana: mana)) { Facing = Direction.Right };

        if (withStaff)
            player.Give(new ItemInstance(Staff));

        return player;
    }

    private static Enemy Dummy(int id, int x, int y = 2, int health = 50) =>
        new(id, new Position(x, y), new Stats(maxHealth: health, attack: 1, defense: 0));

    private static Game NewGame(Player player, params Enemy[] enemies) =>
        new(DungeonMap.Parse(Hall), player, enemies, new FixedRandom(Dice.AlwaysFail(200)));

    private static ItemInstance Scroll(SpellDefinition spell) =>
        new(new ScrollDefinition("scroll-" + spell.Id, spell.Name, Rarity.Rare, spell));

    private static int AddScroll(Player player, SpellDefinition spell)
    {
        player.Inventory.TryAdd(Scroll(spell));
        return player.Inventory.Entries.First(entry => entry.Item.Definition is ScrollDefinition scroll && scroll.Spell == spell).Id;
    }

    private static List<int> Damages(TurnResult result) =>
        result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.Result.Damage).ToList();

    [Fact]
    public void AStaff_CastsItsSpell_ForSpellPowerPlusAttack_AndSpendsMana()
    {
        var player = NewPlayer();
        var game = NewGame(player, Dummy(1, 6));

        var result = game.Attack();

        Assert.Single(result.Events.OfType<SpellCast>());
        Assert.Equal(new List<int> { 4 + 7 }, Damages(result));
        Assert.Equal(27, player.Mana);
    }

    [Fact]
    public void Spells_HaveNoPenaltyUpClose()
    {
        var game = NewGame(NewPlayer(), Dummy(1, 2));

        Assert.Equal(new List<int> { 11 }, Damages(game.Attack()));
    }

    [Fact]
    public void WithoutMana_TheStaffBonksWeakly()
    {
        var game = NewGame(NewPlayer(mana: 2), Dummy(1, 2));

        var result = game.Attack();

        Assert.Contains(new SpellRejected(SpellResult.NotEnoughMana), result.Events);
        Assert.Equal(new List<int> { 3 }, Damages(result));
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyIgnited);
    }

    [Fact]
    public void AScroll_WithoutAStaff_CastsOnce_AndPassesTheTurn()
    {
        var player = NewPlayer(withStaff: false);
        var entry = AddScroll(player, Fireball);
        var game = NewGame(player, Dummy(1, 6), Dummy(2, 7, 3), Dummy(3, 9));

        var result = game.UseItem(entry);

        Assert.True(result.TurnPassed);
        Assert.Equal(2, Damages(result).Count);
        Assert.Empty(player.Inventory.Entries);
        Assert.Equal(24, player.Mana);
    }

    [Fact]
    public void AScroll_WithoutEnoughMana_IsKept()
    {
        var player = NewPlayer(mana: 5, withStaff: false);
        var entry = AddScroll(player, Fireball);
        var game = NewGame(player, Dummy(1, 6));

        var result = game.UseItem(entry);

        Assert.False(result.TurnPassed);
        Assert.Contains(new UseRejected(UseResult.NotEnoughMana), result.Events);
        Assert.NotNull(player.Inventory.Find(entry));
    }

    [Fact]
    public void Burst_HitsEveryEnemyAroundTheImpact()
    {
        var player = NewPlayer();
        var entry = AddScroll(player, Fireball);
        var game = NewGame(player, Dummy(1, 6), Dummy(2, 7, 3), Dummy(3, 8));

        var result = game.UseItem(entry);

        Assert.Equal(new List<int> { 1, 2 }, result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.EnemyId).ToList());
        Assert.Equal(9, Assert.Single(result.Events.OfType<SpellCast>()).Area.Count);
    }

    [Fact]
    public void Line_HitsTheWholeRow_AndPushesTwoTiles()
    {
        var player = NewPlayer();
        var entry = AddScroll(player, Squall);
        var first = Dummy(1, 3);
        var second = Dummy(2, 6);
        var game = NewGame(player, first, second);

        var result = game.UseItem(entry);

        Assert.Equal(2, Damages(result).Count);
        Assert.Contains(new EnemyKnockedBack(1, new Position(5, 2)), result.Events);
        Assert.Contains(new EnemyKnockedBack(2, new Position(8, 2)), result.Events);
    }

    [Fact]
    public void Chain_JumpsToNearbyEnemies_UpToThreeTargets()
    {
        var player = NewPlayer();
        var entry = AddScroll(player, Chain);
        var game = NewGame(player, Dummy(1, 4), Dummy(2, 5, 1), Dummy(3, 7, 1), Dummy(4, 12, 3));

        var result = game.UseItem(entry);

        Assert.Equal(new List<int> { 1, 2, 3 }, result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.EnemyId).ToList());
    }

    [Fact]
    public void Nova_FreezesEveryEnemyAroundTheHero()
    {
        var player = NewPlayer(x: 5, y: 2);
        var entry = AddScroll(player, FrostNova);
        var game = NewGame(player, Dummy(1, 6), Dummy(2, 4, 1), Dummy(3, 9));

        var result = game.UseItem(entry);

        Assert.Equal(2, result.Events.OfType<EnemyFrozen>().Count());
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is PlayerAttackedEnemy { EnemyId: 3 });
    }

    [Fact]
    public void Storm_StrikesEveryEnemyWithinItsRadius()
    {
        var player = NewPlayer(x: 1, y: 2);
        var entry = AddScroll(player, Tempest);
        var game = NewGame(player, Dummy(1, 3, 1), Dummy(2, 6, 3), Dummy(3, 12));

        var result = game.UseItem(entry);

        Assert.Equal(new List<int> { 1, 2 }, result.Events.OfType<PlayerAttackedEnemy>().Select(hit => hit.EnemyId).ToList());
    }

    [Fact]
    public void LearningAScroll_NeedsAStaff()
    {
        var player = NewPlayer(withStaff: false);
        var entry = AddScroll(player, Fireball);
        var game = NewGame(player);

        Assert.Contains(new SpellRejected(SpellResult.NoStaff), game.LearnSpell(entry).Events);
        Assert.NotNull(player.Inventory.Find(entry));
    }

    [Fact]
    public void ALearnedSpell_JoinsTheSpellbook_AndCanBeSelected()
    {
        var player = NewPlayer();
        var entry = AddScroll(player, Fireball);
        var game = NewGame(player, Dummy(1, 6), Dummy(2, 7, 3));

        var learned = game.LearnSpell(entry);
        var selected = game.SelectSpell(1);
        var cast = game.Attack();

        Assert.False(learned.TurnPassed);
        Assert.Contains(new SpellLearned(Fireball), learned.Events);
        Assert.Empty(player.Inventory.Entries);
        Assert.False(selected.TurnPassed);
        Assert.Equal(new List<SpellDefinition> { Bolt, Fireball }, player.Spells.ToList());
        Assert.Equal(Fireball, Assert.Single(cast.Events.OfType<SpellCast>()).Spell);
        Assert.Equal(2, Damages(cast).Count);
    }

    [Fact]
    public void TheSpellbook_HoldsThreeLearnedSpells_AndNoDuplicates()
    {
        var player = NewPlayer();
        var game = NewGame(player);

        game.LearnSpell(AddScroll(player, Fireball));
        Assert.Contains(new SpellRejected(SpellResult.AlreadyKnown), game.LearnSpell(AddScroll(player, Fireball)).Events);

        game.LearnSpell(AddScroll(player, Squall));
        game.LearnSpell(AddScroll(player, Chain));

        Assert.Contains(new SpellRejected(SpellResult.SpellbookFull), game.LearnSpell(AddScroll(player, FrostNova)).Events);
        Assert.Equal(4, player.Spells.Count);
    }

    [Fact]
    public void SelectingAMissingSpell_IsRejected()
    {
        var game = NewGame(NewPlayer());

        Assert.Contains(new SpellRejected(SpellResult.UnknownSpell), game.SelectSpell(2).Events);
    }

    [Fact]
    public void Overload_MakesTheNextSpellFree_AndWider()
    {
        var player = new Player(new Position(1, 2), Mage) { Facing = Direction.Right };
        player.Give(new ItemInstance(Staff));
        var game = NewGame(player, Dummy(1, 6), Dummy(2, 7, 3));

        var ability = game.UseAbility();
        var cast = game.Attack();

        Assert.False(ability.TurnPassed);
        Assert.Equal(30, player.Mana);
        Assert.Equal(2, Damages(cast).Count);
        Assert.False(player.IsOverloaded);
    }

    [Fact]
    public void TheStaffMastery_MakesSpellsCheaper()
    {
        var player = new Player(new Position(1, 2), Mage) { Facing = Direction.Right };
        player.Give(new ItemInstance(Staff));

        Assert.Equal(2, player.SpellCost(Bolt));
    }

    [Fact]
    public void Siphon_RestoresManaOnEveryKill()
    {
        var player = NewPlayer();
        player.Give(TestGear.Ring(EffectKind.ManaSiphon, 5));
        var game = NewGame(player, Dummy(1, 4, health: 5));

        var result = game.Attack();

        Assert.Contains(new ManaSiphoned(3), result.Events);
        Assert.Equal(30, player.Mana);
    }

    [Fact]
    public void TheSecondQuickSlot_CanHoldAScroll()
    {
        var player = NewPlayer(withStaff: false);
        var entry = AddScroll(player, Fireball);
        var game = NewGame(player, Dummy(1, 6));

        game.AssignQuickSlot(entry, slot: 1);
        var result = game.UseQuickSlot(1);

        Assert.True(result.TurnPassed);
        Assert.Single(result.Events.OfType<SpellCast>());
    }
}
