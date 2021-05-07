// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using SPVChannels.Domain.Models;
using System.Collections.Generic;

namespace SPVChannels.Domain.Repositories
{
  public interface IChannelRepository
  {
    IEnumerable<Channel> GetChannels(long accountId);

    Channel GetChannelById(long channelId);

    Channel GetChannelByExternalId(string externalId);

    Channel CreateChannel(Channel channel);

    void DeleteChannel(string externalId);

    Channel AmendChannel(Channel channel);

  }
}
