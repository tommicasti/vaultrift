using Vaultrift.Engine.Items;

namespace Vaultrift.Api.Contracts;

public sealed class MarkRequest
{
    public required int EntryId { get; init; }

    public required ItemMark Mark { get; init; }
}
