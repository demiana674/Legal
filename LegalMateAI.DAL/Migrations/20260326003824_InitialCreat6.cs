using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalMateAI.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreat6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityId",
                table: "LawyerProfiles");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "LawyerProfiles");

            migrationBuilder.CreateTable(
                name: "LawyerSpecialties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LawyerSpecialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LawyerProfileSpecialties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecialtyId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LawyerProfileSpecialties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LawyerProfileSpecialties_LawyerProfiles_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "LawyerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LawyerProfileSpecialties_LawyerSpecialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "LawyerSpecialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LawyerProfileSpecialties_LawyerId_SpecialtyId",
                table: "LawyerProfileSpecialties",
                columns: new[] { "LawyerId", "SpecialtyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LawyerProfileSpecialties_SpecialtyId",
                table: "LawyerProfileSpecialties",
                column: "SpecialtyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LawyerProfileSpecialties");

            migrationBuilder.DropTable(
                name: "LawyerSpecialties");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "LawyerProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "LawyerProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
