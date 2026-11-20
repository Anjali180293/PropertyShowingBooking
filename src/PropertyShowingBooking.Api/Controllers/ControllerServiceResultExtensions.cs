using Microsoft.AspNetCore.Mvc;
using PropertyShowingBooking.Api.Services;

namespace PropertyShowingBooking.Api.Controllers;

internal static class ControllerServiceResultExtensions
{
    public static IActionResult FromServiceResult<T>(
        this ControllerBase controller,
        ServiceResult<T> result,
        Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return result.FailureKind switch
        {
            ServiceFailureKind.NotFound =>
        controller.NotFound(new { message = result.ErrorMessage }),

    ServiceFailureKind.Conflict =>
        controller.Conflict(new { message = result.ErrorMessage }),

    ServiceFailureKind.Validation =>
        controller.BadRequest(new ValidationProblemDetails(result.Errors!)),

    _ => controller.StatusCode(500)
        };
    }
}
