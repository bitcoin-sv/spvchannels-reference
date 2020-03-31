using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SPVChannels.API.Rest
{
  public class StartupChecker : IHostedService
  {
    readonly IChannelRepository channelRepository;
    readonly IHostApplicationLifetime hostApplicationLifetime;
    readonly ILogger<StartupChecker> logger;
    public StartupChecker(IChannelRepository channelRepository,
                          IHostApplicationLifetime hostApplicationLifetime,
                          ILogger<StartupChecker> logger)
    {
      this.channelRepository = channelRepository;
      this.hostApplicationLifetime = hostApplicationLifetime;
      this.logger = logger;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
      logger.LogInformation("Health checks starting.");
      try
      {
        var dbTask = HelperTools.ExecuteWithRetries(10, "Unable to open connection to database", () => TestDBConnection());
        Task.WaitAll(new Task[] { dbTask });
        logger.LogInformation("Health checks completed successfully.");
      }
      catch (Exception ex)
      {
        logger.LogError("Health checks failed. {0}", ex.GetBaseException().ToString());
        // If exception was thrown then we stop the application. All methods in try section must pass without exception
        hostApplicationLifetime.StopApplication();
      }

      return Task.CompletedTask;
    }    

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }
    private Task TestDBConnection()
    {
      channelRepository.GetChannelById(0);
      return Task.CompletedTask;
    }
  }
}
