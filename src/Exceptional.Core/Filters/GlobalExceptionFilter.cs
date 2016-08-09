using Microsoft.AspNetCore.Mvc.Filters;

namespace Exceptional.Core.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var error = new Error(context.Exception, context.HttpContext);
           ErrorStore.LogException(error);
        }
    }
}
