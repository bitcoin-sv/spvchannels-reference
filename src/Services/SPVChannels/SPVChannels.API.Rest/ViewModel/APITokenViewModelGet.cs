// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using SPVChannels.Domain.Models;
using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class APITokenViewModelGet
  {
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("can_read")]
    public bool Can_read { get; set; }

    [JsonPropertyName("can_write")]
    public bool Can_write { get; set; }

    public APITokenViewModelGet() { }

    public APITokenViewModelGet(APIToken APIToken)
    {
      Id = APIToken.Id.ToString();
      Token = APIToken.Token;
      Description = APIToken.Description;
      Can_read = APIToken.CanRead;
      Can_write = APIToken.CanWrite;
    }
  }
}
