# syntax=docker/dockerfile:1
#
# Multi-stage build for the Notes API.
#   - "build" stage uses the full .NET SDK to restore + publish.
#   - "final" stage uses the smaller ASP.NET runtime image to run it.
# Build context is the api/ folder (see docker-compose.yml).

# ---- build ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first and restore — this layer is cached and only
# re-runs when a .csproj changes, so source edits don't re-download packages.
COPY NotesApp.sln ./
COPY src/NotesApp.Api/NotesApp.Api.csproj src/NotesApp.Api/
COPY src/NotesApp.Application/NotesApp.Application.csproj src/NotesApp.Application/
COPY src/NotesApp.Domain/NotesApp.Domain.csproj src/NotesApp.Domain/
COPY src/NotesApp.Infrastructure/NotesApp.Infrastructure.csproj src/NotesApp.Infrastructure/
RUN dotnet restore src/NotesApp.Api/NotesApp.Api.csproj

# Copy the rest and publish a release build.
COPY src/ src/
RUN dotnet publish src/NotesApp.Api/NotesApp.Api.csproj -c Release -o /app --no-restore

# ---- final ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app ./

# Kestrel listens on 8080 inside the container (ASP.NET Core 8 default).
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "NotesApp.Api.dll"]
