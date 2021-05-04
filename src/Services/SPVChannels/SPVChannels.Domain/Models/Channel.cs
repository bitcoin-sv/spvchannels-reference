// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace SPVChannels.Domain.Models
{
  public class Channel
  {
    public long Id { get; set; }

    public string ExternalId { get; set; }

    public long Owner { get; set; }

    public bool PublicRead { get; set; }

    public bool PublicWrite { get; set; }

    public bool Locked { get; set; }

    public bool Sequenced { get; set; }

    public int? MinAgeDays { get; set; }

    public int? MaxAgeDays { get; set; }

    public bool AutoPrune { get; set; }

    public IEnumerable<APIToken> APIToken { get; set; }

    /// <summary>
    /// Calculated max message sequence
    /// </summary>
    public long HeadMessageSequence { get; set; }

    public static string CreateExternalId()
    {
      byte[] data = new byte[64];
      using RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
      crypto.GetBytes(data);

      return WebEncoders.Base64UrlEncode(data);
    }
  }
}
