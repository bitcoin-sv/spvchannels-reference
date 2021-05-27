// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nChain.CreateDB;
using nChain.CreateDB.DB;
using SPVChannels.API.Rest.Database;
using System.IO;

namespace SPVChannels.Test.Functional.Database
{
  public class SPVChannelsTestDbManager : IDbManager
  {
    private const string DB_CHANNELS = "SPVChannels";
    private readonly CreateDB channelsTestDb;
    private readonly CreateDB channelsDb;

    public SPVChannelsTestDbManager(ILogger<CreateDB> logger, IConfiguration configuration)
    {
      string scriptLocation = "..\\..\\..\\Database\\Scripts";
      // Fix path for non windows os
      if (Path.DirectorySeparatorChar != '\\')
        scriptLocation = scriptLocation.Replace('\\', Path.DirectorySeparatorChar);

      channelsDb = new CreateDB(logger, DB_CHANNELS, RDBMS.Postgres,
        configuration["AppConfiguration:DBConnectionStringDDL"],
        configuration["AppConfiguration:DBConnectionStringMaster"]);

      channelsTestDb = new CreateDB(logger, DB_CHANNELS, RDBMS.Postgres,
        configuration["AppConfiguration:DBConnectionStringDDL"],
        configuration["AppConfiguration:DBConnectionStringMaster"],
        scriptLocation);
    }

    public bool CreateDb(out string errorMessage, out string errorMessageShort)
    {
      return channelsTestDb.CreateDatabase(out errorMessage, out errorMessageShort) &&
             channelsDb.CreateDatabase(out errorMessage, out errorMessageShort);
    }

    public bool DatabaseExists()
    {
      return channelsDb.DatabaseExists();
    }
  }
}
