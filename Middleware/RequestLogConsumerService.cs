using System.Data;
using Microsoft.Data.SqlClient;

namespace APP.PDF.Middleware
{
    using System.Data;
    using Microsoft.Data.SqlClient;

    public class RequestLogConsumerService : BackgroundService
    {
        private readonly IRequestLogQueue _queue;
        private readonly ILogger<RequestLogConsumerService> _logger;
        private readonly string _connectionString;
        private readonly string _applicationName;

        // Batching settings (read from configuration)
        private readonly int _batchSize;
        private readonly int _flushIntervalSeconds;

        // Batch and lock
        private readonly List<RequestLogEntry> _batch = new();
        private readonly object _lock = new();
        private readonly Timer _flushTimer;

        public RequestLogConsumerService(
            IRequestLogQueue queue,
            IConfiguration configuration,
            ILogger<RequestLogConsumerService> logger)
        {
            _queue = queue;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _applicationName = configuration.GetValue<string>("ApplicationName") ?? "UnknownApp";

            var section = configuration.GetSection("RequestLogging");
            _batchSize = section.GetValue<int>("BatchSize", 1);
            _flushIntervalSeconds = section.GetValue<int>("FlushIntervalSeconds", 5);

            _flushTimer = new Timer(
                _ => FlushBatchIfNeeded(),
                null,
                TimeSpan.FromSeconds(_flushIntervalSeconds),
                TimeSpan.FromSeconds(_flushIntervalSeconds));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var entry in _queue.ReadAllAsync(stoppingToken))
                {
                    lock (_lock)
                    {
                        _batch.Add(entry);
                        if (_batch.Count >= _batchSize)
                        {
                            // Trigger a flush asynchronously (avoid blocking the reader)
                            _ = Task.Run(() => FlushBatchIfNeeded());
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown
            }
            finally
            {
                _flushTimer?.Dispose();
                await FlushBatchAsync(force: true);
            }
        }

        private void FlushBatchIfNeeded()
        {
            List<RequestLogEntry> batchToFlush;
            lock (_lock)
            {
                if (_batch.Count == 0)
                    return;

                batchToFlush = new List<RequestLogEntry>(_batch);
                _batch.Clear();
            }

            // Fire-and-forget flush
            _ = Task.Run(() => FlushBatchAsync(batchToFlush));
        }

        private async Task FlushBatchAsync(List<RequestLogEntry> batch = null, bool force = false)
        {
            if (batch == null)
            {
                lock (_lock)
                {
                    if (_batch.Count == 0)
                        return;
                    batch = new List<RequestLogEntry>(_batch);
                    _batch.Clear();
                }
            }

            if (batch == null || !batch.Any())
                return;

            try
            {
                var dt = new DataTable();
                // Define columns in the same order as the database (except Id)
                dt.Columns.Add("ApplicationName", typeof(string));
                dt.Columns.Add("Timestamp", typeof(DateTime));
                dt.Columns.Add("ClientIP", typeof(string));
                dt.Columns.Add("HttpMethod", typeof(string));
                dt.Columns.Add("RequestPath", typeof(string));
                dt.Columns.Add("StatusCode", typeof(int));
                dt.Columns.Add("ElapsedMilliseconds", typeof(int));
                dt.Columns.Add("UserAgent", typeof(string));
                dt.Columns.Add("Referer", typeof(string));
                dt.Columns.Add("RequestBody", typeof(string));

                foreach (var entry in batch)
                {
                    dt.Rows.Add(
                        _applicationName,
                        entry.Timestamp,
                        entry.ClientIP,
                        entry.HttpMethod,
                        entry.RequestPath,
                        entry.StatusCode,
                        entry.ElapsedMilliseconds,
                        entry.UserAgent,
                        entry.Referer,
                        entry.RequestBody
                    );
                }

                using var bulkCopy = new SqlBulkCopy(_connectionString)
                {
                    DestinationTableName = "dbo.RequestLogs",
                    BatchSize = batch.Count,
                    EnableStreaming = true
                };

                // ---- CRITICAL: Explicit column mappings ----
                bulkCopy.ColumnMappings.Add("ApplicationName", "ApplicationName");
                bulkCopy.ColumnMappings.Add("Timestamp", "Timestamp");
                bulkCopy.ColumnMappings.Add("ClientIP", "ClientIP");
                bulkCopy.ColumnMappings.Add("HttpMethod", "HttpMethod");
                bulkCopy.ColumnMappings.Add("RequestPath", "RequestPath");
                bulkCopy.ColumnMappings.Add("StatusCode", "StatusCode");
                bulkCopy.ColumnMappings.Add("ElapsedMilliseconds", "ElapsedMilliseconds");
                bulkCopy.ColumnMappings.Add("UserAgent", "UserAgent");
                bulkCopy.ColumnMappings.Add("Referer", "Referer");
                bulkCopy.ColumnMappings.Add("RequestBody", "RequestBody");

                await bulkCopy.WriteToServerAsync(dt);
                _logger.LogInformation($"Flushed {batch.Count} request logs to database.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL BulkCopy failed for request logs. Logs may be lost.");
            }
        }

        public override void Dispose()
        {
            _flushTimer?.Dispose();
            base.Dispose();
        }
    }
}
