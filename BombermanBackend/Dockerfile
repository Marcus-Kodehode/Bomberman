FROM mcr.microsoft.com/dotnet/aspnet:9.0-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:9.0-nanoserver-1809 AS build
ARG configuration=Release
WORKDIR /src
COPY ["BombermanBackend/BombermanBackend.csproj", "BombermanBackend/"]
RUN dotnet restore "BombermanBackend\BombermanBackend.csproj"
COPY . .
WORKDIR "/src/BombermanBackend"
RUN dotnet build "BombermanBackend.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "BombermanBackend.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BombermanBackend.dll"]
