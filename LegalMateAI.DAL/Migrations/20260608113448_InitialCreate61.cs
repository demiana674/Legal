using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalMateAI.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate61 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "LawyerSpecializations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "LawyerSpecializations");
        }
    }
}
