using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalMateAI.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate32 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseDimensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    Complexity = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDimensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationDimensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GovernorateId = table.Column<int>(type: "int", nullable: false),
                    GovernorateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    CityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUrban = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationDimensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendedLawyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SearchContext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectedSpecialization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultsCount = table.Column<int>(type: "int", nullable: false),
                    WasSelected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeDimensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Quarter = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Week = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsWeekend = table.Column<bool>(type: "bit", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeDimensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDimensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgeGroup = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountAgeDays = table.Column<int>(type: "int", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    TotalInteractions = table.Column<int>(type: "int", nullable: false),
                    LifetimeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDimensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataWarehouseFacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeDimId = table.Column<int>(type: "int", nullable: false),
                    UserDimId = table.Column<int>(type: "int", nullable: false),
                    LawyerDimId = table.Column<int>(type: "int", nullable: false),
                    CaseDimId = table.Column<int>(type: "int", nullable: false),
                    ContractDimId = table.Column<int>(type: "int", nullable: false),
                    DocumentDimId = table.Column<int>(type: "int", nullable: false),
                    LocationDimId = table.Column<int>(type: "int", nullable: false),
                    CaseCount = table.Column<int>(type: "int", nullable: false),
                    ContractCount = table.Column<int>(type: "int", nullable: false),
                    DocumentCount = table.Column<int>(type: "int", nullable: false),
                    AppointmentCount = table.Column<int>(type: "int", nullable: false),
                    TotalFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    SuccessRate = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    UserSatisfactionScore = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataWarehouseFacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataWarehouseFacts_CaseDimensions_CaseDimId",
                        column: x => x.CaseDimId,
                        principalTable: "CaseDimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataWarehouseFacts_LocationDimensions_LocationDimId",
                        column: x => x.LocationDimId,
                        principalTable: "LocationDimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataWarehouseFacts_TimeDimensions_TimeDimId",
                        column: x => x.TimeDimId,
                        principalTable: "TimeDimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataWarehouseFacts_UserDimensions_UserDimId",
                        column: x => x.UserDimId,
                        principalTable: "UserDimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataWarehouseFacts_CaseDimId",
                table: "DataWarehouseFacts",
                column: "CaseDimId");

            migrationBuilder.CreateIndex(
                name: "IX_DataWarehouseFacts_LocationDimId",
                table: "DataWarehouseFacts",
                column: "LocationDimId");

            migrationBuilder.CreateIndex(
                name: "IX_DataWarehouseFacts_TimeDimId_CaseDimId_LocationDimId",
                table: "DataWarehouseFacts",
                columns: new[] { "TimeDimId", "CaseDimId", "LocationDimId" });

            migrationBuilder.CreateIndex(
                name: "IX_DataWarehouseFacts_UserDimId",
                table: "DataWarehouseFacts",
                column: "UserDimId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationLogs_CreatedAt",
                table: "RecommendationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationLogs_UserId",
                table: "RecommendationLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataWarehouseFacts");

            migrationBuilder.DropTable(
                name: "RecommendationLogs");

            migrationBuilder.DropTable(
                name: "CaseDimensions");

            migrationBuilder.DropTable(
                name: "LocationDimensions");

            migrationBuilder.DropTable(
                name: "TimeDimensions");

            migrationBuilder.DropTable(
                name: "UserDimensions");
        }
    }
}
