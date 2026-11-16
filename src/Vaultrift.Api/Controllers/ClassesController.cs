using Microsoft.AspNetCore.Mvc;
using Vaultrift.Api.Contracts;
using Vaultrift.Api.Mapping;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Api.Controllers;

[ApiController]
[Route("api/classes")]
public sealed class ClassesController : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<ClassResponse> GetAll() =>
        ClassCatalog.All.Select(ClassMapper.ToResponse).ToList();
}