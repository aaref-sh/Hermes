using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HStore.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class variantType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VariantOptionType",
                table: "ProductVariantOptions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VariantOptionType",
                table: "ProductVariantOptions");
        }
    }
}
