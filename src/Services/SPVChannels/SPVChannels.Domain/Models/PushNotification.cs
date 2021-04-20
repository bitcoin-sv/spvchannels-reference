
using System;
using System.Text.Json.Serialization;

namespace SPVChannels.Domain.Models
{
  public class PushNotification
  {    
    public Channel Channel { get; set; }

    public string Message { get; set; }

    public DateTime Received { get; set; }
  }
}
