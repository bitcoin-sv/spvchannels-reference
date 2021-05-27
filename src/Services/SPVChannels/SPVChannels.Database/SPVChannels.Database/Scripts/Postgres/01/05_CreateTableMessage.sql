-- Copyright (c) 2020 Bitcoin Association.
-- Distributed under the Open BSV software license, see the accompanying file LICENSE

CREATE TABLE Message (
  id            BIGSERIAL          NOT NULL,
  fromtoken     BIGINT             NOT NULL,
  channel       BIGINT             NOT NULL,

  seq           BIGINT             NOT NULL,
  receivedts    TIMESTAMP          NOT NULL,
  contenttype   VARCHAR(64)        NOT NULL,
  payload       BYTEA,

  PRIMARY KEY (id),
  UNIQUE (channel, seq),
  FOREIGN KEY (fromtoken) REFERENCES APIToken (id),
  FOREIGN KEY (channel) REFERENCES Channel (id)
);

CREATE INDEX IMessage_Message ON Message (seq);
CREATE INDEX IMessage_Channel ON Message (channel);
CREATE INDEX IMessage_Channel_Seq ON Message (channel, seq);
CREATE INDEX IMessage_Channel_Seq_Isdeleted ON Message (channel, seq);

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE Message
TO GROUP channels_crud;

GRANT USAGE, SELECT 
ON SEQUENCE Message_id_seq
TO GROUP channels_crud;