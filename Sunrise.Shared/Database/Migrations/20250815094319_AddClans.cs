using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Sunrise.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddClans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClanId",
                table: "user",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "ClanPriv",
                table: "user",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "clan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Tag = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clan_user_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_user_ClanId",
                table: "user",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_user_ClanPriv",
                table: "user",
                column: "ClanPriv");

            migrationBuilder.CreateIndex(
                name: "IX_clan_OwnerId",
                table: "clan",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clan");

            migrationBuilder.DropIndex(
                name: "IX_user_ClanId",
                table: "user");

            migrationBuilder.DropIndex(
                name: "IX_user_ClanPriv",
                table: "user");

            migrationBuilder.DropColumn(
                name: "ClanId",
                table: "user");

            migrationBuilder.DropColumn(
                name: "ClanPriv",
                table: "user");
        }
    }
}
