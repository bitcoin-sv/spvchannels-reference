using SPVChannels.Domain.Models;
using System.Collections.Generic;

namespace SPVChannels.Domain.Repositories
{
  public interface IAPITokenRepository
  {
    IEnumerable<APIToken> GetAPITokens(string channelExternalId, string token = null);

    APIToken GetAPITokenById(long apiTokenId);

    APIToken CreateAPIToken(APIToken token);

    bool RevokeAPIToken(long apiTokenId);
  }
}
