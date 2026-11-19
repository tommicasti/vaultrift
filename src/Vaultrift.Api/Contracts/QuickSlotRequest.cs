namespace Vaultrift.Api.Contracts;

public sealed class QuickSlotRequest
{
    public int? EntryId { get; init; }

    public int Slot { get; init; }
}
