// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Dapper;
using Npgsql;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Options;
using SPVChannels.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;

namespace SPVChannels.Infrastructure.Repositories
{
  public class MessageRepositoryPostgres : BaseRepositoryPostgres, IMessageRepository
  {
    readonly ILogger<MessageRepositoryPostgres> logger;

    public MessageRepositoryPostgres(IOptions<AppConfiguration> op, ILogger<MessageRepositoryPostgres> logger) : base(op) 
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public long GetUnreadMessagesCount(long apiTokenId)
    {

      using var connection = GetNpgsqlConnection();
      connection.Open();

      return GetUnreadMessagesCount(apiTokenId, connection);
    }

    private long GetUnreadMessagesCount(long apiTokenId, NpgsqlConnection connection)
    {
      string selectUnreadMessageCount =
        "SELECT Count(MessageStatus.*) " +
        "FROM MessageStatus " +
        "WHERE MessageStatus.token = @tokenid " +
        "  AND MessageStatus.isread = FALSE " +
        "  AND MessageStatus.isdeleted = FALSE ";

      var unreadCount = connection.ExecuteScalar<long?>(
        selectUnreadMessageCount,
        new { tokenid = apiTokenId }
      );

      return unreadCount ?? 0;
    }

    public string GetMaxSequence(string token, string channelExternalId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectMaxSequence =
"SELECT MAX(Message.seq) AS max_sequence " +
"FROM Message " +
"INNER JOIN Channel ON Channel.id = Message.channel " +
"WHERE Channel.externalid = @channelExternalId " +
  "AND Channel.sequenced = true " +
  "AND EXISTS(" +
    "SELECT 'x' " +
    "FROM APIToken " +
    "WHERE APIToken.token = @token " +
    "AND (APIToken.validto IS NULL OR APIToken.validto >= @validto) " +
    "AND NOT APIToken.id = Message.fromtoken " +
  ") " +
  "AND EXISTS(" +
    "SELECT 'x' " +
    "FROM MessageStatus " +
    "WHERE MessageStatus.message = Message.id AND NOT MessageStatus.isdeleted);";

      var maxSequence = connection.ExecuteScalar<long?>(
        selectMaxSequence,
        new { token, validto = DateTime.UtcNow, channelExternalId }
      );

      return maxSequence.HasValue ? $"{maxSequence.Value}" : "0";
    }

    public IEnumerable<Message> GetMessages(long apiTokenId, bool onlyUnread, out string maxSeq)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string selectChannel =
        "SELECT Channel.sequenced " +
        "FROM Channel " +
        "INNER JOIN APIToken ON APIToken.channel = Channel.id " + 
        "WHERE APIToken.id = @tokenid " + 
        "FOR UPDATE";

      // Channel should exist as it is validated at authentication
      var channelData = connection.Query(
        selectChannel,
        new { tokenid = apiTokenId }
      ).First();

      if (channelData.sequenced)
      {
        string selectMaxSequence =
         "SELECT MAX(Message.seq) AS max_sequence " +
         "FROM Message " +
         "INNER JOIN MessageStatus ON MessageStatus.message = Message.id " +
         "WHERE MessageStatus.token = @tokenid AND NOT MessageStatus.isdeleted;";

        var maxSequence = connection.ExecuteScalar<long?>(
          selectMaxSequence,
          new { tokenid = apiTokenId }
        );

        maxSeq = maxSequence.ToString() ?? "0";
      }
      else
      {
        maxSeq = "";
      }

      string selectMessageById =
        "SELECT Message.* " +
        "FROM Message " +
        "INNER JOIN MessageStatus ON MessageStatus.message = Message.id " +
        "INNER JOIN APIToken ON MessageStatus.token = APIToken.id " +
        "WHERE APIToken.id = @tokenid " +
        "  AND MessageStatus.isdeleted = false " +
        "  AND (MessageStatus.isread = false OR @onlyunread = false) " +
        "ORDER BY Message.seq;";

      var data = connection.Query<Message>(
        selectMessageById,
        new 
        { 
          tokenid = apiTokenId,
          onlyunread = onlyUnread
        }
      ).ToArray();

      transaction.Commit();

      return data;
    }

    public Message WriteMessage(Message message, out int errorCode, out string errorMessage)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      errorCode = 0;
      errorMessage = "";

      // We are generating SEQ based on previous max value so we must retry in case 
      // a parallel request already used same SEQ that we generated.
      bool retryInsert;
      Message messageRes = null;
      do
      {
        try
        { 
          using NpgsqlTransaction transaction = connection.BeginTransaction();
          retryInsert = false;

          string selectChannel =
            "SELECT locked, sequenced " +
            "FROM Channel " +
            "WHERE id = @channel " +
            "FOR UPDATE;";

          // Channel should exist as it is validated at authentication
          var channelData = connection.Query(
            selectChannel,
            new { channel = message.Channel }
          ).First();

          if (channelData.locked)
          {
            errorCode = SPVChannelsHTTPError.ChannelLocked.Code;
            errorMessage = SPVChannelsHTTPError.ChannelLocked.Description;
            return null;
          }

          if (channelData.sequenced)
          {
            var unreadCount = GetUnreadMessagesCount(message.FromToken, connection);
            if (unreadCount > 0)
            {
              errorCode = SPVChannelsHTTPError.SequencingFailure.Code;
              errorMessage = SPVChannelsHTTPError.SequencingFailure.Description;
              return null;
            }
          }

          string insertMessage =
            "INSERT INTO Message " +
            "  (fromtoken, channel, seq, receivedts, contenttype, payload) " +
            "SELECT @fromtoken, @channel, COALESCE(MAX(seq) + 1, 1) AS seq, @receivedts, @contenttype, @payload " +
            "FROM Message " +
            "WHERE channel = @channel " +
            "RETURNING *;";

          messageRes = connection.Query<Message>(insertMessage,
            new
            {
              fromtoken = message.FromToken,
              channel = message.Channel,
              receivedts = message.ReceivedTS,
              contenttype = message.ContentType,
              payload = message.Payload
            }
          ).SingleOrDefault();


          string insertMessageStatus =
            "INSERT INTO MessageStatus " +
            "  (message, token, isread, isdeleted) " +
            "SELECT @messageid, id, " +
            "       CASE WHEN id = @author THEN TRUE ELSE FALSE END AS isread, " +
            "       FALSE AS isdeleted " +
            "FROM APIToken " +
            "WHERE validto IS NULL AND channel = @channel; ";
          connection.Execute(insertMessageStatus,
            new
            {
              messageid = messageRes.Id,
              channel = message.Channel,
              author = message.FromToken
            }
          );

          transaction.Commit();
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation &&
                                          e.ConstraintName == "message_mailbox_seq_key")
        {
          logger.LogInformation($"Failed to acquire unique message sequence when inserting new message. Will retry.");
          retryInsert = true;
        }
      } while (retryInsert);

      return messageRes;
    }

    public int MarkMessages(string channelExternalId, long apiTokenId, long sequenceId, bool older, bool isRead)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string updateMessageStatus =
        "UPDATE MessageStatus SET isread = @isread " +
        "WHERE MessageStatus.message IN ( " +
        "    SELECT Message.id " +
        "    FROM Message " +
        "    INNER JOIN Channel ON Message.channel = Channel.id " +
        "    WHERE Channel.externalid = @channelExternalId " +
        "    AND (Message.seq = @seq OR (Message.seq < @seq AND @markOlder = TRUE)) " +
        "  )" +
        "  AND MessageStatus.token = @token ";
        
      int recordsAffected = connection.Execute(updateMessageStatus,
        new
        {
          channelExternalId = channelExternalId,
          token = apiTokenId,
          seq = sequenceId,
          markOlder = older,
          isread = isRead
        }
      );

      transaction.Commit();

      return recordsAffected;
    }

    public bool SequenceExists(long apiTokenId, long sequenceId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectMaxSequence =
        "SELECT COUNT(Message.seq) AS seq_count " +
        "FROM Message " +
        "INNER JOIN MessageStatus ON MessageStatus.message = Message.id " +
        "WHERE MessageStatus.token = @tokenid " +
        "  AND Message.seq = @seq;";

      var sequenceCount = connection.ExecuteScalar<long?>(
        selectMaxSequence,
        new 
        { 
          tokenid = apiTokenId,
          seq = sequenceId
        }
      );

      return sequenceCount.HasValue && sequenceCount.Value == 1;
    }

    public Message GetMessage(string channelExternalId, long seq)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectChannelById =
        "SELECT Message.* " +
        "FROM Message " +
        "INNER JOIN MessageStatus ON MessageStatus.message = Message.id " +
        "INNER JOIN Channel ON Message.channel = Channel.id " +
        "WHERE Channel.externalid = @channelExternalId " +
        "  AND Message.seq = @seq " +
        "  AND MessageStatus.isdeleted = false;";

      var data = connection.Query<Message>(
        selectChannelById,
        new { channelExternalId, seq }
      ).FirstOrDefault();

      return data;
    }

    public Message GetMessageMetaData(string channelExternalId, long seq)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      string selectChannelById =
        "SELECT Message.id, Message.fromtoken, Message.channel, Message.seq, Message.receivedts, Message.contenttype " +
        "FROM Message " +
        "INNER JOIN MessageStatus ON MessageStatus.message = Message.id " +
        "INNER JOIN Channel ON Message.channel = Channel.id " +
        "WHERE Channel.externalid = @channelExternalId " +
        "  AND Message.seq = @seq " +
        "  AND MessageStatus.isdeleted = false;";

      var data = connection.Query<Message>(
        selectChannelById,
        new { channelExternalId, seq }
      ).FirstOrDefault();

      return data;
    }

    public bool DeleteMessage(long messageId)
    {
      using var connection = GetNpgsqlConnection();
      connection.Open();

      using NpgsqlTransaction transaction = connection.BeginTransaction();

      string deleteMessage =
        "UPDATE MessageStatus SET isdeleted = true WHERE message = @messageId;";

       int sequenceCount  = connection.Execute(deleteMessage, transaction: transaction, param: new { messageId });

      transaction.Commit();

      return sequenceCount != 1;
    }    
  }
}
