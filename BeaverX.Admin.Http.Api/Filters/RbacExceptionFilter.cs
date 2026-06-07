using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeaverX.Admin.Http.Api.Filters;

public class RbacExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var message = context.Exception switch
        {
            RbacException rbacException => rbacException.Message,
            StorageException storageException => storageException.Message,
            _ => null
        };

        if (message == null)
        {
            return;
        }

        context.Result = new BadRequestObjectResult(new
        {
            message
        });
        context.ExceptionHandled = true;
    }
}
