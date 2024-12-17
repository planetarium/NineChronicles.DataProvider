using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class MergeAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("Update Raiders SET Address = CONCAT('0x', Address) WHERE Address NOT LIKE '0x%'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("Update Raiders SET Address = REPLACE(Address, '0x', '')");
        }
    }
}
