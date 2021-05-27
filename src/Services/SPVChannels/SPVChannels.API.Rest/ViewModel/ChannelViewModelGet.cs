// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using SPVChannels.Domain.Models;
using System.Linq;
using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class ChannelViewModelGet
  {
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("href")]
    public string Href { get; set; }

    [JsonPropertyName("public_read")]
    public bool PublicRead { get; set; }

    [JsonPropertyName("public_write")]
    public bool PublicWrite { get; set; }

    [JsonPropertyName("sequenced")]
    public bool Sequenced { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("head")]
    public long HeadSequence { get; set; }

    [JsonPropertyName("retention")]
    public RetentionViewModel Retention { get; set; }

    [JsonPropertyName("access_tokens")]
    public APITokenViewModelGet[] APIToken { get; set; }


    public ChannelViewModelGet() { }

    public ChannelViewModelGet(Channel channel)
    {
      Id = channel.ExternalId;
      APIToken = (from apiToken in channel.APIToken
                  select new APITokenViewModelGet
                  {
                    Id = apiToken.Id.ToString(),
                    Token = apiToken.Token,
                    Description = apiToken.Description,
                    Can_read = apiToken.CanRead,
                    Can_write = apiToken.CanWrite
                  }).ToArray();
      PublicRead = channel.PublicRead;
      PublicWrite = channel.PublicWrite;
      Locked = channel.Locked;
      Sequenced = channel.Sequenced;
      HeadSequence = channel.HeadMessageSequence;
      Retention = new RetentionViewModel
      {
        Min_age_days = channel.MinAgeDays,
        Max_age_days = channel.MaxAgeDays,
        Auto_prune = channel.AutoPrune
      };
    }

    public ChannelViewModelGet(Channel channel, string href) : this(channel)
    {
      Href = href;
    }
  }
}
