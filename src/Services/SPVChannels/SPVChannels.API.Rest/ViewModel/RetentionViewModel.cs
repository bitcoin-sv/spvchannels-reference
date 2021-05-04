// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class RetentionViewModel
  {
    [JsonPropertyName("min_age_days")]
    public int? Min_age_days { get; set; }

    [JsonPropertyName("max_age_days")]
    public int? Max_age_days { get; set; }

    [Required]
    [JsonPropertyName("auto_prune")]
    public bool Auto_prune { get; set; }

    public bool IsValid()
    {
      if (Min_age_days.HasValue && Max_age_days.HasValue && Min_age_days > Max_age_days)
        return false;
      return true;
    }
  }
}
