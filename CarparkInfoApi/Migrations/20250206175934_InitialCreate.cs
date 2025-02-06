using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarparkInfoApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarParkTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarParkTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingSystems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SystemName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShortTermParkings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortTermParkings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoriteCarParks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CarParkId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteCarParks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarParks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CarParkNo = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    XCoord = table.Column<double>(type: "double precision", nullable: false),
                    YCoord = table.Column<double>(type: "double precision", nullable: false),
                    CarParkTypeId = table.Column<int>(type: "integer", nullable: false),
                    ParkingSystemId = table.Column<int>(type: "integer", nullable: false),
                    ShortTermParkingId = table.Column<int>(type: "integer", nullable: false),
                    FreeParking = table.Column<bool>(type: "boolean", nullable: false),
                    NightParking = table.Column<bool>(type: "boolean", nullable: false),
                    CarParkDecks = table.Column<int>(type: "integer", nullable: false),
                    GantryHeight = table.Column<float>(type: "real", nullable: false),
                    CarParkBasement = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarParks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarParks_CarParkTypes_CarParkTypeId",
                        column: x => x.CarParkTypeId,
                        principalTable: "CarParkTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarParks_ParkingSystems_ParkingSystemId",
                        column: x => x.ParkingSystemId,
                        principalTable: "ParkingSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarParks_ShortTermParkings_ShortTermParkingId",
                        column: x => x.ShortTermParkingId,
                        principalTable: "ShortTermParkings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarParks_CarParkTypeId",
                table: "CarParks",
                column: "CarParkTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CarParks_ParkingSystemId",
                table: "CarParks",
                column: "ParkingSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_CarParks_ShortTermParkingId",
                table: "CarParks",
                column: "ShortTermParkingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarParks");

            migrationBuilder.DropTable(
                name: "UserFavoriteCarParks");

            migrationBuilder.DropTable(
                name: "CarParkTypes");

            migrationBuilder.DropTable(
                name: "ParkingSystems");

            migrationBuilder.DropTable(
                name: "ShortTermParkings");
        }
    }
}
