@ECHO OFF

SET /p VERSIONPREFIX=<version.txt

cd ..

git remote update
git pull
git status -uno

FOR /F %%i IN ('git rev-parse --short HEAD') DO SET COMMITID=%%i

SET APPVERSION=%VERSIONPREFIX%-%COMMITID%

ECHO *******************************
ECHO *******************************
ECHO Building docker image for version %APPVERSION%
ECHO Continue if you have latest version (commit %COMMITID%) or terminate job and get latest files.

PAUSE

if not exist "deploy" mkdir "deploy"
cd build

SETLOCAL ENABLEDELAYEDEXPANSION
(
  for /f "delims=" %%A in (template-docker-compose.yml) do (
    set "line=%%A"
	set "line=!line:{{VERSION}}=%VERSIONPREFIX%!"
    echo(!line!
  )
)>../deploy/docker-compose.yml

copy /y .env ..\deploy\.env

ROBOCOPY .\\install ..\\deploy *.*

cd ..

docker build --build-arg APPVERSION=%APPVERSION% -t bitcoinsv/spvchannels:%VERSIONPREFIX% -f src/Services/SPVChannels/SPVChannels.API.Rest/Dockerfile .

docker save bitcoinsv/spvchannels:%VERSIONPREFIX% -o deploy/spvchannelsapi.tar