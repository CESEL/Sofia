FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Sophia/Sophia.csproj", "Sophia/"]
RUN dotnet restore "Sophia/Sophia.csproj"
COPY . .
WORKDIR "/src/Sophia"
RUN dotnet build "Sophia.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Sophia.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Sophia.dll"]
