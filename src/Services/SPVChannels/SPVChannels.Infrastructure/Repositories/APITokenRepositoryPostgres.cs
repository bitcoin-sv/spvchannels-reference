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

    public IEnumerable<APIToken> GetAPITokens(long channelid, string token = null)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectAPITokens = "SELECT * FROM APIToken WHERE channel = @channelid and (validto IS NULL OR validto >= @validto) and (@token IS NULL or token = @token);";

      var apiTokens = connection.Query<APIToken>(selectAPITokens, param: new { channelid, validto = DateTime.UtcNow, token });
      
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
      }

      return revokeAPITokenResult > 0;
    }
    #endregion
  }
}
