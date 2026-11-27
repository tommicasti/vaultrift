namespace Vaultrift.Infrastructure.Cosmos;

public sealed class CosmosKeyRing(IDocumentContainer container)
{
    public const string Partition = "keyring";

    public IReadOnlyList<string> All() =>
        container
            .Query<KeyDocument>(new DocumentQuery(DocumentTypes.Key, "pk", Partition, Partition))
            .Select(document => document.Xml)
            .ToList();

    public void Save(string name, string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        container.Upsert(new KeyDocument($"key-{name}", Partition, DocumentTypes.Key, xml), Partition);
    }
}
