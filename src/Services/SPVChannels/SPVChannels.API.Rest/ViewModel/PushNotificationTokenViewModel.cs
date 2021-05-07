// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class PushNotificationTokenViewModel
  {
    [JsonPropertyName("token")]
    public string Token { get; set; }
  }
}
