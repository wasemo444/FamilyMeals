# LinkNest API — Render entry (same image as Dockerfile.api in repo root)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/LinkNest.Shared/LinkNest.Shared.csproj", "src/LinkNest.Shared/"]
COPY ["src/LinkNest.Api/LinkNest.Api.csproj", "src/LinkNest.Api/"]
RUN dotnet restore "src/LinkNest.Api/LinkNest.Api.csproj"

COPY ["src/LinkNest.Shared/", "src/LinkNest.Shared/"]
COPY ["src/LinkNest.Api/", "src/LinkNest.Api/"]

RUN dotnet publish "src/LinkNest.Api/LinkNest.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN mkdir -p /keys
VOLUME ["/keys"]

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkNest.Api.dll"]
