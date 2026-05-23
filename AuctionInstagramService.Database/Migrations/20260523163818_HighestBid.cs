using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionInstagramService.Database.Migrations
{
    /// <inheritdoc />
    public partial class HighestBid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentHighestBid",
                table: "Auctions",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentHighestBid",
                table: "Auctions");
        }
    }
}
