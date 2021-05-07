// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Logging;
using SPVChannels.Domain.Models;

namespace SPVChannels.Infrastructure.Notification
{
  public class WebSocketHandler : INotificationHandler, IWebSocketHandler
  {
    readonly ILogger<WebSocketHandler> logger;
    private Dictionary<long, List<NotificationSubscription>> subscriptions = new Dictionary<long, List<NotificationSubscription>>();

    public WebSocketHandler(ILogger<WebSocketHandler> logger)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task Subscribe(long channelId, long tokenId, WebSocket webSocket)
    {
      lock (subscriptions)
      {
        if (!subscriptions.ContainsKey(channelId))
        {
          subscriptions.Add(channelId, new List<NotificationSubscription>());
        }

        subscriptions[channelId].Add(new NotificationSubscription
        {
          ChannelId = channelId,
          TokenId = tokenId,
          WebSocket = webSocket
        });
      }
      
      while (webSocket.State == WebSocketState.Open)
      {
        _ = await ReceiveMessage(channelId, tokenId, webSocket);
      }      
    }

    private async Task<string> ReceiveMessage(long channelId, long tokenId, WebSocket webSocket)
    {
      var arraySegment = new ArraySegment<byte>(new byte[4096]);
      _ = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);

      logger.LogDebug($"Received message for channel: {channelId} from token: {tokenId}.");
      return null;
    }

    public async Task SendNotification(long sourceTokenId, PushNotification notification)
    {
      IEnumerable<NotificationSubscription> toSentTo;
      string message = System.Text.Json.JsonSerializer.Serialize(notification.Message);
      lock (subscriptions)
      {
        if (!subscriptions.ContainsKey(notification.Channel.Id))
        {
          return;
        }

        toSentTo = subscriptions[notification.Channel.Id].ToList();
      }

      var tasks = toSentTo.Select(async subscription =>
      {
        if (subscription.WebSocket.State == WebSocketState.Open && subscription.TokenId != sourceTokenId)
        {
          var bytes = Encoding.Default.GetBytes(message);
          var arraySegment = new ArraySegment<byte>(bytes);
          await subscription.WebSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
      });

      try
      {
        await Task.WhenAll(tasks);
      }
      catch (AggregateException ex)
      {
        logger.LogError($"Error pushing notifications to clients: {ex} ({ex.StackTrace}).");
      }
    }

    /// <summary>
    /// Method checks which connections are still alive
    /// </summary>
    public async Task CleanUpConnections()
    {
        Dictionary<long, List<NotificationSubscription>> updatedSubscriptions = new Dictionary<long, List<NotificationSubscription>>();
        List<NotificationSubscription> closedSockets = new List<NotificationSubscription>();
        lock (subscriptions)
        {
          foreach (var channelId in subscriptions.Keys)
          {
            IEnumerable<NotificationSubscription> openSockets;

            openSockets = subscriptions[channelId].Where(x => x.WebSocket.State == WebSocketState.Open || x.WebSocket.State == WebSocketState.Connecting);
            closedSockets.AddRange(subscriptions[channelId].Where(x => x.WebSocket.State != WebSocketState.Open && x.WebSocket.State != WebSocketState.Connecting));

            updatedSubscriptions[channelId] = openSockets.ToList();
          }
          subscriptions = updatedSubscriptions;
        }

        // finish closing sockets
        foreach (var closedSocket in closedSockets)
        {
          await closedSocket.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed socket", CancellationToken.None);
        }
    }

    private class NotificationSubscription
    {
      public long ChannelId { get; set; }

      public long TokenId { get; set; }

      public WebSocket WebSocket { get; set; } 
    }
  }
}
