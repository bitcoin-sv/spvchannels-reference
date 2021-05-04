// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Notification{
  public interface IWebSocketHandler
  {
    Task Subscribe(long channelId, long tokenId, WebSocket webSocket);

    Task CleanUpConnections();
  }
}
