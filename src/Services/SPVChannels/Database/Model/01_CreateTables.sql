CREATE TABLE Account (
  id            BIGSERIAL          NOT NULL,

  name          VARCHAR(256),

  PRIMARY KEY (id)
);

CREATE TABLE AccountCredential (
  id            BIGSERIAL          NOT NULL,
  account       BIGINT             NOT NULL,

  scheme        VARCHAR(1024),
  credential    VARCHAR(1024),

  PRIMARY KEY (id),
  FOREIGN KEY (account) REFERENCES Account (id)
);

CREATE TABLE Channel (
  id            BIGSERIAL          NOT NULL,
  owner         BIGINT             NOT NULL,

  publicread    boolean,
  publicwrite   boolean,
  locked        boolean,
  sequenced     boolean,
  minagedays    INT,
  maxagedays    INT,
  autoprune     boolean,

  PRIMARY KEY (id),
  FOREIGN KEY (owner) REFERENCES Account (id)
);

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