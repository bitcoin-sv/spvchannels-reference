#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
ARG APPVERSION
WORKDIR /src
COPY ["src/Services/SPVChannels/SPVChannels.API.Rest/SPVChannels.API.Rest.csproj", "Services/SPVChannels/SPVChannels.API.Rest/"]
COPY ["src/Services/SPVChannels/SPVChannels.Infrastructure/SPVChannels.Infrastructure.csproj", "Services/SPVChannels/SPVChannels.Infrastructure/"]
COPY ["src/Services/SPVChannels/SPVChannels.Database/SPVChannels.Database.csproj", "Services/SPVChannels/SPVChannels.Database/"]
COPY ["src/Services/SPVChannels/SPVChannels.Domain/SPVChannels.Domain.csproj", "Services/SPVChannels/SPVChannels.Domain/"]

RUN dotnet restore "Services/SPVChannels/SPVChannels.API.Rest/SPVChannels.API.Rest.csproj"
COPY src/. .
WORKDIR "/src/Services/SPVChannels/SPVChannels.API.Rest"
RUN dotnet build "SPVChannels.API.Rest.csproj" -c Release /p:Version=$APPVERSION -o /app/build

FROM build AS publish
ARG APPVERSION
RUN dotnet publish "SPVChannels.API.Rest.csproj" -c Release /p:Version=$APPVERSION -o /app/publish

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS tools-install
RUN dotnet tool install --tool-path /dotnetcore-tools dotnet-sos
RUN dotnet tool install --tool-path /dotnetcore-tools dotnet-trace
RUN dotnet tool install --tool-path /dotnetcore-tools dotnet-dump
RUN dotnet tool install --tool-path /dotnetcore-tools dotnet-counters

RUN apt-get update \
  && apt-get upgrade -y \
  && apt-get install -y \
     file \
     lldb \
  && rm -rf /var/lib/apt/lists/*

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY --from=tools-install /dotnetcore-tools /opt/dotnetcore-tools
ENV PATH="/opt/dotnetcore-tools:${PATH}"
RUN dotnet-sos install

ENTRYPOINT ["dotnet", "SPVChannels.API.Rest.dll"]