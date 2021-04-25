using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class PushNotificationTokenViewModel
  {
    [JsonPropertyName("token")]
    public string Token { get; set; }
  }
}
