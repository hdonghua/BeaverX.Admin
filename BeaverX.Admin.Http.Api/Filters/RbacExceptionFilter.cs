using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Application.Contracts.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeaverX.Admin.Http.Api.Filters;

public class RbacExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case StorageNotFoundException storageNotFoundException:
                context.Result = new NotFoundObjectResult(new { message = storageNotFoundException.Message });
                context.ExceptionHandled = true;
                return;
            case RbacException rbacException:
                context.Result = new BadRequestObjectResult(new { message = rbacException.Message });
                context.ExceptionHandled = true;
                return;
            case StorageException storageException:
                context.Result = new BadRequestObjectResult(new { message = storageException.Message });
                context.ExceptionHandled = true;
                return;
        }
    }
}
