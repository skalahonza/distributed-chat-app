FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["DSVA.App/DSVA.App.csproj", "DSVA.App/"]
RUN dotnet restore "DSVA.App/DSVA.App.csproj"
COPY . .
WORKDIR "/src/DSVA.App"
RUN dotnet build "DSVA.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DSVA.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DSVA.App.dll"]
