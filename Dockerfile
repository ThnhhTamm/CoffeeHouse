# 1. Khởi tạo môi trường chạy ứng dụng .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 2. Khởi tạo môi trường build code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CoffeeHouseAdmin.csproj", "."]
RUN dotnet restore "./CoffeeHouseAdmin.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "CoffeeHouseAdmin.csproj" -c Release -o /app/build

# 3. Xuất bản file .dll hệ thống
FROM build AS publish
RUN dotnet publish "CoffeeHouseAdmin.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Chạy ứng dụng trên môi trường ảo
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoffeeHouseAdmin.dll"]