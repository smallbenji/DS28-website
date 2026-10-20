using DS.Application.Services.Materials;
using DS.Domain;
using DS.Website.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers.ApiControllers;

[Route("/api/v1/material")]
[Authorize(Roles = nameof(AppRoles.MaterialView))]
public class MaterialApiController(MaterialService materialService) : Controller
{
    public async Task<IActionResult> Index()
    {
        return this.ToActionResult(await materialService.GetMaterials());
    }
}