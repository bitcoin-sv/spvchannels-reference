using Microsoft.Extensions.Options;
using Npgsql;
using SPVChannels.Infrastructure.Utilities;
using System;

namespace SPVChannels.Infrastructure.Repositories
{
  public class BaseRepositoryPostgres
  {
    private readonly string connectionString;
    public BaseRepositoryPostgres(IOptions<AppConfiguration> options)
    {
      if (options == null)
      {
        throw new ArgumentNullException(nameof(options));
      }
      else
      {
        if (options.Value == null)
          throw new ArgumentNullException(nameof(AppConfiguration));

        this.connectionString = options.Value.DBConnectionString;
      }
    }

    public NpgsqlConnection GetNpgsqlConnection() => new NpgsqlConnection(connectionString);
  }
}
