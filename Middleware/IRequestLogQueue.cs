using System.Threading.Channels;

namespace APP.PDF.Middleware
{
    public interface IRequestLogQueue
    {
        bool TryWrite(RequestLogEntry entry);
        IAsyncEnumerable<RequestLogEntry> ReadAllAsync(CancellationToken cancellationToken = default);
    }

    public class RequestLogQueue : IRequestLogQueue
    {
        private readonly Channel<RequestLogEntry> _channel;

        public RequestLogQueue()
        {
            // Drop new logs when queue is full (fast, avoids blocking)
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            };
            _channel = Channel.CreateBounded<RequestLogEntry>(options);
        }

        public bool TryWrite(RequestLogEntry entry)
            => _channel.Writer.TryWrite(entry);

        public IAsyncEnumerable<RequestLogEntry> ReadAllAsync(CancellationToken cancellationToken = default)
            => _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
