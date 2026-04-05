using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace N3DMMarket.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromotionRedemptionOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace int OrderId with GUID OrderId (drop and re-add to avoid type clash)
            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "PromotionRedemptions");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "PromotionRedemptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_OrderId",
                table: "PromotionRedemptions",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionRedemptions_Orders_OrderId",
                table: "PromotionRedemptions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRedemptions_Orders_OrderId",
                table: "PromotionRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRedemptions_OrderId",
                table: "PromotionRedemptions");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "PromotionRedemptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
