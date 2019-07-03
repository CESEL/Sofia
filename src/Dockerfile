FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Sofia/Sofia.csproj", "Sofia/"]
RUN dotnet restore "Sofia/Sofia.csproj"
COPY . .
WORKDIR "/src/Sofia"
RUN dotnet build "Sofia.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Sofia.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Sofia.dll"]