// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.Options;
using Npgsql;
using SPVChannels.Infrastructure.Utilities;
using System;

namespace SPVChannels.Infrastructure.Repositories
{
  public class BaseRepositoryPostgres
  {
    private readonly string connectionString;

    public BaseRepositoryPostgres(string connectionString)
    {
      this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public BaseRepositoryPostgres(IOptions<AppConfiguration> options): this(options.Value.DBConnectionString)
    {
      if (options == null)
      {
        throw new ArgumentNullException(nameof(options));
      }
      else
      {
        if (options.Value == null)
          throw new ArgumentNullException(nameof(AppConfiguration));
      }
    }

    public NpgsqlConnection GetNpgsqlConnection() => new NpgsqlConnection(connectionString);
  }
}
