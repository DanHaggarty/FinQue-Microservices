namespace RoutingService
{
    /// <summary>
    /// Processes messages from routing queue.
    /// </summary>
    public class RoutingWorker : BackgroundService
    {
        private readonly ILogger<RoutingWorker> _logger;

        public RoutingWorker(ILogger<RoutingWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}