-- Copyright (c) 2020 Bitcoin Association.
-- Distributed under the Open BSV software license, see the accompanying file LICENSE

CREATE TABLE IF NOT EXISTS Channel (
  id            BIGSERIAL          NOT NULL,
  owner         BIGINT             NOT NULL,

  externalid    VARCHAR(1024)      NOT NULL,
  publicread    boolean,
  publicwrite   boolean,
  locked        boolean,
  sequenced     boolean,
  minagedays    INT,
  maxagedays    INT,
  autoprune     boolean,

  PRIMARY KEY (id),
  UNIQUE (externalid),
  FOREIGN KEY (owner) REFERENCES Account (id)
);

CREATE INDEX IChannel_Owner ON Channel (owner);
CREATE INDEX IChannel_Id ON Channel (id);
CREATE INDEX IChannel_Externalid ON Channel (externalid);
CREATE INDEX IChannel_Owner_Id ON Channel (owner, id);
CREATE INDEX IChannel_Owner_Externalid ON Channel (owner, externalid);

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE Channel
TO GROUP channels_crud;

GRANT USAGE, SELECT 
ON SEQUENCE Channel_id_seq
TO GROUP channels_crud;