// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nChain.CreateDB;
using nChain.CreateDB.DB;

namespace SPVChannels.API.Rest.Database
{
  public class SPVChannelsDbManager : IDbManager
  {
    private const string DB_CHANNELS = "SPVChannels";
    private readonly CreateDB channelsDb;

    public SPVChannelsDbManager(ILogger<CreateDB> logger, IConfiguration configuration)
    {

      channelsDb = new CreateDB(logger, DB_CHANNELS, RDBMS.Postgres,
        configuration["AppConfiguration:DBConnectionStringDDL"],
        configuration["AppConfiguration:DBConnectionStringMaster"]
      );
    }

    public bool CreateDb(out string errorMessage, out string errorMessageShort)
    {
      return channelsDb.CreateDatabase(out errorMessage, out errorMessageShort);
    }

    public bool DatabaseExists()
    {
      return channelsDb.DatabaseExists();
    }
  }
}
