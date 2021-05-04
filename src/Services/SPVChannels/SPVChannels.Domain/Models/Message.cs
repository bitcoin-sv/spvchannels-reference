// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;

namespace SPVChannels.Domain.Models
{
  public class Message
  {
    public long Id { get; set; }

    public long FromToken { get; set; }

    public long Channel { get; set; }
    
    public long Seq { get; set; }

    public DateTime ReceivedTS { get; set; }

    public string ContentType { get; set; }

    public byte[] Payload { get; set; }
  }
}