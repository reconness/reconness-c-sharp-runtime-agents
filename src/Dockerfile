#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0.100-bullseye-slim AS build
WORKDIR /src
COPY ["Domain/ReconNessAgent.Domain/ReconNessAgent.Domain.Core.csproj", "Domain/ReconNessAgent.Domain/"]
COPY ["Application/ReconNessAgent.Application/ReconNessAgent.Application.csproj", "Application/ReconNessAgent.Application/"]
COPY ["Infrestructure/ReconNessAgent.Data.Dapper/ReconNessAgent.Data.Dapper.csproj", "Infrestructure/ReconNessAgent.Data.Dapper/"]
COPY ["Infrestructure/ReconNessAgent.Worker/ReconNessAgent.Worker.csproj", "Infrestructure/ReconNessAgent.Worker/"]
RUN dotnet restore "Infrestructure/ReconNessAgent.Worker/ReconNessAgent.Worker.csproj"
COPY . .
WORKDIR "/src/Infrestructure/ReconNessAgent.Worker"
RUN dotnet build "ReconNessAgent.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReconNessAgent.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReconNessAgent.Worker.dll"]