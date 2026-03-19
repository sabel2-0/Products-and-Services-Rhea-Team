using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAspNetApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeGuideTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SizeGuides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FitTips = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HowToMeasure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeGuides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizeGuides_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SizeGuideImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SizeGuideId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeGuideImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizeGuideImages_SizeGuides_SizeGuideId",
                        column: x => x.SizeGuideId,
                        principalTable: "SizeGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SizeGuideImages_SizeGuideId",
                table: "SizeGuideImages",
                column: "SizeGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_SizeGuides_ProductId",
                table: "SizeGuides",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SizeGuideImages");

            migrationBuilder.DropTable(
                name: "SizeGuides");
        }
    }
}
