// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;

namespace SPVChannels.Domain.Models
{
  public class PushNotification
  {    
    public Channel Channel { get; set; }

    public string Message { get; set; }

    public DateTime Received { get; set; }
  }
}
