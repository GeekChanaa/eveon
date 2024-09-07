using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class recreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChargingStations",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Network = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ChargerQuantity = table.Column<int>(type: "int", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Organisation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParkingType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WifiAmenity = table.Column<bool>(type: "bit", nullable: false),
                    ParkingAmenity = table.Column<bool>(type: "bit", nullable: false),
                    RestaurantsAmenity = table.Column<bool>(type: "bit", nullable: false),
                    WashroomAmenity = table.Column<bool>(type: "bit", nullable: false),
                    SittingAreaAmenity = table.Column<bool>(type: "bit", nullable: false),
                    PartnerID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargingStations", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iso3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumericCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iso2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phonecode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Capital = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrencyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tld = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Native = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subregion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timezones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Translations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Emoji = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmojiU = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    WikiDataId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MessageLogs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChargePointId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectorId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTypes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ForCustomers = table.Column<bool>(type: "bit", nullable: false),
                    ForAdmins = table.Column<bool>(type: "bit", nullable: false),
                    ForPartners = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Car = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPhoneNumberVerified = table.Column<bool>(type: "bit", nullable: false),
                    PhoneVerificationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    ResetPasswordToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ChargePoints",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargePointId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChargingStationID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Make = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientCertThumb = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePoints", x => x.ID);
                    table.UniqueConstraint("AK_ChargePoints_ChargePointId", x => x.ChargePointId);
                    table.ForeignKey(
                        name: "FK_ChargePoints_ChargingStations_ChargingStationID",
                        column: x => x.ChargingStationID,
                        principalTable: "ChargingStations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryID = table.Column<int>(type: "int", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FipsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iso2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    WikiDataId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.ID);
                    table.ForeignKey(
                        name: "FK_States_Countries_CountryID",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Administrators",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administrators", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Administrators_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<double>(type: "float", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Cards_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DebitCards",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CVV = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitCards", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DebitCards_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    NotificationTypeID = table.Column<int>(type: "int", nullable: false),
                    Read = table.Column<bool>(type: "bit", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderID = table.Column<int>(type: "int", nullable: true),
                    ReceiverID = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionOn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Urgent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Notifications_NotificationTypes_NotificationTypeID",
                        column: x => x.NotificationTypeID,
                        principalTable: "NotificationTypes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ReceiverID",
                        column: x => x.ReceiverID,
                        principalTable: "Users",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Notifications_Users_SenderID",
                        column: x => x.SenderID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<bool>(type: "bit", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    NotificationTypeID = table.Column<int>(type: "int", nullable: false),
                    Urgent = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_NotificationTypes_NotificationTypeID",
                        column: x => x.NotificationTypeID,
                        principalTable: "NotificationTypes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChargingStationID = table.Column<int>(type: "int", nullable: true),
                    ChargePointID = table.Column<int>(type: "int", nullable: true),
                    CommentTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Comments_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Comments_ChargingStations_ChargingStationID",
                        column: x => x.ChargingStationID,
                        principalTable: "ChargingStations",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Connectors",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargePointID = table.Column<int>(type: "int", nullable: false),
                    ConnectorType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Power = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    PricePerKWh = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FlatFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerMinute = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxPower = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connectors", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Connectors_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StateID = table.Column<int>(type: "int", nullable: true),
                    StateCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountryID = table.Column<int>(type: "int", nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    WikiDataId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Cities_Countries_CountryID",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Cities_States_StateID",
                        column: x => x.StateID,
                        principalTable: "States",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ChargeTags",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TagName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentTagId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Blocked = table.Column<bool>(type: "bit", nullable: true),
                    CardID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeTags", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargeTags_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    RechargeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Orders_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargePointID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ConnectorID = table.Column<int>(type: "int", nullable: false),
                    StartTagId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MeterStart = table.Column<double>(type: "float", nullable: false),
                    StartResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopTagId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeterStop = table.Column<double>(type: "float", nullable: true),
                    StopReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CardID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Transactions_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Transactions_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ChargePointId");
                });

            migrationBuilder.CreateTable(
                name: "ConnectorStatuses",
                columns: table => new
                {
                    ChargePointID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectorID = table.Column<int>(type: "int", nullable: false),
                    ID = table.Column<int>(type: "int", nullable: false),
                    LastStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastStatusTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChargePointID1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorStatuses", x => new { x.ConnectorID, x.ChargePointID });
                    table.ForeignKey(
                        name: "FK_ConnectorStatuses_ChargePoints_ChargePointID1",
                        column: x => x.ChargePointID1,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ConnectorStatuses_Connectors_ConnectorID",
                        column: x => x.ConnectorID,
                        principalTable: "Connectors",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Administrators_UserID",
                table: "Administrators",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_UserID",
                table: "Cards",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_ChargePointId",
                table: "ChargePoints",
                column: "ChargePointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_ChargingStationID",
                table: "ChargePoints",
                column: "ChargingStationID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeTags_CardID",
                table: "ChargeTags",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeTags_TagID",
                table: "ChargeTags",
                column: "TagID",
                unique: true,
                filter: "[TagID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountryID",
                table: "Cities",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateID",
                table: "Cities",
                column: "StateID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ChargePointID",
                table: "Comments",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ChargingStationID",
                table: "Comments",
                column: "ChargingStationID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserID",
                table: "Comments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Connectors_ChargePointID",
                table: "Connectors",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorStatuses_ChargePointID1",
                table: "ConnectorStatuses",
                column: "ChargePointID1");

            migrationBuilder.CreateIndex(
                name: "IX_DebitCards_UserID",
                table: "DebitCards",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationTypeID",
                table: "Notifications",
                column: "NotificationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReceiverID",
                table: "Notifications",
                column: "ReceiverID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SenderID",
                table: "Notifications",
                column: "SenderID");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_NotificationTypeID",
                table: "NotificationSettings",
                column: "NotificationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_UserID",
                table: "NotificationSettings",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CardID",
                table: "Orders",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserID",
                table: "Orders",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_States_CountryID",
                table: "States",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CardID",
                table: "Transactions",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ChargePointID",
                table: "Transactions",
                column: "ChargePointID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administrators");

            migrationBuilder.DropTable(
                name: "ChargeTags");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ConnectorStatuses");

            migrationBuilder.DropTable(
                name: "DebitCards");

            migrationBuilder.DropTable(
                name: "MessageLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "Connectors");

            migrationBuilder.DropTable(
                name: "NotificationTypes");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "ChargePoints");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ChargingStations");
        }
    }
}
