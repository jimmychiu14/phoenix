FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Phoenix.sln ./
COPY src/Application/Phoenix.Application/Phoenix.Application.csproj ./src/Application/Phoenix.Application/
COPY src/Domain/Phoenix.Domain/Phoenix.Domain.csproj ./src/Domain/Phoenix.Domain/
COPY src/Infrastructure/Phoenix.Infrastructure/Phoenix.Infrastructure.csproj ./src/Infrastructure/Phoenix.Infrastructure/
COPY src/Presentation/Phoenix.Api/Phoenix.Api.csproj ./src/Presentation/Phoenix.Api/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build
RUN dotnet build -c Release --no-restore

# Publish
RUN dotnet publish src/Presentation/Phoenix.Api/Phoenix.Api.csproj -c Release -o /app/publish --no-build

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Phoenix.Api.dll"]
