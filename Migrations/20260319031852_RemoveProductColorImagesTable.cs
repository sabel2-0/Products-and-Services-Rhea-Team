using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAspNetApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductColorImagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop table if it exists to avoid migration failures when the table is missing.
            migrationBuilder.Sql(@"IF OBJECT_ID('dbo.ProductColorImages', 'U') IS NOT NULL
    DROP TABLE [dbo].[ProductColorImages];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductColorImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductColorImages", x => x.Id);
                });
        }
    }
}
