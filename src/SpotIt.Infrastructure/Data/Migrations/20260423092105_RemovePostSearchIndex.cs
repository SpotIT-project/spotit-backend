using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotIt.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePostSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_posts_title_description",
                table: "posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_posts_title_description",
                table: "posts",
                columns: new[] { "title", "description" })
                .Annotation("Npgsql:IndexMethod", "GIN");
        }
    }
}
