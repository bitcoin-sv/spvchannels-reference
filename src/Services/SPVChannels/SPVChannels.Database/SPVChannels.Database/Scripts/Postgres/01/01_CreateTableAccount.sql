CREATE TABLE IF NOT EXISTS Account (
  id            BIGSERIAL          NOT NULL,
  name          VARCHAR(256),

  PRIMARY KEY (id)    
);

CREATE UNIQUE INDEX IF NOT EXISTS IAccount_Name ON Account (name);

GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLE Account
TO GROUP channels_crud;

GRANT USAGE, SELECT 
ON SEQUENCE Account_id_seq
TO GROUP channels_crud;