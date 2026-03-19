using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAspNetApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeGuideJsonToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SizeGuideJson",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeGuideJson",
                table: "Products");
        }
    }
}
