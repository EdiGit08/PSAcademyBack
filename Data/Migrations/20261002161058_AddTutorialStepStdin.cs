using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSAcademyBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorialStepStdin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable a proposito: null significa "este paso hereda los valores del
            // leer del ejercicio", que es lo que necesitan los pasos que no usan Leer.
            // Nullable y no "text vacio" para que un paso con entrada vacia de verdad
            // (un programa que no lee nada) se distinga de uno que aun no se ha
            // configurado, y para no tener que rellenar los 20 pasos ya publicados.
            migrationBuilder.AddColumn<string>(
                name: "stdin",
                table: "tutorial_steps",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stdin",
                table: "tutorial_steps");
        }
    }
}
