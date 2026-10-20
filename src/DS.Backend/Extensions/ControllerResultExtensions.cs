using DS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Extensions
{
    public static class ControllerResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result, bool noContent = false)
        {
            if (result.Outcome != ServiceOutcome.Success)
            {
                return controller.ToActionResult(new ServiceResult(result.Outcome, result.Error), noContent);
            }

            if (noContent) return controller.NoContent();

            return result.Value == null ? controller.Ok() : controller.Ok(result.Value);
        }

        public static IActionResult ToActionResult(this ControllerBase controller, ServiceResult result, bool noContent = false)
        {
            switch (result.Outcome)
            {
                case ServiceOutcome.Success:
                    if (noContent) return controller.NoContent();

                    return controller.Ok();
                case ServiceOutcome.Invalid:
                    return result.Error == null ? controller.BadRequest() : controller.BadRequest(result.Error);
                case ServiceOutcome.NotFound:
                    return result.Error == null ? controller.NotFound() : controller.NotFound(result.Error);
                case ServiceOutcome.Forbidden:
                    return controller.Forbid();
                case ServiceOutcome.Conflict:
                    return controller.Conflict(result.Error);
                default:
                    return controller.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
