FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" notification-workeruser && chown -R notification-workeruser:notification-workeruser /app
USER notification-workeruser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src
COPY ["bbt.notification.worker.csproj", "./"]
RUN dotnet restore "bbt.notification.worker.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "bbt.notification.worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "bbt.notification.worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY kafka_nonprod.cer kafka_prod.cer .
COPY --from=publish /app/publish .
ENV Topic_Id=10158
ENTRYPOINT ["dotnet", "bbt.notification.worker.dll"]
