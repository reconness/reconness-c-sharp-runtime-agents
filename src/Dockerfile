#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0.100-bullseye-slim AS build
WORKDIR /src
COPY ["ReconNessAgent.Worker/ReconNessAgent.Worker.csproj", "ReconNessAgent.Worker/"]
RUN dotnet restore "ReconNessAgent.Worker/ReconNessAgent.Worker.csproj"
COPY . .
WORKDIR "/src/ReconNessAgent.Worker"
RUN dotnet build "ReconNessAgent.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReconNessAgent.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReconNessAgent.Worker.dll"]