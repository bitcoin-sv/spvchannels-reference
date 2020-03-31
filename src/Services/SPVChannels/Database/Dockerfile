FROM postgres

COPY ["src/Services/SPVChannels/Database/Model/01_CreateTables.sql", "docker-entrypoint-initdb.d/"]
COPY ["src/Services/SPVChannels/Database/Model/02_CreateIndexes.sql", "docker-entrypoint-initdb.d/"]
COPY ["src/Services/SPVChannels/Database/Model/03_CreateUser.sql", "docker-entrypoint-initdb.d/"]