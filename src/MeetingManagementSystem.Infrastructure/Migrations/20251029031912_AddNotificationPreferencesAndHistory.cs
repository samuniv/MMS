using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MeetingManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferencesAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    DeliveryMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDelivered = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RelatedMeetingId = table.Column<int>(type: "integer", nullable: true),
                    RelatedActionItemId = table.Column<int>(type: "integer", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationHistories_ActionItems_RelatedActionItemId",
                        column: x => x.RelatedActionItemId,
                        principalTable: "ActionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotificationHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationHistories_Meetings_RelatedMeetingId",
                        column: x => x.RelatedMeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MeetingInvitations = table.Column<bool>(type: "boolean", nullable: false),
                    MeetingReminders = table.Column<bool>(type: "boolean", nullable: false),
                    MeetingUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    MeetingCancellations = table.Column<bool>(type: "boolean", nullable: false),
                    ActionItemAssignments = table.Column<bool>(type: "boolean", nullable: false),
                    ActionItemReminders = table.Column<bool>(type: "boolean", nullable: false),
                    ActionItemUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    EmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    SystemNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    Reminder24Hours = table.Column<bool>(type: "boolean", nullable: false),
                    Reminder1Hour = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistories_RelatedActionItemId",
                table: "NotificationHistories",
                column: "RelatedActionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistories_RelatedMeetingId",
                table: "NotificationHistories",
                column: "RelatedMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_SentAt",
                table: "NotificationHistories",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_User",
                table: "NotificationHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_UserDelivery",
                table: "NotificationHistories",
                columns: new[] { "UserId", "IsDelivered" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreference_User",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationHistories");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");
        }
    }
}
