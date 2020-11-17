#!/bin/bash
DIRECTORY="config"
if [ -d "$DIRECTORY" ]; then
	rm -rf "$DIRECTORY"
fi
docker volume rm spvchannels-volume
docker network rm spvchannels-network
docker image rm bitcoinsv/spvchannels-db:$1
docker image rm bitcoinsv/spvchannels:$1
