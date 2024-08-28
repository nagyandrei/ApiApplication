using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ApiApplication
{
    public class Logger
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<Logger> _logger;

        public Logger(RequestDelegate next, ILogger<Logger> logger)
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

            var executionTime = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation($"Request [{context.Request.Method}] {context.Request.Path} took {executionTime} ms.");
        }
    }
}
