using DS.Application.Services.Materials;
using DS.Domain;
using DS.Backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers;

[Route("/api/v1/material")]
[Authorize(Roles = nameof(AppRoles.MaterialView))]
public class MaterialApiController(MaterialService materialService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return this.ToActionResult(await materialService.GetMaterials());
    }
}