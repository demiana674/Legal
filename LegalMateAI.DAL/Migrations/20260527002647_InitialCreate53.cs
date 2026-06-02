using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalMateAI.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate53 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LawyerSpecializations_LegalSpecializations_SpecializationId",
                table: "LawyerSpecializations");

            migrationBuilder.AddForeignKey(
                name: "FK_LawyerSpecializations_LawyerSpecialties_SpecializationId",
                table: "LawyerSpecializations",
                column: "SpecializationId",
                principalTable: "LawyerSpecialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LawyerSpecializations_LawyerSpecialties_SpecializationId",
                table: "LawyerSpecializations");

            migrationBuilder.AddForeignKey(
                name: "FK_LawyerSpecializations_LegalSpecializations_SpecializationId",
                table: "LawyerSpecializations",
                column: "SpecializationId",
                principalTable: "LegalSpecializations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
