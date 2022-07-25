#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0.100-bullseye-slim AS build
WORKDIR /src
COPY ["Domain/ReconNessAgent.Domain.Core/ReconNessAgent.Domain.Core.csproj", "Domain/ReconNessAgent.Domain.Core/"]
COPY ["Application/ReconNessAgent.Application/ReconNessAgent.Application.csproj", "Application/ReconNessAgent.Application/"]
COPY ["Application/ReconNessAgent.Application.Services/ReconNessAgent.Application.Services.csproj", "Application/ReconNessAgent.Application.Services/"]
COPY ["Infrastructure/ReconNessAgent.Infrastructure/ReconNessAgent.Infrastructure.csproj", "Infrastructure/ReconNessAgent.Infrastructure/"]
COPY ["Infrastructure/Data/ReconNessAgent.Infrastructure.Data.EF/ReconNessAgent.Infrastructure.Data.EF.csproj", "Infrastructure/Data/ReconNessAgent.Infrastructure.Data.EF/"]
COPY ["Infrastructure/ReconNessAgent.Infrastructure.Worker/ReconNessAgent.Infrastructure.Worker.csproj", "Infrastructure/ReconNessAgent.Infrastructure.Worker/"]
RUN dotnet restore "Infrastructure/ReconNessAgent.Infrastructure.Worker/ReconNessAgent.Infrastructure.Worker.csproj"
COPY . .
WORKDIR "/src/Infrastructure/ReconNessAgent.Infrastructure.Worker"
RUN dotnet build "ReconNessAgent.Infrastructure.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReconNessAgent.Infrastructure.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# -------- Agents dependencies -------- 

# -------- End Agents dependencies -------- 

ENTRYPOINT ["dotnet", "ReconNessAgent.Infrastructure.Worker.dll"]