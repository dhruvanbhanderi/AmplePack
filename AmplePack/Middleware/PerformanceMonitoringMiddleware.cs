using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AmplePack.Middleware
{
    /// <summary>
    /// Performance monitoring middleware to detect slow requests
    /// Logs warnings for requests exceeding threshold
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        private const int SLOW_REQUEST_THRESHOLD_MS = 3000; // 3 seconds
        private const int CRITICAL_THRESHOLD_MS = 10000; // 10 seconds

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;
            var method = context.Request.Method;
            
            // Skip static files and health checks
            if (path.StartsWithSegments("/css") || 
                path.StartsWithSegments("/js") ||
                path.StartsWithSegments("/lib") ||
                path.StartsWithSegments("/images") ||
                path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            var sw = Stopwatch.StartNew();
            
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var elapsed = sw.ElapsedMilliseconds;

                if (elapsed > CRITICAL_THRESHOLD_MS)
                {
                    _logger.LogError(
                        "?? CRITICAL SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms (>{ThresholdMs}ms) - Status: {StatusCode}",
                        method, path, elapsed, CRITICAL_THRESHOLD_MS, context.Response.StatusCode);
                }
                else if (elapsed > SLOW_REQUEST_THRESHOLD_MS)
                {
                    _logger.LogWarning(
                        "?? Slow request: {Method} {Path} took {ElapsedMs}ms (>{ThresholdMs}ms) - Status: {StatusCode}",
                        method, path, elapsed, SLOW_REQUEST_THRESHOLD_MS, context.Response.StatusCode);
                }
                else
                {
                    _logger.LogInformation(
                        "? {Method} {Path} completed in {ElapsedMs}ms - Status: {StatusCode}",
                        method, path, elapsed, context.Response.StatusCode);
                }
            }
        }
    }

    /// <summary>
    /// Extension method for easy middleware registration
    /// </summary>
    public static class PerformanceMonitoringMiddlewareExtensions
    {
        public static IApplicationBuilder UsePerformanceMonitoring(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
        }
    }
}
