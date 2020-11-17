using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class MessageViewModelMark
  {
    [JsonPropertyName("read")]
    public bool Read { get; set; }
  }
}
