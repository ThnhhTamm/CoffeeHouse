# 1. Khởi tạo môi trường chạy ứng dụng .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 2. Môi trường biên dịch code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy trực tiếp file dự án vào hệ thống máy ảo
COPY CoffeeHouseAdmin.csproj .
RUN dotnet restore CoffeeHouseAdmin.csproj

# Copy toàn bộ đống file còn lại vào và tiến hành build
COPY . .
RUN dotnet build CoffeeHouseAdmin.csproj -c Release -o /app/build

# 3. Xuất bản file hệ thống .dll
FROM build AS publish
RUN dotnet publish CoffeeHouseAdmin.csproj -c Release -o /app/publish /p:UseAppHost=false

# 4. Kích hoạt server online
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoffeeHouseAdmin.dll"]