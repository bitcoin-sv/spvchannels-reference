-- Copyright (c) 2020 Bitcoin Association.
-- Distributed under the Open BSV software license, see the accompanying file LICENSE

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT FROM pg_catalog.pg_roles  
    WHERE  rolname = 'channels_crud') THEN
	  CREATE ROLE "channels_crud" WITH
	    NOLOGIN
	    NOSUPERUSER
	    INHERIT
	    NOCREATEDB
	    NOCREATEROLE
	    NOREPLICATION;
  END IF;

  DROP ROLE IF EXISTS channels;

  CREATE ROLE channels LOGIN
    PASSWORD 'channels'
    NOSUPERUSER INHERIT NOCREATEDB NOCREATEROLE NOREPLICATION;

  GRANT channels_crud TO channels;

  DROP ROLE IF EXISTS channelsddl;

  CREATE ROLE channelsddl LOGIN
    PASSWORD 'channels'
	NOSUPERUSER INHERIT NOCREATEDB NOCREATEROLE NOREPLICATION;
END
$$;