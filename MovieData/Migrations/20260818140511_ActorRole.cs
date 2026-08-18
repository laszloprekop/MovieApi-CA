using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieData.Migrations
{
    /// <inheritdoc />
    public partial class ActorRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF scaffolded DropTable + CreateTable here — that throws every
            // existing movie–actor link away. Same shape, new names: renames
            // carry the rows across, and only Role is genuinely new.
            // (The PK and FK constraints keep their old ActorMovie names in
            // the database; neither SQL Server nor EF reads a constraint by
            // name at runtime.)
            migrationBuilder.RenameTable(
                name: "ActorMovie",
                newName: "MovieActor");

            migrationBuilder.RenameColumn(
                name: "ActorsId",
                table: "MovieActor",
                newName: "ActorId");

            migrationBuilder.RenameColumn(
                name: "MoviesId",
                table: "MovieActor",
                newName: "MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_ActorMovie_MoviesId",
                table: "MovieActor",
                newName: "IX_MovieActor_MovieId");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "MovieActor",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "MovieActor");

            migrationBuilder.RenameIndex(
                name: "IX_MovieActor_MovieId",
                table: "MovieActor",
                newName: "IX_ActorMovie_MoviesId");

            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "MovieActor",
                newName: "MoviesId");

            migrationBuilder.RenameColumn(
                name: "ActorId",
                table: "MovieActor",
                newName: "ActorsId");

            migrationBuilder.RenameTable(
                name: "MovieActor",
                newName: "ActorMovie");
        }
    }
}
