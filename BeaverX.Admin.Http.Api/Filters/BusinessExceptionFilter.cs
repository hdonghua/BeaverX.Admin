using BeaverX.Admin.Application.Contracts.Storage;
using BeaverX.Admin.Domain.Shared;
using BeaverX.Admin.Http.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeaverX.Admin.Http.Api.Filters;

public class BusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case StorageNotFoundException storageNotFoundException:
                context.Result = new NotFoundObjectResult(ApiResponse<object>.Fail(
                    ApiResponseCodes.NotFound,
                    storageNotFoundException.Message,
                    context.HttpContext));
                context.ExceptionHandled = true;
                return;
            case BusinessException businessException:
                context.Result = new BadRequestObjectResult(ApiResponse<object>.Fail(
                    ApiResponseCodes.BadRequest,
                    businessException.Message,
                    context.HttpContext));
                context.ExceptionHandled = true;
                return;
            case StorageException storageException:
                context.Result = new BadRequestObjectResult(ApiResponse<object>.Fail(
                    ApiResponseCodes.BadRequest,
                    storageException.Message,
                    context.HttpContext));
                context.ExceptionHandled = true;
                return;
            default:
                context.Result = new ObjectResult(ApiResponse<object>.Fail(
                    ApiResponseCodes.ServerError,
                    context.Exception.Message,
                    context.HttpContext))
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
