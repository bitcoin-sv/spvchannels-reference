// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Net;

namespace SPVChannels.Infrastructure.Utilities
{
  public class Enumeration
   : IComparable
  {
    public string Description { get; private set; }

    public int Code { get; private set; }

    protected Enumeration(int code, string description)
    {
      Code = code;
      Description = description;
    }

    public override string ToString() => Description;

    public int CompareTo(object other) => Code.CompareTo(((Enumeration)other).Code);

  }

  public class SPVChannelsHTTPError : Enumeration
  {
    public static readonly SPVChannelsHTTPError Unauthorized = new SPVChannelsHTTPError((int)HttpStatusCode.Unauthorized, $"The authorization header provided was not valid.");
    public static readonly SPVChannelsHTTPError Forbidden = new SPVChannelsHTTPError((int)HttpStatusCode.Forbidden, $"The authorization header provided was valid, but the request was not authorized. This is because either the account is disabled, or the account holder is not the owner of the specified Channel.");
    public static readonly SPVChannelsHTTPError NotFound = new SPVChannelsHTTPError((int) HttpStatusCode.NotFound, $"The Channel, API token, or other resource was not found.");
    public static readonly SPVChannelsHTTPError RetentionNotExpired = new SPVChannelsHTTPError((int)HttpStatusCode.BadRequest, $"Retention period has not yet expired.");
    public static readonly SPVChannelsHTTPError RetentionInvalidMinMax = new SPVChannelsHTTPError((int)HttpStatusCode.BadRequest, $"Invalid retention: max days should be greater than min days.");
    public static readonly SPVChannelsHTTPError ChannelLocked = new SPVChannelsHTTPError((int) HttpStatusCode.Forbidden, $"Channel is locked");
    public static readonly SPVChannelsHTTPError SequencingFailure = new SPVChannelsHTTPError((int)HttpStatusCode.Conflict, $"Sequencing Failure.");
    private SPVChannelsHTTPError(int code, string description)
        : base(code, description)
    {
    }
  }
}
