using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Infrastructure.Accounts;

public sealed class LiveFeats(string classId)
{
    public string ClassId { get; } = classId;

    public bool Slew { get; set; }

    public bool SlewBoss { get; set; }

    public bool FoundExotic { get; set; }

    public bool CleanFloor { get; set; }

    public int FloorReached { get; set; }

    public int Falls { get; set; }

    public long Coins { get; set; }

    public Difficulty? Conquered { get; set; }

    public bool IsEmpty =>
        !Slew && !SlewBoss && !FoundExotic && !CleanFloor && FloorReached == 0 && Coins == 0 && Conquered is null;
}
