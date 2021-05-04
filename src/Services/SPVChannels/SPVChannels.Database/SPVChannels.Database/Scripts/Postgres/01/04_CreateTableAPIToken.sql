-- Copyright (c) 2020 Bitcoin Association.
-- Distributed under the Open BSV software license, see the accompanying file LICENSE

CREATE TABLE APIToken (
  id            BIGSERIAL          NOT NULL,
  account       BIGINT             NOT NULL,
  channel       BIGINT             NOT NULL,

  token		VARCHAR(1024),
  description   VARCHAR(1024),
  canread       boolean,
  canwrite      boolean,
  validfrom     TIMESTAMP          NOT NULL,
  validto       TIMESTAMP,

  PRIMARY KEY (id),
  UNIQUE (token),
  FOREIGN KEY (account) REFERENCES Account (id),
  FOREIGN KEY (channel) REFERENCES Channel (id)
);

CREATE INDEX IAPIToken_Id ON APIToken (id);
CREATE INDEX IAPIToken_Token ON APIToken (token);
CREATE INDEX IAPIToken_Channel ON APIToken (channel);
CREATE INDEX IAPIToken_Channel_Validto ON APIToken (channel, validto);
CREATE INDEX IAPIToken_Account_Validto ON APIToken (account, validto);
CREATE INDEX IAPIToken_Account_Channel_Id ON APIToken (account, channel, id);

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE APIToken
TO GROUP channels_crud;

GRANT USAGE, SELECT 
ON SEQUENCE APIToken_id_seq
TO GROUP channels_crud;