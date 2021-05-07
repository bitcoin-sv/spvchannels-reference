// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using Dapper;
using Npgsql;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using SPVChannels.Infrastructure.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace SPVChannels.Infrastructure.Repositories
{
  public class ChannelRepositoryPostgres : BaseRepositoryPostgres, IChannelRepository
  {
    readonly int _tokenSize;
    readonly IMemoryCache cache;
    public ChannelRepositoryPostgres(IOptions<AppConfiguration> op, IMemoryCache cache) : base(op)
    {
      _tokenSize = op.Value.TokenSize;
      this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    #region CreateAPIToken
    APIToken CreateAPIToken(APIToken APIToken, NpgsqlConnection connection)
    {
      if (string.IsNullOrEmpty(APIToken.Token))
        APIToken.CreateToken(_tokenSize);

      string insertOrUpdate =
"INSERT INTO APIToken (account, channel, token, description, canread, canwrite, validfrom) " +
"VALUES(@account, @channel, @token, @description, @canread, @canwrite, @validfrom) " +
"RETURNING *;";

      return connection.Query<APIToken>(insertOrUpdate,
        new
        {
          account = APIToken.Account,
          channel = APIToken.Channel,
          token = APIToken.Token,
          description = APIToken.Description,
          canread = APIToken.CanRead,
          canwrite = APIToken.CanWrite,
          validfrom = DateTime.UtcNow
        }
      ).Single();
    }
    #endregion

    public Channel CreateChannel(Channel channel)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string insertOrUpdate =
"INSERT INTO Channel (owner, externalid, publicread, publicwrite, locked, sequenced, minagedays, maxagedays, autoprune) " +
"VALUES(@owner, @externalid, @publicread, @publicwrite, @locked, @sequenced, @minagedays, @maxagedays, @autoprune) " +
"RETURNING *;"
;

      var channelRes = connection.Query<Channel>(insertOrUpdate,
        new
        {
          owner = channel.Owner,
          externalid = Channel.CreateExternalId(),
          publicread = channel.PublicRead,
          publicwrite = channel.PublicWrite,
          locked = channel.Locked,
          sequenced = channel.Sequenced,
          minagedays = channel.MinAgeDays,
          maxagedays = channel.MaxAgeDays,
          autoprune = channel.AutoPrune
        }
      ).Single();

      var newAPIToken = CreateAPIToken(
        new APIToken
        {
          Account = channelRes.Owner,
          Channel = channelRes.Id,
          Description = "Owner",
          CanRead = true,
          CanWrite = true
        }, connection);

      transaction.Commit();
      
      channelRes.APIToken = new [] { newAPIToken };

      return channelRes;
    }

    public IEnumerable<Channel> GetChannels(long accountId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectChannels =
        "SELECT id, owner, externalid, publicread, publicwrite, locked, sequenced, minagedays, maxagedays, autoprune, (select max(seq) FROM Message where Channel.id = Message.channel) AS seq " +
        "FROM Channel " +
        "WHERE owner = @account;";
      
      var channels = connection.Query(selectChannels, param: new { account = accountId });

      if (!channels.Any())
      {
        return Enumerable.Empty<Channel>();
      }

      var result = (from channel in channels
           select new Channel {
             Id = channel.id,
             Owner = channel.owner,
             ExternalId = channel.externalid,
             PublicRead = channel.publicread,
             PublicWrite = channel.publicwrite,
             Locked = channel.locked,
             Sequenced = channel.sequenced,
             AutoPrune = channel.autoprune,
             MinAgeDays = channel.minagedays,
             MaxAgeDays = channel.maxagedays,
             HeadMessageSequence = channel.seq?? 0
           }).ToArray();

      string selectAPITokens = "SELECT * FROM APIToken WHERE account = @account and (validto IS NULL OR validto >= @validto);";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { account = accountId, validto = DateTime.UtcNow });

      if (apiTokens.Any())
      {
        foreach(var channel in result)
        {
          channel.APIToken = apiTokens.Where(apiToken => channel.Id == apiToken.Channel).ToArray();
        }
      }

      return result;
    }

    public Channel GetChannelById(long channelId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();
      Channel channel = null;
      string selectChannelById =
"SELECT id, owner, externalid, publicread, publicwrite, locked, sequenced, minagedays, maxagedays, autoprune, (select max(seq) FROM Message where Channel.id = Message.channel) AS seq " +
"FROM Channel " +
"WHERE Channel.id = @id;";

      var data = connection.Query(selectChannelById, new { id = channelId }).FirstOrDefault();

      if (data != null)
      {
        channel = new Channel
        {
          Id = data.id,
          Owner = data.owner,
          ExternalId = data.externalid,
          PublicRead = data.publicread,
          PublicWrite = data.publicwrite,
          Locked = data.locked,
          Sequenced = data.sequenced,
          AutoPrune = data.autoprune,
          MinAgeDays = data.minagedays,
          MaxAgeDays = data.maxagedays,
          HeadMessageSequence = data.seq ?? 0
        };

        channel.APIToken = GetChannelAPITokens(connection, channel.Id);

      }
      return channel;
    }

    public Channel GetChannelByExternalId(string externalId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();
      Channel channel = null;
      string selectChannelById =
"SELECT id, owner, externalid, publicread, publicwrite, locked, sequenced, minagedays, maxagedays, autoprune, (select max(seq) FROM Message where Channel.id = Message.channel) AS seq " +
"FROM Channel " +
"WHERE Channel.externalid = @externalid;";

      var data = connection.Query(selectChannelById, new { externalid = externalId }).FirstOrDefault();

      if (data != null)
      {
        channel = new Channel
        {
          Id = data.id,
          Owner = data.owner,
          ExternalId = data.externalid,
          PublicRead = data.publicread,
          PublicWrite = data.publicwrite,
          Locked = data.locked,
          Sequenced = data.sequenced,
          AutoPrune = data.autoprune,
          MinAgeDays = data.minagedays,
          MaxAgeDays = data.maxagedays,
          HeadMessageSequence = data.seq ?? 0
        };

        channel.APIToken = GetChannelAPITokens(connection, channel.Id);
      }
      return channel;
    }

    private IEnumerable<APIToken> GetChannelAPITokens(NpgsqlConnection connection, long channelId)
    {
      string selectAPITokens =
"SELECT APIToken.*, FCMToken.* " +
"FROM APIToken " +
"LEFT JOIN FCMToken ON FCMToken.apitoken = APIToken.id " +
"WHERE APIToken.channel = @channel and (APIToken.validto IS NULL OR APIToken.validto >= @validto);";

      var apiTokensDict = new Dictionary<long, APIToken>();
      connection.Query<APIToken, FCMToken, APIToken>(
        selectAPITokens,
        (apiToken, fcmToken) => {
          APIToken obj;
            // check if this api token is already in our dictionary and we will only add FCM token
            if (!apiTokensDict.TryGetValue(apiToken.Id, out obj))
          {
            apiTokensDict.Add(apiToken.Id, obj = apiToken);
            obj.FCMTokens = new List<FCMToken>();
          }
          if (fcmToken != null)
            obj.FCMTokens.Add(fcmToken);
          return obj;
        },
        param: new { channel = channelId, validto = DateTime.UtcNow }
      ).AsQueryable();

      return apiTokensDict.Values.ToArray();
    }

    public void DeleteChannel(string externalId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string selectChannelByExternalId = "SELECT id FROM Channel WHERE externalid = @externalid;";
      var channelId = connection.ExecuteScalar<long?>(selectChannelByExternalId, new { externalid = externalId });

      string selectAPITokens = "SELECT * FROM APIToken WHERE channel = @channel;";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { channel = channelId }).ToArray();

      string delete = 
"DELETE FROM MessageStatus WHERE message IN (SELECT id FROM Message WHERE Message.channel = @id); " +
"DELETE FROM Message WHERE channel = @id; " +
"DELETE FROM FCMToken WHERE apitoken IN (SELECT id FROM APIToken WHERE APIToken.channel = @id); " +
"DELETE FROM APIToken WHERE channel = @id; " +
"DELETE FROM Channel WHERE id = @id;";

      connection.Execute(delete, transaction: transaction, param: new { id = channelId });

      transaction.Commit();

      foreach(var apiToken in apiTokens)
      {
        cache.Remove(apiToken.Token);
        cache.Remove($"{ apiToken.Account }_{ apiToken.Channel }"); 
        cache.Remove($"{ apiToken.Account }_{ apiToken.Channel }_{ apiToken.Id }");
        cache.Remove($"{ apiToken.Channel }_{ apiToken.Id }");
      }
    }

    public Channel AmendChannel(Channel data)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string update =
"UPDATE Channel " +
"SET  publicread=@publicread, publicwrite=@publicwrite, locked=@locked " +
"WHERE externalid=@externalid " +
"RETURNING *";

      var updateChannelResult = connection.Query<Channel>(update,
        new
        {
          externalid = data.ExternalId,
          publicread = data.PublicRead,
          publicwrite = data.PublicWrite,
          locked = data.Locked
        }
      ).Single();
      transaction.Commit();

      return updateChannelResult;
    }

    public static void EmptyRepository(string connectionString)
    {
      using var connection = new NpgsqlConnection(connectionString);
      connection.Open();
      string cmdText =
        "DELETE FROM MessageStatus; DELETE FROM Message; DELETE FROM APIToken; DELETE FROM Channel; ALTER SEQUENCE Channel_id_seq RESTART WITH 1; ALTER SEQUENCE APIToken_id_seq RESTART WITH 1; ALTER SEQUENCE Message_id_seq RESTART WITH 1; ALTER SEQUENCE MessageStatus_id_seq RESTART WITH 1;";
      connection.Execute(cmdText, null);
    }
  }
}
