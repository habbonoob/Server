using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UsernamesUniqueIgnoringCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // names are looked up as lower(Username), so that is what must be unique: "Bob" and "bob" were both
            // allowed, and every lookup picked one of them. Two names already alike are left for staff to rename.
            migrationBuilder.Sql("""
                DO $$
                DECLARE alike text;
                BEGIN
                    SELECT string_agg(name, ', ') INTO alike FROM (
                        SELECT string_agg("Username", ' / ') AS name FROM users GROUP BY lower("Username") HAVING count(*) > 1) AS pairs;
                    IF alike IS NOT NULL THEN
                        RAISE EXCEPTION 'Usernames that differ only in case must be renamed first: %', alike;
                    END IF;
                END $$;
                """);
            migrationBuilder.Sql("""CREATE UNIQUE INDEX "IX_users_lower_Username" ON users (lower("Username"));""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_users_lower_Username";""");
        }
    }
}
