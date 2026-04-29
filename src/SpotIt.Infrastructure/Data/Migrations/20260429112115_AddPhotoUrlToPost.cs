using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotIt.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoUrlToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_url",
                table: "posts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_url",
                table: "posts");
        }
    }
}
