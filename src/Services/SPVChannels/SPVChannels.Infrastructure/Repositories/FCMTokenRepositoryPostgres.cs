// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Utilities;
using System;
using System.Linq;

namespace SPVChannels.Infrastructure.Repositories
{
  public class FCMTokenRepositoryPostgres : BaseRepositoryPostgres, IFCMTokenRepository
  {    
    public FCMTokenRepositoryPostgres(IOptions<AppConfiguration> op) : base(op)
    {
    }

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
        string delete =
"DELETE FROM FCMToken " +
"WHERE token = @oldToken ";

        deleteFCMTokenResult = connection.Execute(delete, transaction: transaction, param: new { oldToken = oldToken });
      }
      else
      {
        string delete =
"DELETE FROM FCMToken " +
"WHERE token = @oldToken " +
"AND apitoken IN (SELECT APIToken.id FROM APIToken INNER JOIN Channel ON APIToken.channel = Channel.Id WHERE Channel.externalid = @channelId)";
        deleteFCMTokenResult = connection.Execute(delete, transaction: transaction, param: new { oldToken = oldToken, channelId = channelId });
      }

      transaction.Commit();
      return deleteFCMTokenResult > 0;
    }
    #endregion

    #region GetAPITokenByFCMToken
    public APIToken GetAPITokenByFCMToken(string fcmToken)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectAPITokens = 
"SELECT APIToken.* " +
"FROM APIToken " +
"INNER JOIN FCMToken ON FCMToken.apitoken = APIToken.id " +
"WHERE FCMToken.token = @fcmToken AND (validto IS NULL OR validto >= @validto);";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { fcmToken, validto = DateTime.UtcNow }).FirstOrDefault();

      return apiTokens;
    }
    #endregion

    #region MarkFCMTokenAsInvalid
    public bool MarkFCMTokenAsInvalid(string fcmToken)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string markAsInvalid =
"UPDATE FCMToken SET IsValid = false " +
"WHERE token = @fcmToken ";
      var markAsInvalidResult = connection.Execute(markAsInvalid, transaction: transaction, param: new { fcmToken = fcmToken });

      transaction.Commit();
      return markAsInvalidResult > 0;
    }
    #endregion
  }
}
