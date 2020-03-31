using SPVChannels.Domain.Models;
using System.Collections.Generic;

namespace SPVChannels.Domain.Repositories
{
  public interface IChannelRepository
  {
    IEnumerable<Channel> GetChannels(long accountId);

    Channel GetChannelById(long channelId);

    Channel CreateChannel(Channel channel);

    void DeleteChannel(long channelId);

    Channel AmendChannel(Channel channel);

  }
}
