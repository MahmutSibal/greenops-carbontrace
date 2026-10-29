using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetTokenColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PasswordResetToken & PasswordResetTokenExpiry — idempotent
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users','PasswordResetToken') IS NULL
                    ALTER TABLE [Users] ADD [PasswordResetToken] nvarchar(max) NULL;
                IF COL_LENGTH('Users','PasswordResetTokenExpiry') IS NULL
                    ALTER TABLE [Users] ADD [PasswordResetTokenExpiry] datetimeoffset NULL;
                IF COL_LENGTH('Licenses','CreatedAt') IS NULL
                    ALTER TABLE [Licenses] ADD [CreatedAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
                IF COL_LENGTH('Licenses','MaxFacilities') IS NULL
                    ALTER TABLE [Licenses] ADD [MaxFacilities] int NOT NULL DEFAULT 0;
                IF COL_LENGTH('Licenses','MaxUsers') IS NULL
                    ALTER TABLE [Licenses] ADD [MaxUsers] int NOT NULL DEFAULT 0;
                IF COL_LENGTH('Licenses','PlanName') IS NULL
                    ALTER TABLE [Licenses] ADD [PlanName] nvarchar(max) NOT NULL DEFAULT '';
                IF COL_LENGTH('EmissionEntries','IsDeleted') IS NULL
                    ALTER TABLE [EmissionEntries] ADD [IsDeleted] bit NOT NULL DEFAULT 0;
                IF COL_LENGTH('AuditLogs','CompanyId') IS NULL
                    ALTER TABLE [AuditLogs] ADD [CompanyId] uniqueidentifier NULL;
            ");

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6358), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6368), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6375), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 786, DateTimeKind.Unspecified).AddTicks(6380), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1970), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1977), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1983), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 5, 0, 50, 59, 789, DateTimeKind.Unspecified).AddTicks(1989), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "MaxFacilities",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "PlanName",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmissionEntries");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AuditLogs");

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8048), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8054), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8059), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8062), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000001"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8104), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000002"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8109), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000003"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8112), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000004"),
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 3, 1, 7, 7, 1, 880, DateTimeKind.Unspecified).AddTicks(8115), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
