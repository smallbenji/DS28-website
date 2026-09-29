using ClosedXML.Excel;
using DS.DTOs;
using DS.Website.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DS.Website.Controllers
{
    [Authorize(Roles = nameof(AppRoles.ExportsView))]
    [Route("api/v1/exports")]
    public class ExportsApiController(IEnumerable<DataExport> exports) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var visible = exports
                .Where(e => User.IsInRole(e.RequiredRole))
                .Select(e => new DataExportDto
                {
                    Key = e.Key,
                    Title = e.Title,
                    Description = e.Description,
                    FileName = e.FileName,
                    RequiredRole = e.RequiredRole
                })
                .ToList();

            return Ok(visible);
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> Download(string key, CancellationToken cancellationToken)
        {
            var export = exports.FirstOrDefault(e => e.Key == key);
            if (export == null) return NotFound("Dataudtrækket findes ikke.");
            if (!User.IsInRole(export.RequiredRole)) return Forbid();

            using var workbook = new XLWorkbook();
            await export.BuildAsync(workbook, cancellationToken);

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{export.FileName}.xlsx");
        }
    }
}
