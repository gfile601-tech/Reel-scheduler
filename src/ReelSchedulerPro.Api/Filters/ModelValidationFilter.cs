using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using Serilog;

namespace ReelSchedulerPro.Api.Filters;

/// <summary>
/// Model validation filter for automatic validation
/// </summary>
public class ModelValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<ModelValidationFilter> _logger;

    public ModelValidationFilter(ILogger<ModelValidationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            _logger.LogWarning("Model validation failed: {@Errors}", errors);

            context.Result = new BadRequestObjectResult(new
            {
                error = "Validation failed",
                errors = errors
            });

            return;
        }

        await next();
    }
}
