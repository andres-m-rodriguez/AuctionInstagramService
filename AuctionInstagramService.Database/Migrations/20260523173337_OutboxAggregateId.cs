using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionInstagramService.Database.Migrations
{
    /// <inheritdoc />
    public partial class OutboxAggregateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "OutboxEvents");

            migrationBuilder.AddColumn<Guid>(
                name: "AggregateId",
                table: "OutboxEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AggregateId",
                table: "OutboxEvents");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "OutboxEvents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
