// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

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
