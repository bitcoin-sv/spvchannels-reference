-- Copyright (c) 2020 Bitcoin Association.
-- Distributed under the Open BSV software license, see the accompanying file LICENSE

CREATE TABLE FCMToken (
  id            BIGSERIAL          NOT NULL,
  apitoken      BIGINT             NOT NULL,
  token         VARCHAR(1024),
  isvalid       BOOLEAN,
  PRIMARY KEY (id),
  UNIQUE (apitoken, token),
  FOREIGN KEY (apitoken) REFERENCES APIToken (id)
);

CREATE INDEX IFCMToken_Token ON FCMToken (token);
CREATE INDEX IFCMToken_APIToken ON FCMToken (apitoken);
CREATE INDEX IFCMToken_Token_APIToken ON FCMToken (token, apitoken);

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE FCMToken 
TO GROUP channels_crud;

GRANT USAGE, SELECT 
ON SEQUENCE FCMToken_id_seq
TO GROUP channels_crud;
