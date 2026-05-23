using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionInstagramService.Database.Migrations
{
    /// <inheritdoc />
    public partial class Flatten : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_AuctionItems_AuctionItemId",
                table: "Bids");

            migrationBuilder.DropTable(
                name: "AuctionItems");

            migrationBuilder.RenameColumn(
                name: "AuctionItemId",
                table: "Bids",
                newName: "AuctionId");

            migrationBuilder.RenameIndex(
                name: "IX_Bids_AuctionItemId",
                table: "Bids",
                newName: "IX_Bids_AuctionId");

            migrationBuilder.AddColumn<decimal>(
                name: "StartingPrice",
                table: "Auctions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AuctionImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionImages_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionImages_AuctionId",
                table: "AuctionImages",
                column: "AuctionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Auctions_AuctionId",
                table: "Bids",
                column: "AuctionId",
                principalTable: "Auctions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Auctions_AuctionId",
                table: "Bids");

            migrationBuilder.DropTable(
                name: "AuctionImages");

            migrationBuilder.DropColumn(
                name: "StartingPrice",
                table: "Auctions");

            migrationBuilder.RenameColumn(
                name: "AuctionId",
                table: "Bids",
                newName: "AuctionItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Bids_AuctionId",
                table: "Bids",
                newName: "IX_Bids_AuctionItemId");

            migrationBuilder.CreateTable(
                name: "AuctionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartingPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionItems_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionItems_AuctionId",
                table: "AuctionItems",
                column: "AuctionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_AuctionItems_AuctionItemId",
                table: "Bids",
                column: "AuctionItemId",
                principalTable: "AuctionItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
