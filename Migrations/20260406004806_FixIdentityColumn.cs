using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeHouseAdmin.Migrations
{
    public partial class FixIdentityColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. THÁO CÁC MỐI QUAN HỆ ĐANG RÀNG BUỘC (Tránh lỗi Foreign Key)
            // migrationBuilder.DropForeignKey(
            //     name: "FK_TableBookings_CoffeeTables_CoffeeTableID",
            //     table: "TableBookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CoffeeTables",
                table: "CoffeeTables");

            // 2. PHẪU THUẬT BẢNG TABLEBOOKINGS (Đổi CoffeeTableID sang string)
            migrationBuilder.DropColumn(
                name: "CoffeeTableID",
                table: "TableBookings");

            migrationBuilder.AddColumn<string>(
                name: "CoffeeTableID",
                table: "TableBookings",
                type: "nvarchar(450)",
                nullable: true);

            // 3. PHẪU THUẬT BẢNG COFFEETABLES (Đổi TableID sang string & Thêm Location)
            migrationBuilder.DropColumn(
                name: "TableID",
                table: "CoffeeTables");

            migrationBuilder.AddColumn<string>(
                name: "TableID",
                table: "CoffeeTables",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CoffeeTables",
                type: "nvarchar(max)",
                nullable: true);

            // Đặt lại Khóa chính (Primary Key) cho bảng bàn
            migrationBuilder.AddPrimaryKey(
                name: "PK_CoffeeTables",
                table: "CoffeeTables",
                column: "TableID");

            // 4. CẬP NHẬT CÁC CỘT KHÁC TRONG TABLEBOOKINGS (Dỗ lỗi Warning)
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TableBookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Chờ xác nhận",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "TableBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // 5. THIẾT LẬP LẠI MỐI QUAN HỆ (Foreign Key & Index)
            migrationBuilder.CreateIndex(
                name: "IX_TableBookings_CoffeeTableID",
                table: "TableBookings",
                column: "CoffeeTableID");

            migrationBuilder.AddForeignKey(
                name: "FK_TableBookings_CoffeeTables_CoffeeTableID",
                table: "TableBookings",
                column: "CoffeeTableID",
                principalTable: "CoffeeTables",
                principalColumn: "TableID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khi Down, chúng mình xóa cái string đi và tạo lại cái int (Nếu cần quay lại)
            migrationBuilder.DropForeignKey(name: "FK_TableBookings_CoffeeTables_CoffeeTableID", table: "TableBookings");
            migrationBuilder.DropPrimaryKey(name: "PK_CoffeeTables", table: "CoffeeTables");

            migrationBuilder.DropColumn(name: "CoffeeTableID", table: "TableBookings");
            migrationBuilder.AddColumn<int>(name: "CoffeeTableID", table: "TableBookings", type: "int", nullable: false);

            migrationBuilder.DropColumn(name: "TableID", table: "CoffeeTables");
            migrationBuilder.AddColumn<int>(name: "TableID", table: "CoffeeTables", type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(name: "PK_CoffeeTables", table: "CoffeeTables", column: "TableID");
        }
    }
}