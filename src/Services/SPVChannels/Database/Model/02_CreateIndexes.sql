CREATE INDEX IAccount_Name ON Account (name);

CREATE INDEX IAccountCredential_Scheme_Credential ON AccountCredential (scheme, credential);

CREATE INDEX IChannel_Owner ON Channel (owner);
CREATE INDEX IChannel_Id ON Channel (id);
CREATE INDEX IChannel_Owner_Id ON Channel (owner, id);

CREATE INDEX IAPIToken_Id ON APIToken (id);
CREATE INDEX IAPIToken_Token ON APIToken (token);
CREATE INDEX IAPIToken_Channel ON APIToken (channel);
CREATE INDEX IAPIToken_Channel_Validto ON APIToken (channel, validto);
CREATE INDEX IAPIToken_Account_Validto ON APIToken (account, validto);
CREATE INDEX IAPIToken_Account_Channel_Id ON APIToken (account, channel, id);

CREATE INDEX IMessage_Message ON Message (seq);
CREATE INDEX IMessage_Channel ON Message (channel);
CREATE INDEX IMessage_Channel_Seq ON Message (channel, seq);
CREATE INDEX IMessage_Channel_Seq_Isdeleted ON Message (channel, seq);

CREATE INDEX IMessageStatus_Token ON MessageStatus (id);
CREATE INDEX IMessageStatus_Isdeleted ON MessageStatus (isdeleted);
CREATE INDEX IMessageStatus_Token_Seq ON MessageStatus (token);
CREATE INDEX IMessageStatus_Token_Isread ON MessageStatus (token, isread);
CREATE INDEX IMessageStatus_Isdeleted_Isread ON MessageStatus (isdeleted, isread);