using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NineChronicles.DataProvider.Executable.Migrations
{
    public partial class AddUniqueIndexAvatarAddressCollectionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_ActivateCollections_Avatars_AvatarAddress",
                table: "ActivateCollections");
            migrationBuilder.DropIndex(
                name: "IX_ActivateCollections_AvatarAddress",
                table: "ActivateCollections");
            migrationBuilder.AlterColumn<int>(
                name: "ActivateCollectionId",
                table: "CollectionOptionModel",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(
                "DELETE t1 from ActivateCollections t1 INNER JOIN ActivateCollections t2 where t1.Id < t2.Id and t1.AvatarAddress = t2.AvatarAddress and t1.CollectionId = t2.CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivateCollections_AvatarAddress_CollectionId",
                table: "ActivateCollections",
                columns: new[] { "AvatarAddress", "CollectionId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivateCollections_AvatarAddress_CollectionId",
                table: "ActivateCollections");
            migrationBuilder.AlterColumn<int>(
                name: "ActivateCollectionId",
                table: "CollectionOptionModel",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivateCollections_Avatars_AvatarAddress",
                table: "ActivateCollections",
                column: "AvatarAddress",
                principalTable: "Avatars",
                principalColumn: "Address",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.CreateIndex(
                name: "IX_ActivateCollections_AvatarAddress",
                table: "ActivateCollections",
                column: "AvatarAddress");
        }
    }
}
