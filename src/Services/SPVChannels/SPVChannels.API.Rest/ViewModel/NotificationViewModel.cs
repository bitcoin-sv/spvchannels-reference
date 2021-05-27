// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class NotificationViewModel
  {
    [JsonPropertyName("channel_id")]
    public string Channel { get; set; }

    [JsonPropertyName("notification")]
    public string Notification { get; set; }

    [JsonPropertyName("received")]
    public DateTime Received { get; set; }
  }
}