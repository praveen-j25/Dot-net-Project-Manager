using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskManagerMVC.Filters;

/// <summary>
/// Custom Anti-Forgery Token validation filter
/// Validates CSRF tokens for state-changing operations
/// </summary>
public class ValidateAntiForgeryTokenAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
            await next();
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                message = "Invalid anti-forgery token. Please refresh the page and try again."
            });
        }
    }
}

/// <summary>
/// Auto-validate anti-forgery tokens for all POST, PUT, DELETE, PATCH requests
/// </summary>
public class AutoValidateAntiforgeryTokenAttribute : TypeFilterAttribute
{
    public AutoValidateAntiforgeryTokenAttribute() : base(typeof(AutoValidateAntiforgeryTokenFilter))
    {
    }
}

public class AutoValidateAntiforgeryTokenFilter : IAsyncActionFilter
{
    private readonly IAntiforgery _antiforgery;

    public AutoValidateAntiforgeryTokenFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpMethod = context.HttpContext.Request.Method;

        // Only validate for state-changing methods
        if (httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE" || httpMethod == "PATCH")
        {
            // Skip validation for API controllers with JWT
            var isApiController = context.Controller.GetType().Namespace?.Contains(".Api") ?? false;
            var hasJwtToken = context.HttpContext.Request.Headers["Authorization"]
                .FirstOrDefault()?.StartsWith("Bearer ") ?? false;

            if (!isApiController || !hasJwtToken)
            {
                try
                {
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "Invalid anti-forgery token"
                    });
                    return;
                }
            }
        }

        await next();
    }
}
