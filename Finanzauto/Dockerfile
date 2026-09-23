FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Finanzauto/Finanzauto.csproj", "Finanzauto/"]
COPY ["Finanzauto.Domain/Finanzauto.Domain.csproj", "Finanzauto.Domain/"]
COPY ["Finanzauto.Application/Finanzauto.Application.csproj", "Finanzauto.Application/"]
COPY ["Finanzauto.Infrastructure/Finanzauto.Infrastructure.csproj", "Finanzauto.Infrastructure/"]
RUN dotnet restore "Finanzauto/Finanzauto.csproj"
COPY . .
RUN dotnet publish "Finanzauto/Finanzauto.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Finanzauto.dll"]
