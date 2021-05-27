// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPVChannels.API.Rest.Database;
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
    readonly IDbManager dbManager;

    public StartupChecker(IChannelRepository channelRepository,
                          IHostApplicationLifetime hostApplicationLifetime,
                          ILogger<StartupChecker> logger,
                          IDbManager dbManager)
    {
      this.channelRepository = channelRepository ?? throw new ArgumentException(nameof(channelRepository));
      this.hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentException(nameof(hostApplicationLifetime));
      this.logger = logger ?? throw new ArgumentException(nameof(logger));
      this.dbManager = dbManager ?? throw new ArgumentException(nameof(dbManager));
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
      logger.LogInformation("Health checks starting.");
      try
      {
        var dbTask = HelperTools.ExecuteWithRetries(10, "Unable to open connection to database", () => TestDBConnection());
        Task.WaitAll(new Task[] { dbTask });

        ExecuteCreateDb();

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

    private void ExecuteCreateDb()
    {
      logger.LogInformation($"Starting with execution of CreateDb ...");


      if (dbManager.CreateDb(out string errorMessage, out string errorMessageShort))
      {
        logger.LogInformation("CreateDb finished successfully.");
      }
      else
      {
        // if error we must stop application
        throw new Exception($"Error when executing CreateDb: { errorMessage }{ Environment.NewLine }ErrorMessage: {errorMessageShort}");
      }

      logger.LogInformation($"ExecuteCreateDb completed.");
    }

    private Task TestDBConnection()
    {
      if (dbManager.DatabaseExists())
      {
        logger.LogInformation($"Successfully connected to DB.");
      }
      return Task.CompletedTask;
    }
  }
}
