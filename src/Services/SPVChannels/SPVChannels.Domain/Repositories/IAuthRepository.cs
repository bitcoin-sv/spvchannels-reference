using SPVChannels.Domain.Models;
using System.Threading.Tasks;

namespace SPVChannels.Domain.Repositories
{
  public interface IAuthRepository
  {

    Task<long> AuthenticateCacheAsync(string scheme, string credential);

    Task<bool> IsAuthorizedToChannelCacheAsync(long accountId, long channelId);

    Task<bool> IsAuthorizedToAPITokenCacheAsync(long accountId, long channelId, long apiToken);

    Task<APIToken> GetAPITokenAsync(string apiToken);

  }
}
