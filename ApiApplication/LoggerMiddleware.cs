using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ApiApplication
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggerMiddleware> _logger;

        public LoggerMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            await _next(context);

            stopwatch.Stop();

            var executionTime = stopwatch.Elapsed.TotalSeconds;
            var formattedTime = executionTime < 1 ? $"{stopwatch.ElapsedMilliseconds} ms" : $"{executionTime:F2} seconds";

            _logger.LogInformation($"Request [{context.Request.Method}] {context.Request.Path} took {formattedTime}.");
        }
    }
}
