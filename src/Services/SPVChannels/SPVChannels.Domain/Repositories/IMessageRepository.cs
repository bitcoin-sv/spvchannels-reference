// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using SPVChannels.Domain.Models;
using System.Collections.Generic;

namespace SPVChannels.Domain.Repositories
{
  public interface IMessageRepository
  {
    string GetMaxSequence(string apiToken, string channelExternalId);

    long GetUnreadMessagesCount(long apiTokenId);

    IEnumerable<Message> GetMessages(long apiTokenId, bool onlyUndread, out string maxSeq);

    Message WriteMessage(Message message, out int errorCode, out string errorMessage);

    int MarkMessages(string channelExternalId, long apiTokenId, long sequenceId, bool older, bool isRead);

    bool SequenceExists(long apiTokenId, long sequenceId);

    Message GetMessage(string channelExternalId, long seq);

    Message GetMessageMetaData(string channelExternalId, long seq);

    bool DeleteMessage(long messageId);
  }
}
