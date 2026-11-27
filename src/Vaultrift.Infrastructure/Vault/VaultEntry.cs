namespace Vaultrift.Infrastructure.Vault;

public enum VaultKind
{
    Hero,
    World,
    Deposit
}

public sealed record VaultEntry(Guid Id, Guid AccountId, VaultKind Kind, string Data, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
