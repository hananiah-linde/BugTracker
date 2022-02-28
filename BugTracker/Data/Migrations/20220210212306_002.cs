using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTracker.Data.Migrations
{
    public partial class _002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_Tickets_Ticketid",
                table: "TicketAttachments");

            migrationBuilder.RenameColumn(
                name: "Ticketid",
                table: "TicketAttachments",
                newName: "TicketId");

            migrationBuilder.RenameIndex(
                name: "IX_TicketAttachments_Ticketid",
                table: "TicketAttachments",
                newName: "IX_TicketAttachments_TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_Tickets_TicketId",
                table: "TicketAttachments",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_Tickets_TicketId",
                table: "TicketAttachments");

            migrationBuilder.RenameColumn(
                name: "TicketId",
                table: "TicketAttachments",
                newName: "Ticketid");

            migrationBuilder.RenameIndex(
                name: "IX_TicketAttachments_TicketId",
                table: "TicketAttachments",
                newName: "IX_TicketAttachments_Ticketid");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_Tickets_Ticketid",
                table: "TicketAttachments",
                column: "Ticketid",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
