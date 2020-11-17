using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Notification{
  public interface INotificationWebSocketHandler
  {
    Task Subscribe(long channelId, long tokenId, WebSocket webSocket);

    Task SendNotification(long sourceTokenId, long channelId, DateTime timestamp, string message);

    Task CleanUpConnections();
  }
}
