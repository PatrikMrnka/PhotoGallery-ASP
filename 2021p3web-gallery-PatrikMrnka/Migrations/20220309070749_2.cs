using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2021p3web_gallery_PatrikMrnka.Migrations
{
    public partial class _2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ae605389-4f99-42ac-a62e-7abc67bdd298",
                column: "ConcurrencyStamp",
                value: "0bba0f70-0ee8-4f1d-b240-a1fb2eb4193b");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "6540d112-4e61-48da-9236-06d12567613e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "a1497dd0-a663-4767-9bc6-c0edcbe8a74c", "AQAAAAEAACcQAAAAEL4nOl3XgwbOkwI3ROune48bo08S1tLEsUc2kGmMP2UY6X8+OvIrqNeOQWukbrryxg==" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ae605389-4f99-42ac-a62e-7abc67bdd298",
                column: "ConcurrencyStamp",
                value: "a6b3a76d-a34b-4b82-8118-73084fe6f455");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "6540d112-4e61-48da-9236-06d12567613e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "fecc9c2a-c6b4-4af2-a03b-f8a327eaefa1", "AQAAAAEAACcQAAAAEKlPCiph9Vjki9CSZh3HSMhqS+oG+9O+2dfwZ9CaOIkNCM7lLvOT++roTM3DPmTwzg==" });
        }
    }
}
