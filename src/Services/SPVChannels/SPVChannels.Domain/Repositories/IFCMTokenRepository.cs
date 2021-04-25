using SPVChannels.Domain.Models;
using System.Collections.Generic;

namespace SPVChannels.Domain.Repositories
{
  public interface IFCMTokenRepository
  {

    FCMToken InsertFCMToken(APIToken apiToken, string token);

    bool UpdateFCMToken(string oldToken, string newToken);

    bool DeleteFCMToken(string oldToken, string channelId);

    APIToken GetAPITokenByFCMToken(string fcmToken);

    bool MarkFCMTokenAsInvalid(string fcmToken);

  }
}
