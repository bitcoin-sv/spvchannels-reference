--by default users should not have create permission in shema public 
REVOKE CREATE ON SCHEMA public FROM public;
--ddl user should be the owner of public shema
ALTER SCHEMA public OWNER TO channelsddl;
