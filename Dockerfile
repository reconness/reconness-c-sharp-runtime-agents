#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0.100-bullseye-slim AS build

COPY ["/src/Domain/ReconNessAgent.Domain.Core/ReconNessAgent.Domain.Core.csproj", "/src/Domain/ReconNessAgent.Domain.Core/"]
COPY ["/src/Application/ReconNessAgent.Application/ReconNessAgent.Application.csproj", "/src/Application/ReconNessAgent.Application/"]
COPY ["/src/Application/ReconNessAgent.Application.Services/ReconNessAgent.Application.Services.csproj", "/src/Application/ReconNessAgent.Application.Services/"]
COPY ["/src/Infrastructure/ReconNessAgent.Infrastructure/ReconNessAgent.Infrastructure.csproj", "/src/Infrastructure/ReconNessAgent.Infrastructure/"]
COPY ["/src/Infrastructure/Data/ReconNessAgent.Infrastructure.Data.EF/ReconNessAgent.Infrastructure.Data.EF.csproj", "/src/Infrastructure/Data/ReconNessAgent.Infrastructure.Data.EF/"]
COPY ["/src/Infrastructure/ReconNessAgent.Infrastructure.Worker/ReconNessAgent.Infrastructure.Worker.csproj", "/src/Infrastructure/ReconNessAgent.Infrastructure.Worker/"]
RUN dotnet restore "/src/Infrastructure/ReconNessAgent.Infrastructure.Worker/ReconNessAgent.Infrastructure.Worker.csproj"
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