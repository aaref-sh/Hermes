using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HStore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePasswordResetTokenFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiration",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiration",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }
    }
}
