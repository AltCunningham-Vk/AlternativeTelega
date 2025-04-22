using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telega.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpirationDateToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Messages");
        }
    }
}
