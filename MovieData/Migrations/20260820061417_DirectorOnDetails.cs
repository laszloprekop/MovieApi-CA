using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieData.Migrations
{
    /// <inheritdoc />
    public partial class DirectorOnDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Director",
                table: "MovieDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Director",
                table: "MovieDetails");
        }
    }
}
