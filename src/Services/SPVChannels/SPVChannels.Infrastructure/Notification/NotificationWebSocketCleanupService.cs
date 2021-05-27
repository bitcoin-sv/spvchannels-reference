// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Notification
{
  public class NotificationWebSocketCleanupService : BackgroundService 
  {
    readonly ILogger<WebSocketHandler> logger;
    readonly IWebSocketHandler webSocketHandler;

    public NotificationWebSocketCleanupService(ILogger<WebSocketHandler> logger, IWebSocketHandler webSocketHandler)
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
