using SPVChannels.Domain.Models;
using System.Collections.Generic;

namespace SPVChannels.Domain.Repositories
{
  public interface IMessageRepository
  {
    string GetMaxSequence(string apiToken, long channelId);

    long GetUnreadMessagesCount(long apiTokenId);

    IEnumerable<Message> GetMessages(long apiTokenId, bool onlyUndread, out string maxSeq);

    Message WriteMessage(Message message, out int errorCode, out string errorMessage);

    int MarkMessages(long channelId, long apiTokenId, long sequenceId, bool older, bool isRead);

    bool SequenceExists(long apiTokenId, long sequenceId);

    Message GetMessage(long channelId, long seq);

    Message GetMessageMetaData(long channel, long seq);

    bool DeleteMessage(long messageId);
  }
}
