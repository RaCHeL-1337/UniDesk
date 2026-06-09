using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UniDesk.Web.Data;

#nullable disable

namespace UniDesk.Web.Migrations
{
    [DbContext(typeof(UniDeskDbContext))]
    [Migration("20260604013000_AddTicketOwnership")]
    public partial class AddTicketOwnership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByEmail",
                table: "Tickets",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "system@unidesk.local");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Tickets",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: "system");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByEmail",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Tickets");
        }
    }
}
