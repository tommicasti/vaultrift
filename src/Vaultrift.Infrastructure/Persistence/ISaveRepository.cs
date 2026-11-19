namespace Vaultrift.Infrastructure.Persistence;

public interface ISaveRepository
{
    void Upsert(SaveRecord record);

    SaveRecord? FindById(Guid id);

    SaveRecord? FindByCode(string code);
}
