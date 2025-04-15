# Mono Repo/Dockerfile (Using Intermediate Stage)

# Stage 0: Copy the entire build context
FROM scratch AS context_copier
COPY . .

# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy the entire BombermanBackend directory to maintain project structure
COPY --from=context_copier /BombermanBackend /source/BombermanBackend

# Copy the BombermanBackend.Contracts directory to maintain the expected relative path
COPY --from=context_copier /BombermanBackend.Contracts /source/BombermanBackend.Contracts

# Restore dependencies using the solution file
WORKDIR /source/BombermanBackend
RUN dotnet restore "BombermanBackend.sln"

# Copy any additional files if needed
# COPY --from=context_copier . .

# Publish the application
RUN dotnet publish "BombermanBackend.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Serve
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BombermanBackend.dll"]