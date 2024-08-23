FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./

COPY ["MarketplaceApi/MarketplaceApi.csproj", "MarketplaceApi/"]
COPY ["DataLibrary/DataLibrary.csproj", "DataLibrary/"]

# Restore as distinct layers
RUN dotnet restore
RUN dotnet tool restore

WORKDIR /App/MarketplaceApi
# Build and publish a release
RUN dotnet publish -c Release -o ../out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "MarketplaceApi.dll"]

RUN apt-get update \
&& apt-get install -y --no-install-recommends libfontconfig1 \
&& rm -rf /var/lib/apt/lists/*