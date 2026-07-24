using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRDocs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAccountKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountKind",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "InternalOffice");

            migrationBuilder.AddColumn<string>(
                name: "ExternalOrganizationName",
                table: "Users",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountKind",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalOrganizationName",
                table: "Users");
        }
    }
}
