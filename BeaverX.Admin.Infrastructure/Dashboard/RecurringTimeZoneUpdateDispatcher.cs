using System.Net;
using System.Text.Json;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Infrastructure.Dashboard;

public sealed class RecurringTimeZoneUpdateDispatcher : IDashboardDispatcher
{
    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;

        if (!HttpMethods.IsPost(request.Method))
        {
            response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        var form = await request.ReadFormAsync(httpContext.RequestAborted);
        var recurringJobId = form["id"].ToString();
        var timeZoneId = form["timeZoneId"].ToString();

        try
        {
            var service = httpContext.RequestServices.GetRequiredService<Scheduling.IRecurringJobCronService>();
            await service.UpdateTimeZoneAsync(recurringJobId, timeZoneId, httpContext.RequestAborted);

            if (IsJsonRequest(request))
            {
                response.ContentType = "application/json; charset=utf-8";
                await response.WriteAsync(
                    JsonSerializer.Serialize(new { success = true }),
                    httpContext.RequestAborted);
                return;
            }

            response.Redirect($"{request.PathBase}/recurring");
        }
        catch (Exception ex)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            if (IsJsonRequest(request))
            {
                response.ContentType = "application/json; charset=utf-8";
                await response.WriteAsync(
                    JsonSerializer.Serialize(new { success = false, message = ex.Message }),
                    httpContext.RequestAborted);
                return;
            }

            await response.WriteAsync(WebUtility.HtmlEncode(ex.Message), httpContext.RequestAborted);
        }
    }

    private static bool IsJsonRequest(HttpRequest request) =>
        request.Headers.Accept.Any(x => x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true) ||
        string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
