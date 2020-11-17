#!/bin/bash

read -r VERSIONPREFIX<version.txt

cd ..

git remote update
git pull
git status -uno

COMMITID=$(git rev-parse --short HEAD)

APPVERSION="$VERSIONPREFIX-$COMMITID"

echo "***************************"
echo "***************************"
echo "Building docker image for version $APPVERSION"
read -p "Continue if you have latest version (commit $COMMITID) or terminate job and get latest files."

mkdir -p deploy
cd build

sed -e s/{{VERSION}}/$VERSIONPREFIX/ < template-docker-compose.yml > ../deploy/docker-compose.yml

cp .env ../deploy/.env

cp install/* ../deploy

cd ..

docker build --build-arg APPVERSION=$APPVERSION -t bitcoinsv/spvchannels:$VERSIONPREFIX -f src/Services/SPVChannels/SPVChannels.API.Rest/Dockerfile .
docker build  -t bitcoinsv/spvchannels-db:$VERSIONPREFIX -f src/Services/SPVChannels/Database/Dockerfile .

docker save bitcoinsv/spvchannels:$VERSIONPREFIX > deploy/spvchannelsapi.tar
docker save bitcoinsv/spvchannels-db:$VERSIONPREFIX > deploy/spvchannelsdata.tar
