using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAPP.Migrations
{
    public partial class wishlist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_Products_ProductIdId",
                table: "Wishlists");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_UserModel_UserIdId",
                table: "Wishlists");

            migrationBuilder.RenameColumn(
                name: "UserIdId",
                table: "Wishlists",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ProductIdId",
                table: "Wishlists",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlists_UserIdId",
                table: "Wishlists",
                newName: "IX_Wishlists_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlists_ProductIdId",
                table: "Wishlists",
                newName: "IX_Wishlists_ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_Products_ProductId",
                table: "Wishlists",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_UserModel_UserId",
                table: "Wishlists",
                column: "UserId",
                principalTable: "UserModel",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_Products_ProductId",
                table: "Wishlists");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_UserModel_UserId",
                table: "Wishlists");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Wishlists",
                newName: "UserIdId");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Wishlists",
                newName: "ProductIdId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlists_UserId",
                table: "Wishlists",
                newName: "IX_Wishlists_UserIdId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlists_ProductId",
                table: "Wishlists",
                newName: "IX_Wishlists_ProductIdId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_Products_ProductIdId",
                table: "Wishlists",
                column: "ProductIdId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_UserModel_UserIdId",
                table: "Wishlists",
                column: "UserIdId",
                principalTable: "UserModel",
                principalColumn: "Id");
        }
    }
}
