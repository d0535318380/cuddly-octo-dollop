FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Crawler.Host/Crawler.Host.csproj", "Crawler.Host/"]
RUN dotnet restore "Crawler.Host/Crawler.Host.csproj"
COPY . .
WORKDIR "/src/Crawler.Host"
RUN dotnet build "Crawler.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Crawler.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Crawler.Host.dll"]
