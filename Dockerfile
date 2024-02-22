FROM mcr.microsoft.com/dotnet/sdk:8.0
MAINTAINER chris@sutcliff.me
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# copy and build everything else
COPY . ./
RUN dotnet publish -c Release -o out

EXPOSE 5000
ENV DOMAIN_NAME=example.com
ENV EXTERNAL_IP=172.16.1.1

ENTRYPOINT ["dotnet", "out/DummyServer.dll"]
