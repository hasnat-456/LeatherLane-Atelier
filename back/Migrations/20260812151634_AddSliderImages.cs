using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeatherLane_Atelier.Migrations
{
    /// <inheritdoc />
    public partial class AddSliderImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CraftSliderImages",
                table: "SiteSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroSliderImages",
                table: "SiteSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CraftSliderImages",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroSliderImages",
                table: "SiteSettings");
        }
    }
}
