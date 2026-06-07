using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Banking_System.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullableProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_CitizenId",
                table: "CustomerProfiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "CustomerProfiles",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CitizenId",
                table: "CustomerProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CustomerProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_CitizenId",
                table: "CustomerProfiles",
                column: "CitizenId",
                unique: true,
                filter: "[CitizenId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_CitizenId",
                table: "CustomerProfiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "CustomerProfiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CitizenId",
                table: "CustomerProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CustomerProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_CitizenId",
                table: "CustomerProfiles",
                column: "CitizenId",
                unique: true);
        }
    }
}
