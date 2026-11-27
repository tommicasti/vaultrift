using Vaultrift.Infrastructure.Cosmos;

namespace Vaultrift.Infrastructure.Tests.Cosmos;

public class CosmosKeyRingTests
{
    [Fact]
    public void KeysSurviveANewRingOverTheSameContainer()
    {
        var container = new MemoryDocumentContainer();
        new CosmosKeyRing(container).Save("one", "<key id=\"one\" />");
        new CosmosKeyRing(container).Save("two", "<key id=\"two\" />");

        var keys = new CosmosKeyRing(container).All();

        Assert.Equal(2, keys.Count);
        Assert.Contains("<key id=\"one\" />", keys);
        Assert.Contains("<key id=\"two\" />", keys);
    }

    [Fact]
    public void SavingAKeyAgainReplacesIt()
    {
        var container = new MemoryDocumentContainer();
        var ring = new CosmosKeyRing(container);

        ring.Save("one", "<key id=\"one\" version=\"1\" />");
        ring.Save("one", "<key id=\"one\" version=\"2\" />");

        Assert.Equal(["<key id=\"one\" version=\"2\" />"], ring.All());
    }
}
