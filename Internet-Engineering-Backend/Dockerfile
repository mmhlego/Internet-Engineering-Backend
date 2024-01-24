FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["Internet-Engineering-Backend/Internet-Engineering-Backend.csproj", "Internet-Engineering-Backend/"]
RUN dotnet restore "Internet-Engineering-Backend/Internet-Engineering-Backend.csproj"
COPY . .
WORKDIR "/src/Internet-Engineering-Backend"
RUN dotnet build "Internet-Engineering-Backend.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Internet-Engineering-Backend.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Internet-Engineering-Backend.dll"]
