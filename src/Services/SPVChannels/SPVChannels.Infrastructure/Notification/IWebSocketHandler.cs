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
