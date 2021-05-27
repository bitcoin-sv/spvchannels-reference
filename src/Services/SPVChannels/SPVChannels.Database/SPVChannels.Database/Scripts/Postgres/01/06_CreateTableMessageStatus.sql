-- Copyright (c) 2020 Bitcoin Association.
-- Distributed under the Open BSV software license, see the accompanying file LICENSE

CREATE TABLE MessageStatus (
  id            BIGSERIAL          NOT NULL,
  message       BIGINT             NOT NULL,
  token         BIGINT             NOT NULL,

  isread        boolean            NOT NULL,
  isdeleted     boolean            NOT NULL,

  PRIMARY KEY (id),
  FOREIGN KEY (message) REFERENCES Message (id),
  FOREIGN KEY (token) REFERENCES APIToken (id)
);

CREATE INDEX IMessageStatus_Token ON MessageStatus (id);
CREATE INDEX IMessageStatus_Isdeleted ON MessageStatus (isdeleted);
CREATE INDEX IMessageStatus_Token_Seq ON MessageStatus (token);
CREATE INDEX IMessageStatus_Token_Isread ON MessageStatus (token, isread);
CREATE INDEX IMessageStatus_Isdeleted_Isread ON MessageStatus (isdeleted, isread);

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE MessageStatus
TO GROUP channels_crud;

GRANT USAGE, SELECT 
ON SEQUENCE MessageStatus_id_seq
TO GROUP channels_crud;