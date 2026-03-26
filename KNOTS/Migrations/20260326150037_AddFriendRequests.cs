using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KNOTS.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "friend_request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequesterUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReceiverUsername = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    PairKey = table.Column<string>(type: "TEXT", maxLength: 101, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_request", x => x.Id);
                    table.CheckConstraint("CK_friend_request_RequesterNotReceiver", "lower(RequesterUsername) <> lower(ReceiverUsername)");
                    table.ForeignKey(
                        name: "FK_friend_request_Users_ReceiverUsername",
                        column: x => x.ReceiverUsername,
                        principalTable: "Users",
                        principalColumn: "Username",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friend_request_Users_RequesterUsername",
                        column: x => x.RequesterUsername,
                        principalTable: "Users",
                        principalColumn: "Username",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_friend_request_PairKey",
                table: "friend_request",
                column: "PairKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_friend_request_ReceiverUsername_Status_CreatedAt",
                table: "friend_request",
                columns: new[] { "ReceiverUsername", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_friend_request_RequesterUsername_Status",
                table: "friend_request",
                columns: new[] { "RequesterUsername", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "friend_request");
        }
    }
}
