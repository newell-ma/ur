FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and build config
COPY RoyalGameOfUr.slnx Directory.Build.props Directory.Packages.props ./

# Copy project files for restore
COPY src/RoyalGameOfUr.Engine/RoyalGameOfUr.Engine.csproj src/RoyalGameOfUr.Engine/
COPY src/RoyalGameOfUr.Web/RoyalGameOfUr.Web.csproj src/RoyalGameOfUr.Web/
COPY src/RoyalGameOfUr.Server/RoyalGameOfUr.Server.csproj src/RoyalGameOfUr.Server/

# Restore (pulls in Engine + Web transitively)
RUN dotnet restore src/RoyalGameOfUr.Server/RoyalGameOfUr.Server.csproj

# Copy source
COPY src/ src/

# Publish
RUN dotnet publish src/RoyalGameOfUr.Server/RoyalGameOfUr.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "RoyalGameOfUr.Server.dll"]
