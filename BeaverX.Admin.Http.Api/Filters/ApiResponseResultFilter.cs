using BeaverX.Admin.Http.Api.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeaverX.Admin.Http.Api.Filters;

public class ApiResponseResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (ShouldSkip(context.Result))
        {
            await next();
            return;
        }

        context.Result = context.Result switch
        {
            ObjectResult objectResult => new ObjectResult(ApiResponse<object>.Success(objectResult.Value, context.HttpContext))
            {
                StatusCode = objectResult.StatusCode ?? StatusCodes.Status200OK
            },
            EmptyResult or NoContentResult => new ObjectResult(ApiResponse<object>.Success(null, context.HttpContext))
            {
                StatusCode = StatusCodes.Status200OK
            },
            _ => context.Result
        };

        await next();
    }

    private static bool ShouldSkip(IActionResult result)
    {
        if (result is FileResult or PhysicalFileResult or VirtualFileResult or FileStreamResult or ContentResult or RedirectResult or RedirectToActionResult or RedirectToRouteResult)
            return true;

        if (result is ObjectResult { Value: not null } objectResult)
        {
            var valueType = objectResult.Value.GetType();
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>)) return true;
            if (objectResult.StatusCode is >= 400) return true;
        }

        return false;
    }
}
