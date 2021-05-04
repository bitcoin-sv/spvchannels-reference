// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using SPVChannels.Domain.Models;
using System.Threading.Tasks;

namespace SPVChannels.Domain.Repositories
{
  public interface IAuthRepository
  {

    Task<long> AuthenticateCacheAsync(string scheme, string credential);

    Task<bool> IsAuthorizedToChannelCacheAsync(long accountId, string channelExternalId);

    Task<bool> IsAuthorizedToAPITokenCacheAsync(long accountId, string channelExternalId, long apiToken);

    Task<bool> IsAuthorizedToAPITokenCacheAsync(string channelExternalId, long apiToken);

    Task<APIToken> GetAPITokenAsync(string apiToken);

  }
}
