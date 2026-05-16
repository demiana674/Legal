using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalMateAI.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate37 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Placeholders",
                table: "ContractTemplates",
                newName: "TemplateFilePath");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ContractTemplates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TemplateFilePath",
                table: "ContractTemplates",
                newName: "Placeholders");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ContractTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
