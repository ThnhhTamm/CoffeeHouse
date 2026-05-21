using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeHouseAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        //     migrationBuilder.CreateTable(
        //         name: "Orders",
        //         columns: table => new
        //         {
        //             OrderID = table.Column<string>(type: "nvarchar(450)", nullable: false),
        //             CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //             Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //             Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
        //             Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //             OrderTime = table.Column<DateTime>(type: "datetime2", nullable: false)
        //         },
        //         constraints: table =>
        //         {
        //             table.PrimaryKey("PK_Orders", x => x.OrderID);
        //         });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
