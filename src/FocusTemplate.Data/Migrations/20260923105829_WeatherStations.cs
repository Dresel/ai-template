using System;
using FocusTemplate.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FocusTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeatherStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherForecasts",
                table: "WeatherForecasts");

            migrationBuilder.RenameTable(
                name: "WeatherForecasts",
                newName: "weather_forecasts");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "weather_forecasts",
                newName: "summary");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "weather_forecasts",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "weather_forecasts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TemperatureC",
                table: "weather_forecasts",
                newName: "temperature_c");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:alert_kind", "temperature_above_maximum,temperature_below_minimum")
                .Annotation("Npgsql:Enum:station_status", "active,maintenance,retired")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "summary",
                table: "weather_forecasts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "station_id",
                table: "weather_forecasts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "pk_weather_forecasts",
                table: "weather_forecasts",
                column: "id");

            migrationBuilder.CreateTable(
                name: "stations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    location = table.Column<Point>(type: "geography (point)", nullable: false),
                    status = table.Column<StationStatus>(type: "station_status", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observation_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<AlertKind>(type: "alert_kind", nullable: false),
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
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "observations",
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
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_weather_forecasts_station_id_date",
                table: "weather_forecasts",
                columns: new[] { "station_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_station_id_resolved_at",
                table: "alerts",
                columns: new[] { "station_id", "resolved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_observations_station_id_measured_at",
                table: "observations",
                columns: new[] { "station_id", "measured_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_stations_owner_id_code",
                table: "stations",
                columns: new[] { "owner_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_weather_forecasts_stations_station_id",
                table: "weather_forecasts",
                column: "station_id",
                principalTable: "stations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_weather_forecasts_stations_station_id",
                table: "weather_forecasts");

            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "observations");

            migrationBuilder.DropTable(
                name: "stations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_weather_forecasts",
                table: "weather_forecasts");

            migrationBuilder.DropIndex(
                name: "ix_weather_forecasts_station_id_date",
                table: "weather_forecasts");

            migrationBuilder.DropColumn(
                name: "station_id",
                table: "weather_forecasts");

            migrationBuilder.RenameTable(
                name: "weather_forecasts",
                newName: "WeatherForecasts");

            migrationBuilder.RenameColumn(
                name: "summary",
                table: "WeatherForecasts",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "WeatherForecasts",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "WeatherForecasts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "temperature_c",
                table: "WeatherForecasts",
                newName: "TemperatureC");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:alert_kind", "temperature_above_maximum,temperature_below_minimum")
                .OldAnnotation("Npgsql:Enum:station_status", "active,maintenance,retired")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "WeatherForecasts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherForecasts",
                table: "WeatherForecasts",
                column: "Id");
        }
    }
}
