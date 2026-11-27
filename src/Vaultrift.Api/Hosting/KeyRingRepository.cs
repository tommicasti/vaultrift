using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Vaultrift.Infrastructure.Cosmos;

namespace Vaultrift.Api.Hosting;

public sealed class KeyRingRepository(CosmosKeyRing ring) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements() => ring.All().Select(XElement.Parse).ToList();

    public void StoreElement(XElement element, string friendlyName) => ring.Save(friendlyName, element.ToString(SaveOptions.DisableFormatting));
}
