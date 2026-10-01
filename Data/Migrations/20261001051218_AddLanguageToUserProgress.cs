using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSAcademyBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageToUserProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_submitted_language_id",
                table: "user_progress",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_progress_last_submitted_language_id",
                table: "user_progress",
                column: "last_submitted_language_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_progress_languages_last_submitted_language_id",
                table: "user_progress",
                column: "last_submitted_language_id",
                principalTable: "languages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_progress_languages_last_submitted_language_id",
                table: "user_progress");

            migrationBuilder.DropIndex(
                name: "IX_user_progress_last_submitted_language_id",
                table: "user_progress");

            migrationBuilder.DropColumn(
                name: "last_submitted_language_id",
                table: "user_progress");
        }
    }
}
