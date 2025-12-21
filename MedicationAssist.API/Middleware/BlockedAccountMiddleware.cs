using System.Security.Claims;
using MedicationAssist.Domain.Repositories;

namespace MedicationAssist.API.Middleware;

/// <summary>
/// Middleware для блокировки доступа заблокированным пользователям
/// </summary>
public class BlockedAccountMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BlockedAccountMiddleware> _logger;

    public BlockedAccountMiddleware(RequestDelegate next, ILogger<BlockedAccountMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        // Проверяем только для аутентифицированных запросов
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await userRepository.GetByIdAsync(userId, context.RequestAborted);

                if (user?.IsBlocked == true)
                {
                    _logger.LogWarning(
                        "Blocked user {UserId} attempted to access {Path}. Reason: {Reason}",
                        userId, context.Request.Path, user.BlockedReason);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "AccountBlocked",
                        message = "Your account has been blocked.",
                        reason = user.BlockedReason,
                        blockedAt = user.BlockedAt
                    };

                    await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
                    return;
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods для регистрации BlockedAccountMiddleware
/// </summary>
public static class BlockedAccountMiddlewareExtensions
{
    public static IApplicationBuilder UseBlockedAccountCheck(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<BlockedAccountMiddleware>();
    }
}
