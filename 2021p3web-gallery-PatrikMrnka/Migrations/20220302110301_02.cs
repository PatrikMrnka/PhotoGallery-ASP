using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2021p3web_gallery_PatrikMrnka.Migrations
{
    public partial class _02 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ae605389-4f99-42ac-a62e-7abc67bdd298",
                column: "ConcurrencyStamp",
                value: "666c85e6-8c3f-446d-a342-9965037bc22c");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "6540d112-4e61-48da-9236-06d12567613e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "3e9cb7ec-265f-4533-90ca-2fccc1c93812", "AQAAAAEAACcQAAAAEB8nxCQj1bNoCJoxbzdq9/xk1my3VCUnNDO/+BUtjAg1uEg/xBtIR/8s3W0PEZZc1g==" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Files");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ae605389-4f99-42ac-a62e-7abc67bdd298",
                column: "ConcurrencyStamp",
                value: "09ffe261-f5d7-44c1-81ed-24e5735ce603");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "6540d112-4e61-48da-9236-06d12567613e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "8eea89e7-6892-4e1b-8fa5-33a5183201e8", "AQAAAAEAACcQAAAAEHTyblvT/UBw+hS67uZuV1OPu4oLX+dOuZg2YByvjDOs61FSnF/JIVl5QWYrnHWRSQ==" });
        }
    }
}
