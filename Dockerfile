FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["src/BuildingBlocks/RestaurantMenu.Application.Abstractions/RestaurantMenu.Application.Abstractions.csproj", "src/BuildingBlocks/RestaurantMenu.Application.Abstractions/"]
COPY ["src/BuildingBlocks/RestaurantMenu.Presentation.Abstractions/RestaurantMenu.Presentation.Abstractions.csproj", "src/BuildingBlocks/RestaurantMenu.Presentation.Abstractions/"]
COPY ["src/BuildingBlocks/RestaurantMenu.SharedKernel/RestaurantMenu.SharedKernel.csproj", "src/BuildingBlocks/RestaurantMenu.SharedKernel/"]
COPY ["src/Modules/Restaurants/RestaurantMenu.Restaurants.Domain/RestaurantMenu.Restaurants.Domain.csproj", "src/Modules/Restaurants/RestaurantMenu.Restaurants.Domain/"]
COPY ["src/Modules/Restaurants/RestaurantMenu.Restaurants.Application/RestaurantMenu.Restaurants.Application.csproj", "src/Modules/Restaurants/RestaurantMenu.Restaurants.Application/"]
COPY ["src/Modules/Restaurants/RestaurantMenu.Restaurants.Infrastructure/RestaurantMenu.Restaurants.Infrastructure.csproj", "src/Modules/Restaurants/RestaurantMenu.Restaurants.Infrastructure/"]
COPY ["src/Modules/Restaurants/RestaurantMenu.Restaurants.Presentation/RestaurantMenu.Restaurants.Presentation.csproj", "src/Modules/Restaurants/RestaurantMenu.Restaurants.Presentation/"]
COPY ["src/Modules/Catalog/RestaurantMenu.Catalog.Domain/RestaurantMenu.Catalog.Domain.csproj", "src/Modules/Catalog/RestaurantMenu.Catalog.Domain/"]
COPY ["src/Modules/Catalog/RestaurantMenu.Catalog.Application/RestaurantMenu.Catalog.Application.csproj", "src/Modules/Catalog/RestaurantMenu.Catalog.Application/"]
COPY ["src/Modules/Catalog/RestaurantMenu.Catalog.Infrastructure/RestaurantMenu.Catalog.Infrastructure.csproj", "src/Modules/Catalog/RestaurantMenu.Catalog.Infrastructure/"]
COPY ["src/Modules/Catalog/RestaurantMenu.Catalog.Presentation/RestaurantMenu.Catalog.Presentation.csproj", "src/Modules/Catalog/RestaurantMenu.Catalog.Presentation/"]
COPY ["src/Modules/Media/RestaurantMenu.Media.Domain/RestaurantMenu.Media.Domain.csproj", "src/Modules/Media/RestaurantMenu.Media.Domain/"]
COPY ["src/Modules/Media/RestaurantMenu.Media.Application/RestaurantMenu.Media.Application.csproj", "src/Modules/Media/RestaurantMenu.Media.Application/"]
COPY ["src/Modules/Media/RestaurantMenu.Media.Infrastructure/RestaurantMenu.Media.Infrastructure.csproj", "src/Modules/Media/RestaurantMenu.Media.Infrastructure/"]
COPY ["src/Modules/Media/RestaurantMenu.Media.Presentation/RestaurantMenu.Media.Presentation.csproj", "src/Modules/Media/RestaurantMenu.Media.Presentation/"]
COPY ["src/Modules/Ordering/RestaurantMenu.Ordering.Domain/RestaurantMenu.Ordering.Domain.csproj", "src/Modules/Ordering/RestaurantMenu.Ordering.Domain/"]
COPY ["src/Modules/Ordering/RestaurantMenu.Ordering.Application/RestaurantMenu.Ordering.Application.csproj", "src/Modules/Ordering/RestaurantMenu.Ordering.Application/"]
COPY ["src/Modules/Ordering/RestaurantMenu.Ordering.Infrastructure/RestaurantMenu.Ordering.Infrastructure.csproj", "src/Modules/Ordering/RestaurantMenu.Ordering.Infrastructure/"]
COPY ["src/Modules/Ordering/RestaurantMenu.Ordering.Presentation/RestaurantMenu.Ordering.Presentation.csproj", "src/Modules/Ordering/RestaurantMenu.Ordering.Presentation/"]
COPY ["src/RestaurantMenu.Api/RestaurantMenu.Api.csproj", "src/RestaurantMenu.Api/"]

RUN dotnet restore "src/RestaurantMenu.Api/RestaurantMenu.Api.csproj"

COPY . .
RUN dotnet publish "src/RestaurantMenu.Api/RestaurantMenu.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
RUN apk add --no-cache krb5-libs
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RestaurantMenu.Api.dll"]
