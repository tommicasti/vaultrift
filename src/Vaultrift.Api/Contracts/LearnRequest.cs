namespace Vaultrift.Api.Contracts;

public sealed class LearnRequest
{
    public required int EntryId { get; init; }

    public required int Key { get; init; }
}
