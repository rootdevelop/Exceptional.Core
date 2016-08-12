using Microsoft.AspNetCore.Mvc.Filters;

namespace Exceptional.Core.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {

        public bool RollupPerServer { get; set; }

        public void OnException(ExceptionContext context)
        {
            var error = new Error(context.Exception, RollupPerServer, context.HttpContext);
           ErrorStore.LogException(error);
        }
    }
}
