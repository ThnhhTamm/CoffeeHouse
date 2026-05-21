# 1. Khởi tạo môi trường chạy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# 2. Khởi tạo môi trường build code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file giải pháp .sln và tìm nạp tất cả các file .csproj trong folder con vào
COPY ["CoffeeHouseAdmin.sln", "."]
COPY ["CoffeeHouseAdmin.csproj", "CoffeeHouseAdmin/"] 
# (Nếu file .csproj của Boss viết thường, nhớ sửa chữ trên thành coffeehouseadmin.csproj nha)

RUN dotnet restore

# Sao chép toàn bộ code còn lại và tiến hành build
COPY . .
RUN dotnet build -c Release -o /app/build

# 3. Xuất bản file hệ thống
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 4. Kích hoạt Server chạy online
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoffeeHouseAdmin.dll"]