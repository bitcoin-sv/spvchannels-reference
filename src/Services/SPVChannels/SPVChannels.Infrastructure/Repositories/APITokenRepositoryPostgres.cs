// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Npgsql;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SPVChannels.Infrastructure.Repositories
{
  public class APITokenRepositoryPostgres : BaseRepositoryPostgres, IAPITokenRepository
  {
    readonly int _tokenSize;
    readonly IMemoryCache cache;
    public APITokenRepositoryPostgres(IOptions<AppConfiguration> op, IMemoryCache cache) : base(op)
    {
      this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
      _tokenSize = op.Value.TokenSize;
    }

    #region CreateAPIToken
    public APIToken CreateAPIToken(APIToken APIToken)
    {
      if (string.IsNullOrEmpty(APIToken.Token))
        APIToken.CreateToken(_tokenSize);

      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string insertOrUpdate =
"INSERT INTO APIToken (account, channel, token, description, canread, canwrite, validfrom) " +
"VALUES(@account, @channel, @token, @description, @canread, @canwrite, @validfrom) " +
"RETURNING *;";

      var createdAPIToken = connection.Query<APIToken>(insertOrUpdate,
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

      transaction.Commit();
      return createdAPIToken;
    }
    #endregion

    public APIToken GetAPITokenById(long apiTokenId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectAPITokens = "SELECT * FROM APIToken WHERE id = @apiTokenId and (validto IS NULL OR validto >= @validto);";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { apiTokenId, validto = DateTime.UtcNow }).FirstOrDefault();

      return apiTokens;
    }

    public APIToken GetAPITokenByFCMToken(string fcmToken)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectAPITokens = "SELECT APIToken.* FROM APIToken INNER JOIN FCMToken ON FCMToken.apitoken = APIToken.id WHERE FCMToken.token = @fcmToken and (validto IS NULL OR validto >= @validto);";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { fcmToken, validto = DateTime.UtcNow }).FirstOrDefault();

      return apiTokens;
    }

    public IEnumerable<APIToken> GetAPITokens(string channelExternalId, string token = null)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectAPITokens =
        "SELECT APIToken.* " +
        "FROM APIToken " +
        "INNER JOIN Channel ON APIToken.channel = Channel.id " +
        "WHERE Channel.externalid = @channelExternalId " +
        "  AND (APIToken.validto IS NULL OR APIToken.validto >= @validto) and (@token IS NULL or APIToken.token = @token);";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { channelExternalId, validto = DateTime.UtcNow, token });

      return apiTokens;
    }

    #region RevokeAPIToken
    public bool RevokeAPIToken(long tokenId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string selectAPITokens = "SELECT * FROM APIToken WHERE id = @apiTokenId;";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { apiTokenId = tokenId }).FirstOrDefault();

      string updateAPIToken = "UPDATE APIToken " +
        "SET validto = @validto " +
        "WHERE id=@tokenId;";

      var revokeAPITokenResult = connection.Execute(updateAPIToken, transaction: transaction, param: new { tokenId, validto = DateTime.UtcNow });

      transaction.Commit();
      if (apiTokens != null)
      {
        cache.Remove(apiTokens.Token);
        cache.Remove($"{ apiTokens.Account }_{ apiTokens.Channel }_{ apiTokens.Id }");
        cache.Remove($"{ apiTokens.Channel }_{ apiTokens.Id }");
      }

      return revokeAPITokenResult > 0;
    }
    #endregion

    #region InsertFCMToken
    public FCMToken InsertFCMToken(APIToken apiToken, string token)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string insert =
"INSERT INTO FCMToken (apitoken, token, isvalid) " +
"VALUES(@apitoken, @token, @isvalid) " +
"ON CONFLICT (apitoken, token) DO NOTHING " +
"RETURNING *;"
;

      var fcmTokenRes = connection.Query<FCMToken>(insert,
        new
        {
          apitoken = apiToken.Id,
          token = token,
          isvalid = true
        }
      ).SingleOrDefault();

      transaction.Commit();

      return fcmTokenRes;
    }
    #endregion

    #region UpdateFCMToken
    public bool UpdateFCMToken(string oldToken, string newToken)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string update =
  "UPDATE FCMToken SET token = @newToken, isvalid = true " +
  "WHERE token = @oldToken;";

      var updateFCMTokenResult = connection.Execute(update, transaction: transaction, param: new { oldToken = oldToken, newToken = newToken });


      transaction.Commit();
      return updateFCMTokenResult > 0;
    }

    #endregion

    #region DeleteFCMToken
    public bool DeleteFCMToken(string oldToken, string channelId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      int deleteFCMTokenResult = 0;
      if (string.IsNullOrEmpty(channelId))
      {
        string update =
    "DELETE FROM FCMToken " +
    "WHERE token = @oldToken ";

        deleteFCMTokenResult = connection.Execute(update, transaction: transaction, param: new { oldToken = oldToken });
      }
      else
      {
        string update =
    "DELETE FROM FCMToken " +
    "WHERE token = @oldToken " +
    "AND apitoken IN (SELECT APIToken.id FROM APIToken INNER JOIN Channel ON APIToken.channel = Channel.Id WHERE Channel.externalid = @channelId)";
        deleteFCMTokenResult = connection.Execute(update, transaction: transaction, param: new { oldToken = oldToken, channelId = channelId });
      }

      transaction.Commit();
      return deleteFCMTokenResult > 0;
    }
    #endregion
  }
}
