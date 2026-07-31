namespace APP.PDF.Middleware
{
    public class RequestLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string ApplicationName { get; set; }
        public string ClientIP { get; set; }
        public string HttpMethod { get; set; }
        public string RequestPath { get; set; }
        public int StatusCode { get; set; }
        public int ElapsedMilliseconds { get; set; }
        public string UserAgent { get; set; }
        public string Referer { get; set; }
        public string RequestBody { get; set; } // Optional
    }
}
