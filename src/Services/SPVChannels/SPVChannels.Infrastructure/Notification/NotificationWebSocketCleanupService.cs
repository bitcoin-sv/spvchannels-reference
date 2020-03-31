using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Notification
{
  public class NotificationWebSocketCleanupService : BackgroundService 
  {
    readonly ILogger<NotificationWebSocketHandler> logger;
    readonly INotificationWebSocketHandler webSocketHandler;

    public NotificationWebSocketCleanupService(ILogger<NotificationWebSocketHandler> logger, INotificationWebSocketHandler webSocketHandler)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.webSocketHandler = webSocketHandler ?? throw new ArgumentNullException(nameof(webSocketHandler));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      logger.LogInformation($"NotificationWebSocketCleanupService is starting.");

      while (!stoppingToken.IsCancellationRequested)
      {
        await webSocketHandler.CleanUpConnections();
        await Task.Delay(5000, stoppingToken);
      }

      logger.LogInformation($"NotificationWebSocketCleanupService is stopping.");
    }

  }
}
