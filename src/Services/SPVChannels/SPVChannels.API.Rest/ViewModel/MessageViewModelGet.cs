// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Text.Json.Serialization;
using SPVChannels.Domain.Models;

namespace SPVChannels.API.Rest.ViewModel
{
  public class MessageViewModelGet
  {
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    [JsonPropertyName("received")]
    public DateTime Received { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("payload")]
    public string Payload { get; set; }
    public MessageViewModelGet() { }

    public MessageViewModelGet(Message message)
    {
      Sequence = message.Seq;
      Received = message.ReceivedTS;
      ContentType = message.ContentType;
      Payload = Convert.ToBase64String(message.Payload);
    }
  }
}
