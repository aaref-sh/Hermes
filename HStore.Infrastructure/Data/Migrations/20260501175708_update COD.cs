using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HStore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateCOD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodCollectionDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CodFee",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCodCollected",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodCollectionDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CodFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsCodCollected",
                table: "Orders");
        }
    }
}
