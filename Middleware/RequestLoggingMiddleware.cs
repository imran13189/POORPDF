using System.Diagnostics;
using System.Text;


namespace APP.PDF.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestLogQueue _queue;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, IRequestLogQueue queue, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _queue = queue;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Capture request details (safe to access here)
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var method = context.Request.Method;
            var path = context.Request.Path.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var referer = context.Request.Headers["Referer"].ToString();

            // Optional: Read request body (only for POST/PUT)
            string requestBody = null;
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                context.Request.EnableBuffering(); // Allows reading the stream multiple times
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0; // Reset for actual pipeline
            }

            try
            {
                await _next(context); // Execute the actual endpoint
            }
            finally
            {
                stopwatch.Stop();

                // Build the log entry
                var entry = new RequestLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    ClientIP = ip,
                    HttpMethod = method,
                    RequestPath = path,
                    StatusCode = context.Response.StatusCode,
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                    UserAgent = userAgent,
                    Referer = referer,
                    RequestBody = requestBody // Be careful with sensitive data (passwords!)
                };

                // Queue it in the background (non-blocking)
                if (!_queue.TryWrite(entry))
                {
                    _logger.LogWarning("Request log queue is full. Dropping log entry.");
                }
            }
        }
    }
}
