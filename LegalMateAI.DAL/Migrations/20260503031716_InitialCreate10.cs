using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalMateAI.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLogs_Admins_AdminId",
                table: "AdminLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "AdminId",
                table: "AdminLogs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ActorId",
                table: "AdminLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "AdminLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "AdminLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLogs_Admins_AdminId",
                table: "AdminLogs",
                column: "AdminId",
                principalTable: "Admins",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLogs_Admins_AdminId",
                table: "AdminLogs");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "AdminLogs");

            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "AdminLogs");

            migrationBuilder.DropColumn(
                name: "ActorRole",
                table: "AdminLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "AdminId",
                table: "AdminLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLogs_Admins_AdminId",
                table: "AdminLogs",
                column: "AdminId",
                principalTable: "Admins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
