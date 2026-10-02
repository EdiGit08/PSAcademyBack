using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PSAcademyBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorialSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tutorial_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    task = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    code_snippet = table.Column<string>(type: "text", nullable: false),
                    expected_output = table.Column<string>(type: "text", nullable: false),
                    tip = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tutorial_steps", x => x.id);
                    table.ForeignKey(
                        name: "FK_tutorial_steps_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tutorial_steps_exercise_id_order_index",
                table: "tutorial_steps",
                columns: new[] { "exercise_id", "order_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tutorial_steps");
        }
    }
}
