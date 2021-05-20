namespace NineChronicles.DataProvider.Executable.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Address = table.Column<string>(type: "varchar(767)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Address);
                });

            migrationBuilder.CreateTable(
                name: "Avatars",
                columns: table => new
                {
                    Address = table.Column<string>(type: "varchar(767)", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(767)", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avatars", x => x.Address);
                    table.ForeignKey(
                        name: "FK_Avatars_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HackAndSlashes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(767)", nullable: false),
                    AgentAddress = table.Column<string>(type: "varchar(767)", nullable: true),
                    AvatarAddress = table.Column<string>(type: "varchar(767)", nullable: true),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    Cleared = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mimisbrunnr = table.Column<bool>(type: "tinyint(1)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HackAndSlashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HackAndSlashes_Agents_AgentAddress",
                        column: x => x.AgentAddress,
                        principalTable: "Agents",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HackAndSlashes_Avatars_AvatarAddress",
                        column: x => x.AvatarAddress,
                        principalTable: "Avatars",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avatars_AgentAddress",
                table: "Avatars",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashes_AgentAddress",
                table: "HackAndSlashes",
                column: "AgentAddress");

            migrationBuilder.CreateIndex(
                name: "IX_HackAndSlashes_AvatarAddress",
                table: "HackAndSlashes",
                column: "AvatarAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HackAndSlashes");

            migrationBuilder.DropTable(
                name: "Avatars");

            migrationBuilder.DropTable(
                name: "Agents");
        }
    }
}
