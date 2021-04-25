using SPVChannels.Domain.Models;
using System;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Notification
{
  public interface INotificationHandler
  {
    Task SendNotification(long sourceTokenId, PushNotification notification);
  }
}
