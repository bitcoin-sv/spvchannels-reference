-- Copyright (c) 2020 Bitcoin Association.
-- Distributed under the Open BSV software license, see the accompanying file LICENSE

CREATE TABLE IF NOT EXISTS AccountCredential (
  id            BIGSERIAL          NOT NULL,
  account       BIGINT             NOT NULL,

  scheme        VARCHAR(1024),
  credential    VARCHAR(1024),

  PRIMARY KEY (id),
  FOREIGN KEY (account) REFERENCES Account (id)
);

CREATE INDEX IAccountCredential_Scheme_Credential ON AccountCredential (scheme, credential);

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE AccountCredential
TO GROUP channels_crud;

GRANT USAGE, SELECT 
ON SEQUENCE AccountCredential_id_seq
TO GROUP channels_crud;