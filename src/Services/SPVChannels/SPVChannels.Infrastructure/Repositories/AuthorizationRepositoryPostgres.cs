using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Repositories
{
  public class AuthorizationRepositoryPostgres : BaseRepositoryPostgres, IAuthRepository
  {

    readonly long _cacheSize;
    readonly TimeSpan _slidingExpirationTime;
    readonly TimeSpan _absoluteExpirationTime;
    readonly IMemoryCache cache;
    public AuthorizationRepositoryPostgres(IOptions<AppConfiguration> op, IMemoryCache cache) : base(op)
    {
      this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
      _cacheSize = op.Value.CacheSize;
      _slidingExpirationTime = new TimeSpan(10000 * op.Value.CacheSlidingExpirationTime);
      _absoluteExpirationTime = new TimeSpan(10000 * op.Value.CacheAbsoluteExpirationTime);
    }

    #region Authenticate
    public async Task<long> AuthenticateCacheAsync(string scheme, string credential)
    {
      return await cache.GetOrCreateAsync($"{scheme}_{credential}", async (cacheEntry) => 
      {
        SetupCacheEntry(cacheEntry);
        return await AuthenticateDb(scheme, credential);
      });
    }    

    Task<long> AuthenticateDb(string scheme, string credential)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string select =
"SELECT AccountCredential.account " +
"FROM AccountCredential " +
"WHERE AccountCredential.scheme=@scheme AND AccountCredential.credential = @credential;";

      var accountId = connection.ExecuteScalar<long?>(select, new { scheme, credential });
      
      return Task.FromResult(accountId ?? -1);
    }
    #endregion

    #region IsAuthorizedToChannel
    public async Task<bool> IsAuthorizedToChannelCacheAsync(long accountId, long channelId)
    {
      return await cache.GetOrCreateAsync($"{accountId}_{channelId}", async (cacheEntry) =>
      {
        SetupCacheEntry(cacheEntry);
        return await IsAuthorizedToChannelDb(accountId, channelId);
      });
    }

    Task<bool> IsAuthorizedToChannelDb(long accountId, long channelId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string select =
"SELECT COUNT('x') " +
"FROM Channel " +
"WHERE Channel.owner = @accountId AND Channel.id = @channelId;";

      var res = connection.ExecuteScalar<long?>(select, new { accountId, channelId });
      
      return Task.FromResult(res.HasValue && res.Value == 1);
    }
    #endregion

    #region GetAPIToken
    public async Task<APIToken> GetAPITokenAsync(string apiToken)
    {
      return await cache.GetOrCreateAsync($"{apiToken}", async (cacheEntry) =>
      {
        SetupCacheEntry(cacheEntry);
        return await GetAPITokenDb(apiToken);
      });
    }

    Task<APIToken> GetAPITokenDb(string apiToken)
    {
      
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectAPITokenByToken =
"SELECT * " +
"FROM APIToken " +
"WHERE APIToken.token = @token;";

      var data = connection.Query<APIToken>(
        selectAPITokenByToken,
        new 
        { 
          token = apiToken 
        }
      ).FirstOrDefault();

      return Task.FromResult(data);
    }
    #endregion

    #region IsAuthorizedToAPIToken
    public async Task<bool> IsAuthorizedToAPITokenCacheAsync(long accountId, long channelId, long apiTokenId)
    {
      return await cache.GetOrCreateAsync($"{accountId}_{channelId}_{apiTokenId}", async (cacheEntry) =>
      {
        SetupCacheEntry(cacheEntry);
        return await IsAuthorizedToAPITokenDb(accountId, channelId, apiTokenId);
      });
    }

    Task<bool> IsAuthorizedToAPITokenDb(long accountId, long channelId, long apiTokenId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string select =
"SELECT COUNT('x') " +
"FROM APIToken " +
"WHERE APIToken.account = @accountId AND APIToken.channel = @channelId and APIToken.id = @apiTokenId;";

      var res = connection.ExecuteScalar<long?>(select, new { accountId, channelId, apiTokenId });

      return Task.FromResult(res.HasValue && res.Value == 1);
    }
    #endregion

    void SetupCacheEntry(ICacheEntry cacheEntry)
    {
      cacheEntry.SetSize(_cacheSize);
      cacheEntry.SetSlidingExpiration(_slidingExpirationTime);
      cacheEntry.SetAbsoluteExpiration(_absoluteExpirationTime);
    }
  }
}
