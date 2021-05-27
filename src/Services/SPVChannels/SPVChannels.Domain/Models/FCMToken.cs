// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

namespace SPVChannels.Domain.Models
{
  public class FCMToken
  {
    public long Id { get; set; }

    public string Token { get; set; }

    public bool IsValid { get; set; }
  }
}
