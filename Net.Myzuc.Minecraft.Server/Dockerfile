FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Net.Myzuc.MME/Net.Myzuc.MME.csproj", "Net.Myzuc.MME/"]
RUN dotnet restore "Net.Myzuc.MME/Net.Myzuc.MME.csproj"
COPY . .
WORKDIR "/src/Net.Myzuc.MME"
RUN dotnet build "./Net.Myzuc.MME.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Net.Myzuc.MME.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Net.Myzuc.MME.dll"]
