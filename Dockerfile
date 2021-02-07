FROM mcr.microsoft.com/azure-functions/dotnet:3.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["azfunction-dotnet-runps.csproj", "./"]
RUN dotnet restore "azfunction-dotnet-runps.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "azfunction-dotnet-runps.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "azfunction-dotnet-runps.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
