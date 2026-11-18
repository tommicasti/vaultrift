using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Api.Contracts;

public sealed class UnequipRequest
{
    public required EquipmentSlot Slot { get; init; }
}