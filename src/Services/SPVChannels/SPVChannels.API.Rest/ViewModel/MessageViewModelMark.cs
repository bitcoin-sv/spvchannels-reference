// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class MessageViewModelMark
  {
    [JsonPropertyName("read")]
    public bool Read { get; set; }
  }
}
