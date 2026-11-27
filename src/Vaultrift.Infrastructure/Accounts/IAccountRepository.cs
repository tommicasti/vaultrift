namespace Vaultrift.Infrastructure.Accounts;

public interface IAccountRepository
{
    bool TryCreate(Account account);

    Account? FindById(Guid id);

    Account? FindByName(string userName);

    IReadOnlyList<Account> All();

    LinkResult Link(Guid accountId, Guid gameId);

    bool Unlink(Guid accountId, Guid gameId);

    IReadOnlyList<Guid> GamesOf(Guid accountId);

    Guid? OwnerOf(Guid gameId);

    bool Unlock(Guid accountId, string trophyId, DateTimeOffset at);

    IReadOnlyDictionary<string, DateTimeOffset> TrophiesOf(Guid accountId);

    long AddToCounter(Guid accountId, string name, long amount);

    IReadOnlyDictionary<string, long> CountersOf(Guid accountId);
}

public enum LinkResult
{
    Linked,
    AlreadyYours,
    OwnedByOther
}
