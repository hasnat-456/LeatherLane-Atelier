using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace LeatherLaneAtelier.Filters
{
    public class AntiXssFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;

            // Only check form data or query strings.
            // For JSON payloads, a custom model binder or middleware is preferred, 
            // but we can reject obvious <script> tags by checking the body stream if we really need to.
            // Since this is a quick security shield, we'll check route values and query parameters.
            
            foreach (var arg in context.ActionArguments.Values.OfType<string>())
            {
                if (arg != null && (arg.Contains("<") || arg.Contains(">")))
                {
                    context.Result = new BadRequestObjectResult(new { message = "Invalid characters detected. HTML/Script tags are not allowed." });
                    return;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
