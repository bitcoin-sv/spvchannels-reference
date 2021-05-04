// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using SPVChannels.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class ChannelViewModelCreate
  {
    [Required]
    [JsonPropertyName("public_read")]
    public bool PublicRead { get; set; }

    [Required]
    [JsonPropertyName("public_write")]
    public bool PublicWrite { get; set; }

    [Required]
    [JsonPropertyName("sequenced")]
    public bool Sequenced { get; set; }

    [Required]
    [JsonPropertyName("retention")]
    public RetentionViewModel Retention { get; set; }

    public Channel ToDomainObject(long owner = 0)
    {
      return new Channel {
        Owner =owner,
        PublicRead = PublicRead,
        PublicWrite = PublicWrite,
        Sequenced = Sequenced,
        MinAgeDays = Retention.Min_age_days,
        MaxAgeDays = Retention.Max_age_days,
        AutoPrune = Retention.Auto_prune
      };
    }
  }
}
