using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CRM.Migrations
{
    /// <inheritdoc />
    public partial class clientprofilecolumnsaddition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientsAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "ClientAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Mt5LoginID = table.Column<int>(type: "integer", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    CreditBalance = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CountryOfResidence = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    MarketingConsent = table.Column<bool>(type: "boolean", nullable: false),
                    EmploymentStatus = table.Column<string>(type: "text", nullable: true),
                    AnnualIncome = table.Column<string>(type: "text", nullable: true),
                    PrimarySourceOfTradingFund = table.Column<string>(type: "text", nullable: true),
                    TradingObjective = table.Column<string>(type: "text", nullable: true),
                    DegreeOfRisk = table.Column<string>(type: "text", nullable: true),
                    YearsOfTradingExperience = table.Column<int>(type: "integer", nullable: false),
                    ConfirmTradingKnowledge = table.Column<bool>(type: "boolean", nullable: false),
                    BuildingNumber = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Nationality = table.Column<string>(type: "text", nullable: true),
                    PlaceOfBirth = table.Column<string>(type: "text", nullable: true),
                    PassportUrl = table.Column<string>(type: "text", nullable: true),
                    ProofofAddressUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientProfiles", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientAccounts");

            migrationBuilder.DropTable(
                name: "ClientProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "ClientsAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Mt5LoginID = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientsAccounts", x => x.Id);
                });
        }
    }
}
