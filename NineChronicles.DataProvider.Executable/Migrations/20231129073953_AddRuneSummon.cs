using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddRuneSummon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuneSummonFails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    SummonCount = table.Column<int>(type: "int", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    Exception = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuneSummonFails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuneSummonFails_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_RuneSummonFails_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RuneSummons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarAddress = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    SummonCount = table.Column<int>(type: "int", nullable: false),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    SummonResult = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuneSummons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuneSummons_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address");
                    table.ForeignKey(
                        name: "FK_RuneSummons_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RuneSummonFails_AgentAddress",
                table: "RuneSummonFails",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RuneSummonFails_AvatarAddress",
                table: "RuneSummonFails",
                column: "AvatarAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RuneSummons_AgentAddress",
                table: "RuneSummons",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RuneSummons_AvatarAddress",
                table: "RuneSummons",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuneSummonFails");

            migrationBuilder.DropTable(
                name: "RuneSummons");
        }
    }
}
