using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Popug.Task.Migrations
{
    /// <inheritdoc />
    public partial class TaskIsDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Tasks",
                newName: "IsDone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDone",
                table: "Tasks",
                newName: "IsActive");
        }
    }
}
