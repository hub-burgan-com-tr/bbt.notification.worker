using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bbt.notification.worker.Migrations
{
    public partial class AddBlogCreatedLogTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogId = table.Column<int>(type: "int", nullable: false),
                    ResponseData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "SourceServices",
                keyColumn: "Id",
                keyValue: 1,
                column: "ServiceUrl",
                value: "X");

            migrationBuilder.UpdateData(
                table: "Sources",
                keyColumn: "Id",
                keyValue: 1,
                column: "KafkaUrl",
                value: "x");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogDetails");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.UpdateData(
                table: "SourceServices",
                keyColumn: "Id",
                keyValue: 1,
                column: "ServiceUrl",
                value: "http://notification-enrichment.test-notification-enrichment.svc:5000/GetTelephoneNumber");

            migrationBuilder.UpdateData(
                table: "Sources",
                keyColumn: "Id",
                keyValue: 1,
                column: "KafkaUrl",
                value: "cluster-kafka-tls-bootstrap-test-kafka4cdc.apps.nonprod.ebt.bank:443");
        }
    }
}
