using System;
using FocusTemplate.Weather.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FocusTemplate.Weather.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "weather");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:weather.alert_kind", "temperature_above_maximum,temperature_below_minimum")
                .Annotation("Npgsql:Enum:weather.station_status", "active,maintenance,retired")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "stations",
                schema: "weather",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    location = table.Column<Point>(type: "geography (point)", nullable: false),
                    status = table.Column<StationStatus>(type: "weather.station_status", nullable: false),
                    min_temperature_c = table.Column<int>(type: "integer", nullable: true),
                    max_temperature_c = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stations", x => x.id);
                    table.ForeignKey(
                        name: "fk_stations_user_reference_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                schema: "weather",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observation_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<AlertKind>(type: "weather.alert_kind", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_alerts_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "weather",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "observations",
                schema: "weather",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    measured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    temperature_c = table.Column<double>(type: "double precision", nullable: false),
                    humidity_percent = table.Column<int>(type: "integer", nullable: false),
                    pressure_hpa = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_observations", x => x.id);
                    table.CheckConstraint("CK_observations_humidity_percent_Range", "humidity_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_observations_pressure_hpa_Range", "pressure_hpa BETWEEN 800.0 AND 1200.0");
                    table.CheckConstraint("CK_observations_temperature_c_Range", "temperature_c BETWEEN -100.0 AND 100.0");
                    table.ForeignKey(
                        name: "fk_observations_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "weather",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weather_forecasts",
                schema: "weather",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    summary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    temperature_c = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weather_forecasts", x => x.id);
                    table.ForeignKey(
                        name: "fk_weather_forecasts_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "weather",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_station_id_resolved_at",
                schema: "weather",
                table: "alerts",
                columns: new[] { "station_id", "resolved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_observations_station_id_measured_at",
                schema: "weather",
                table: "observations",
                columns: new[] { "station_id", "measured_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_stations_owner_id_code",
                schema: "weather",
                table: "stations",
                columns: new[] { "owner_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weather_forecasts_station_id_date",
                schema: "weather",
                table: "weather_forecasts",
                columns: new[] { "station_id", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "observations",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "weather_forecasts",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "stations",
                schema: "weather");
        }
    }
}
