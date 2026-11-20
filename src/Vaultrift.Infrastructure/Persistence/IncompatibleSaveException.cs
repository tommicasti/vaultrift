namespace Vaultrift.Infrastructure.Persistence;

public sealed class IncompatibleSaveException(string code, Exception inner)
    : Exception($"Save {code} cannot be loaded by this version of Vaultrift.", inner)
{
    public string Code { get; } = code;
}
