#!/bin/bash
DIRECTORY="config"
if [ ! -d "$DIRECTORY" ]; then
	mkdir "$DIRECTORY"
fi
cat spvchannelsdata.tar | docker load
cat spvchannelsapi.tar | docker load
