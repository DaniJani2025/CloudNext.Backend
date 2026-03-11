using CloudNext.Interfaces;
using System.Threading.Channels;

namespace CloudNext.Services.Background
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

        public BackgroundTaskQueue()
        {
            _queue = Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();
        }

        public async ValueTask QueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken token)
        {
            return await _queue.Reader.ReadAsync(token);
        }
    }
}