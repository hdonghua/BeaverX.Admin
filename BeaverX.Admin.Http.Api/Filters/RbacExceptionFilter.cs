using BeaverX.Admin.Application.Contracts.Rbac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeaverX.Admin.Http.Api.Filters;

public class RbacExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not RbacException rbacException)
        {
            return;
        }

        context.Result = new BadRequestObjectResult(new
        {
            message = rbacException.Message
        });
        context.ExceptionHandled = true;
    }
}
