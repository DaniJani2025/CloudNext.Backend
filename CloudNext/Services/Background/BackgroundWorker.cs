using CloudNext.Interfaces;
using Microsoft.Extensions.Hosting;

namespace CloudNext.Services.Background
{
    public class BackgroundWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceProvider _services;

        public BackgroundWorker(IBackgroundTaskQueue queue, IServiceProvider services)
        {
            _queue = queue;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var job = await _queue.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _services.CreateScope();
                    await job(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}