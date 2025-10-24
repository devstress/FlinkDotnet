using Microsoft.AspNetCore.Mvc.Filters;

namespace FlinkDotNet.JobGateway.Filters;

/// <summary>
/// Action filter that logs model state validation errors for debugging purposes.
/// </summary>
internal sealed class ModelStateLoggingFilter : IActionFilter
{
    private readonly ILogger<ModelStateLoggingFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStateLoggingFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ModelStateLoggingFilter(ILogger<ModelStateLoggingFilter> logger) => _logger = logger;

    /// <summary>
    /// Called before the action executes. Logs any model state validation errors.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var errors = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .Select(kv => $"{kv.Key}:{string.Join("|", kv.Value!.Errors.Select(e => e.ErrorMessage))}");
        _logger.LogWarning("ModelState invalid for {Path}. Errors: {Errors}",
            context.HttpContext.Request.Path,
            string.Join("; ", errors));
    }

    /// <summary>
    /// Called after the action executes.
    /// </summary>
    /// <param name="context">The action executed context.</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
