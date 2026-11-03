using DS.Application.FormTemplates;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers.ApiControllers
{
    [Route("api/v1/forms")]
    [ApiController]
    public class FormApiController(FormTemplateService formTemplateService) : Controller
    {
        [HttpGet("{id}")]
        public IActionResult GetTemplate(string id)
        {
            var template = formTemplateService.GetTemplate(id);
            if (template == null) return NotFound();

            return Ok(template);
        }

        [HttpGet("test")]
        public IActionResult GetTestTemplate()
        {
            return GetTemplate("test-form-template");
        }
    }
}
