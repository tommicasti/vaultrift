using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Api.Contracts;

public sealed class EntryRequest
{
    public required int EntryId { get; init; }

    public EquipmentSlot? Hand { get; init; }
}
