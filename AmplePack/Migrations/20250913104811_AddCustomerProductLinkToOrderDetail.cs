using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmplePack.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerProductLinkToOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerProductId",
                table: "OrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_CustomerProductId",
                table: "OrderDetails",
                column: "CustomerProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_CustomerProducts_CustomerProductId",
                table: "OrderDetails",
                column: "CustomerProductId",
                principalTable: "CustomerProducts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_CustomerProducts_CustomerProductId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_CustomerProductId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "CustomerProductId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "OrderDetails");
        }
    }
}
