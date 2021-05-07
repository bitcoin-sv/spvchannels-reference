// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace SPVChannels.Domain.Models
{
  public class APIToken
  {
    public long Id { get; set; }

    public long Account { get; set; }

    public long Channel { get; set; }

    public string Token { get; set; }

    public IList<FCMToken> FCMTokens { get; set; }

    public string Description { get; set; }

    public bool CanRead { get; set; }

    public bool CanWrite { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public void CreateToken(int tokenSize)
    {
      byte[] data = new byte[tokenSize];
      using RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
      crypto.GetBytes(data);
      
      Token = WebEncoders.Base64UrlEncode(data);
    }
  }
}