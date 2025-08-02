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
                name: "ChargePointBrands",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePointBrands", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ChargePointConfigurationItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComponentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariableInstance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariableUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariableMinLimit = table.Column<double>(type: "float", nullable: true),
                    VariableMaxLimit = table.Column<double>(type: "float", nullable: true),
                    VariableValuesList = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePointConfigurationItems", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ChargePointUptimes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargePointID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChargePointUptimeStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePointUptimes", x => x.ID);
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
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    WikiDataId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ElectricVehicleModels",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Make = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Range = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatteryCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageSrc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoSrc = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricVehicleModels", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.ID);
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
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentSent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentReceived = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    EmailTemplatePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEmail = table.Column<bool>(type: "bit", nullable: false),
                    IsSms = table.Column<bool>(type: "bit", nullable: false),
                    IsPushNotification = table.Column<bool>(type: "bit", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ForAdmins = table.Column<bool>(type: "bit", nullable: false),
                    ForSupports = table.Column<bool>(type: "bit", nullable: false),
                    ForPartners = table.Column<bool>(type: "bit", nullable: false),
                    ForUsers = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.ID);
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
                    ForPartners = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OcppComponents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Component = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcppComponents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationEVSEs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EVSEId = table.Column<int>(type: "int", nullable: false),
                    ConnectorId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationEVSEs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationVariableCharacteristics",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    MinLimit = table.Column<double>(type: "float", nullable: true),
                    MaxLimit = table.Column<double>(type: "float", nullable: true),
                    ValuesList = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportsMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationVariableCharacteristics", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationVariables",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationVariables", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OCPPDisplayMessageContents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Format = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPDisplayMessageContents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OcppVariableComponents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Component = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Variable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Required = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcppVariableComponents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OcppVariables",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcppVariables", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ChargePointModels",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConnectorCount = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargePointBrandID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePointModels", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargePointModels_ChargePointBrands_ChargePointBrandID",
                        column: x => x.ChargePointBrandID,
                        principalTable: "ChargePointBrands",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationItemVariableAttributes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mutability = table.Column<int>(type: "int", nullable: true),
                    Persistent = table.Column<bool>(type: "bit", nullable: true),
                    Constant = table.Column<bool>(type: "bit", nullable: true),
                    ChargePointConfigurationItemID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationItemVariableAttributes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ConfigurationItemVariableAttributes_ChargePointConfigurationItems_ChargePointConfigurationItemID",
                        column: x => x.ChargePointConfigurationItemID,
                        principalTable: "ChargePointConfigurationItems",
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
                    CountryID = table.Column<int>(type: "int", nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FipsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Iso2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    WikiDataId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.ID);
                    table.ForeignKey(
                        name: "FK_States_Countries_CountryID",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxIdentificationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartnerIdentificationNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Partners_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationComponents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OCPPConfigurationEVSEID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationComponents", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationComponents_OCPPConfigurationEVSEs_OCPPConfigurationEVSEID",
                        column: x => x.OCPPConfigurationEVSEID,
                        principalTable: "OCPPConfigurationEVSEs",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    PermissionID = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionID",
                        column: x => x.PermissionID,
                        principalTable: "Permissions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargePointFeaturess",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargePointModelID = table.Column<int>(type: "int", nullable: false),
                    AutoSchedules = table.Column<bool>(type: "bit", nullable: false),
                    AutoStart = table.Column<bool>(type: "bit", nullable: false),
                    LocalAuthListManagement = table.Column<bool>(type: "bit", nullable: false),
                    Powerbank = table.Column<bool>(type: "bit", nullable: true),
                    ReleaseDetection = table.Column<bool>(type: "bit", nullable: true),
                    AutoCharge = table.Column<bool>(type: "bit", nullable: true),
                    LoadBalancing = table.Column<bool>(type: "bit", nullable: true),
                    FirmwareManagement = table.Column<bool>(type: "bit", nullable: true),
                    SolarCharge = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePointFeaturess", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargePointFeaturess_ChargePointModels_ChargePointModelID",
                        column: x => x.ChargePointModelID,
                        principalTable: "ChargePointModels",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargePointIntegrations",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargePointModelID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePointIntegrations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargePointIntegrations_ChargePointModels_ChargePointModelID",
                        column: x => x.ChargePointModelID,
                        principalTable: "ChargePointModels",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportedKwhs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<double>(type: "float", nullable: false),
                    ChargePointModelID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportedKwhs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SupportedKwhs_ChargePointModels_ChargePointModelID",
                        column: x => x.ChargePointModelID,
                        principalTable: "ChargePointModels",
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
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    WikiDataId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "ChargingStations",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                    PartnerID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargingStations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargingStations_Partners_PartnerID",
                        column: x => x.PartnerID,
                        principalTable: "Partners",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ElectricVehicleModelID = table.Column<int>(type: "int", nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PartnerID = table.Column<int>(type: "int", nullable: true),
                    ImageID = table.Column<int>(type: "int", nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPhoneNumberVerified = table.Column<bool>(type: "bit", nullable: false),
                    PhoneVerificationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    ResetPasswordToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Users_ElectricVehicleModels_ElectricVehicleModelID",
                        column: x => x.ElectricVehicleModelID,
                        principalTable: "ElectricVehicleModels",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Users_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Users_Partners_PartnerID",
                        column: x => x.PartnerID,
                        principalTable: "Partners",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargePoints",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargePointId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChargingStationID = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargePointModelID = table.Column<int>(type: "int", nullable: true),
                    ChargePointBrandID = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowOnMap = table.Column<bool>(type: "bit", nullable: true),
                    HasChargeCable = table.Column<bool>(type: "bit", nullable: true),
                    ClientCertThumb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargePoints", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargePoints_ChargePointBrands_ChargePointBrandID",
                        column: x => x.ChargePointBrandID,
                        principalTable: "ChargePointBrands",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ChargePoints_ChargePointModels_ChargePointModelID",
                        column: x => x.ChargePointModelID,
                        principalTable: "ChargePointModels",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_ChargePoints_ChargingStations_ChargingStationID",
                        column: x => x.ChargingStationID,
                        principalTable: "ChargingStations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargingStationImages",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargingStationID = table.Column<int>(type: "int", nullable: false),
                    ImageID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargingStationImages", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargingStationImages_ChargingStations_ChargingStationID",
                        column: x => x.ChargingStationID,
                        principalTable: "ChargingStations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChargingStationImages_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Administrators",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administrators", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Administrators_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
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
                    Blocked = table.Column<bool>(type: "bit", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Cards_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "DebitCards",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CVV = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitCards", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DebitCards_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    NotificationTypeID = table.Column<int>(type: "int", nullable: true),
                    Read = table.Column<bool>(type: "bit", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderID = table.Column<int>(type: "int", nullable: true),
                    ReceiverID = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionOn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Urgent = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Notifications_NotificationTypes_NotificationTypeID",
                        column: x => x.NotificationTypeID,
                        principalTable: "NotificationTypes",
                        principalColumn: "ID");
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
                    UserID = table.Column<int>(type: "int", nullable: true),
                    NotificationTypeID = table.Column<int>(type: "int", nullable: true),
                    Urgent = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_NotificationTypes_NotificationTypeID",
                        column: x => x.NotificationTypeID,
                        principalTable: "NotificationTypes",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_NotificationSettings_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Ratings_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInfoDownloadRequests",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RequestTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInfoDownloadRequests", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserInfoDownloadRequests_Users_UserID",
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
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    EvseID = table.Column<int>(type: "int", nullable: false),
                    ChargePointID = table.Column<int>(type: "int", nullable: true),
                    ConnectorType = table.Column<int>(type: "int", nullable: true),
                    Power = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerKWh = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerIdleMinute = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerMinute = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostPerKwh = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FlatFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxPower = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connectors", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Connectors_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargePointID = table.Column<int>(type: "int", nullable: false),
                    OCPPConfigurationComponentID = table.Column<int>(type: "int", nullable: false),
                    OCPPConfigurationVariableID = table.Column<int>(type: "int", nullable: false),
                    OCPPConfigurationVariableCharacteristicID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_OCPPConfigurationComponents_OCPPConfigurationComponentID",
                        column: x => x.OCPPConfigurationComponentID,
                        principalTable: "OCPPConfigurationComponents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_OCPPConfigurationVariableCharacteristics_OCPPConfigurationVariableCharacteristicID",
                        column: x => x.OCPPConfigurationVariableCharacteristicID,
                        principalTable: "OCPPConfigurationVariableCharacteristics",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationItems_OCPPConfigurationVariables_OCPPConfigurationVariableID",
                        column: x => x.OCPPConfigurationVariableID,
                        principalTable: "OCPPConfigurationVariables",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OCPPDisplayMessageInfos",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargePointID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayID = table.Column<int>(type: "int", nullable: false),
                    DisplayMessageId = table.Column<int>(type: "int", nullable: false),
                    MessageID = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargePointID1 = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPDisplayMessageInfos", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPDisplayMessageInfos_ChargePoints_ChargePointID1",
                        column: x => x.ChargePointID1,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_OCPPDisplayMessageInfos_OCPPConfigurationComponents_DisplayID",
                        column: x => x.DisplayID,
                        principalTable: "OCPPConfigurationComponents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OCPPDisplayMessageInfos_OCPPDisplayMessageContents_MessageID",
                        column: x => x.MessageID,
                        principalTable: "OCPPDisplayMessageContents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OCPPLocalListVersions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ChargePointID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPLocalListVersions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPLocalListVersions_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardExpirationNotifications",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardID = table.Column<int>(type: "int", nullable: false),
                    IntervalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardExpirationNotifications", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CardExpirationNotifications_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
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
                    CardID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeTags", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargeTags_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RechargeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Orders_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "RatingReports",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RatingID = table.Column<int>(type: "int", nullable: true),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    IssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingReports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RatingReports_Ratings_RatingID",
                        column: x => x.RatingID,
                        principalTable: "Ratings",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_RatingReports_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargingSessions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CardID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StoppedReason = table.Column<int>(type: "int", nullable: false),
                    ChargingSessionStatus = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargingSessions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChargingSessions_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChargingSessions_Connectors_ConnectorID",
                        column: x => x.ConnectorID,
                        principalTable: "Connectors",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChargingSessions_Users_UserID",
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
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChargingStationID = table.Column<int>(type: "int", nullable: true),
                    ChargePointID = table.Column<int>(type: "int", nullable: true),
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    CommentTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        name: "FK_Comments_Connectors_ConnectorID",
                        column: x => x.ConnectorID,
                        principalTable: "Connectors",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ConnectorStatuses",
                columns: table => new
                {
                    ChargePointID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectorID = table.Column<int>(type: "int", nullable: false),
                    ID = table.Column<int>(type: "int", nullable: false),
                    LastStatus = table.Column<int>(type: "int", nullable: true),
                    LastStatusTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChargePointID1 = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    ChargePointID = table.Column<int>(type: "int", nullable: true),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    IssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsEmail = table.Column<bool>(type: "bit", nullable: false),
                    IsNotification = table.Column<bool>(type: "bit", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Reports_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Reports_Connectors_ConnectorID",
                        column: x => x.ConnectorID,
                        principalTable: "Connectors",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Reports_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemReports",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    CardID = table.Column<int>(type: "int", nullable: true),
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    ChargePointID = table.Column<int>(type: "int", nullable: true),
                    ResolvedByID = table.Column<int>(type: "int", nullable: true),
                    AssignedID = table.Column<int>(type: "int", nullable: true),
                    IssueDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEmail = table.Column<bool>(type: "bit", nullable: false),
                    IsNotification = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Criticality = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemReports", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemReports_Cards_CardID",
                        column: x => x.CardID,
                        principalTable: "Cards",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SystemReports_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SystemReports_Connectors_ConnectorID",
                        column: x => x.ConnectorID,
                        principalTable: "Connectors",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SystemReports_Users_AssignedID",
                        column: x => x.AssignedID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemReports_Users_ResolvedByID",
                        column: x => x.ResolvedByID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemReports_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "OCPPConfigurationVariableAttributes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mutability = table.Column<int>(type: "int", nullable: true),
                    Persistent = table.Column<bool>(type: "bit", nullable: true),
                    Constant = table.Column<bool>(type: "bit", nullable: true),
                    OCPPConfigurationItemID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPConfigurationVariableAttributes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPConfigurationVariableAttributes_OCPPConfigurationItems_OCPPConfigurationItemID",
                        column: x => x.OCPPConfigurationItemID,
                        principalTable: "OCPPConfigurationItems",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OCPPLocalListItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenType = table.Column<int>(type: "int", nullable: false),
                    TokenStatus = table.Column<int>(type: "int", nullable: false),
                    OCPPLocalListVersionID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCPPLocalListItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OCPPLocalListItems_OCPPLocalListVersions_OCPPLocalListVersionID",
                        column: x => x.OCPPLocalListVersionID,
                        principalTable: "OCPPLocalListVersions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargingSessionID = table.Column<int>(type: "int", nullable: false),
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    StartCardID = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MeterStart = table.Column<double>(type: "float", nullable: false),
                    StartResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopCardID = table.Column<int>(type: "int", nullable: true),
                    StopTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeterStop = table.Column<double>(type: "float", nullable: true),
                    StopReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChargePointID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Transactions_Cards_StartCardID",
                        column: x => x.StartCardID,
                        principalTable: "Cards",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Transactions_Cards_StopCardID",
                        column: x => x.StopCardID,
                        principalTable: "Cards",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Transactions_ChargePoints_ChargePointID",
                        column: x => x.ChargePointID,
                        principalTable: "ChargePoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Transactions_ChargingSessions_ChargingSessionID",
                        column: x => x.ChargingSessionID,
                        principalTable: "ChargingSessions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Connectors_ConnectorID",
                        column: x => x.ConnectorID,
                        principalTable: "Connectors",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CommentImages",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentID = table.Column<int>(type: "int", nullable: false),
                    ImageID = table.Column<int>(type: "int", nullable: false),
                    ImagePriority = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentImages", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CommentImages_Comments_CommentID",
                        column: x => x.CommentID,
                        principalTable: "Comments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentImages_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentReplies",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentID = table.Column<int>(type: "int", nullable: false),
                    Reply = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentReplies", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CommentReplies_Comments_CommentID",
                        column: x => x.CommentID,
                        principalTable: "Comments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportImages",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportID = table.Column<int>(type: "int", nullable: false),
                    ImageID = table.Column<int>(type: "int", nullable: false),
                    ImagePriority = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportImages", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ReportImages_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportImages_Reports_ReportID",
                        column: x => x.ReportID,
                        principalTable: "Reports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportReplies",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportID = table.Column<int>(type: "int", nullable: false),
                    Reply = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportReplies", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ReportReplies_Reports_ReportID",
                        column: x => x.ReportID,
                        principalTable: "Reports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemReportComments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemReportID = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemReportComments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemReportComments_SystemReports_SystemReportID",
                        column: x => x.SystemReportID,
                        principalTable: "SystemReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemReportComments_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemReportImage",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageID = table.Column<int>(type: "int", nullable: false),
                    SystemReportID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemReportImage", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemReportImage_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemReportImage_SystemReports_SystemReportID",
                        column: x => x.SystemReportID,
                        principalTable: "SystemReports",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorUptimes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionID = table.Column<int>(type: "int", nullable: true),
                    ConnectorUptimeStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorUptimes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ConnectorUptimes_Transactions_TransactionID",
                        column: x => x.TransactionID,
                        principalTable: "Transactions",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SystemReportCommentImage",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageID = table.Column<int>(type: "int", nullable: false),
                    SystemReportCommentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemReportCommentImage", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemReportCommentImage_Images_ImageID",
                        column: x => x.ImageID,
                        principalTable: "Images",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemReportCommentImage_SystemReportComments_SystemReportCommentID",
                        column: x => x.SystemReportCommentID,
                        principalTable: "SystemReportComments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Administrators_UserID",
                table: "Administrators",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_CardExpirationNotifications_CardID",
                table: "CardExpirationNotifications",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_UserID",
                table: "Cards",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargePointFeaturess_ChargePointModelID",
                table: "ChargePointFeaturess",
                column: "ChargePointModelID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargePointIntegrations_ChargePointModelID",
                table: "ChargePointIntegrations",
                column: "ChargePointModelID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargePointModels_ChargePointBrandID",
                table: "ChargePointModels",
                column: "ChargePointBrandID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_ChargePointBrandID",
                table: "ChargePoints",
                column: "ChargePointBrandID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_ChargePointId",
                table: "ChargePoints",
                column: "ChargePointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargePoints_ChargePointModelID",
                table: "ChargePoints",
                column: "ChargePointModelID");

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
                name: "IX_ChargingSessions_CardID",
                table: "ChargingSessions",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSessions_ConnectorID",
                table: "ChargingSessions",
                column: "ConnectorID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSessions_UserID",
                table: "ChargingSessions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingStationImages_ChargingStationID",
                table: "ChargingStationImages",
                column: "ChargingStationID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingStationImages_ImageID",
                table: "ChargingStationImages",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingStations_Name",
                table: "ChargingStations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChargingStations_PartnerID",
                table: "ChargingStations",
                column: "PartnerID");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountryID",
                table: "Cities",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateID",
                table: "Cities",
                column: "StateID");

            migrationBuilder.CreateIndex(
                name: "IX_CommentImages_CommentID",
                table: "CommentImages",
                column: "CommentID");

            migrationBuilder.CreateIndex(
                name: "IX_CommentImages_ImageID",
                table: "CommentImages",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReplies_CommentID",
                table: "CommentReplies",
                column: "CommentID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ChargePointID",
                table: "Comments",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ChargingStationID",
                table: "Comments",
                column: "ChargingStationID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ConnectorID",
                table: "Comments",
                column: "ConnectorID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserID",
                table: "Comments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationItemVariableAttributes_ChargePointConfigurationItemID",
                table: "ConfigurationItemVariableAttributes",
                column: "ChargePointConfigurationItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Connectors_ChargePointID",
                table: "Connectors",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_Connectors_EvseID_ConnectorID_ChargePointID",
                table: "Connectors",
                columns: new[] { "EvseID", "ConnectorID", "ChargePointID" },
                unique: true,
                filter: "[ConnectorID] IS NOT NULL AND [ChargePointID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorStatuses_ChargePointID1",
                table: "ConnectorStatuses",
                column: "ChargePointID1");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorUptimes_TransactionID",
                table: "ConnectorUptimes",
                column: "TransactionID");

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
                name: "IX_OCPPConfigurationComponents_OCPPConfigurationEVSEID",
                table: "OCPPConfigurationComponents",
                column: "OCPPConfigurationEVSEID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_ChargePointID",
                table: "OCPPConfigurationItems",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_OCPPConfigurationComponentID",
                table: "OCPPConfigurationItems",
                column: "OCPPConfigurationComponentID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_OCPPConfigurationVariableCharacteristicID",
                table: "OCPPConfigurationItems",
                column: "OCPPConfigurationVariableCharacteristicID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationItems_OCPPConfigurationVariableID",
                table: "OCPPConfigurationItems",
                column: "OCPPConfigurationVariableID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPConfigurationVariableAttributes_OCPPConfigurationItemID",
                table: "OCPPConfigurationVariableAttributes",
                column: "OCPPConfigurationItemID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPDisplayMessageInfos_ChargePointID1",
                table: "OCPPDisplayMessageInfos",
                column: "ChargePointID1");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPDisplayMessageInfos_DisplayID",
                table: "OCPPDisplayMessageInfos",
                column: "DisplayID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPDisplayMessageInfos_MessageID",
                table: "OCPPDisplayMessageInfos",
                column: "MessageID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPLocalListItems_OCPPLocalListVersionID",
                table: "OCPPLocalListItems",
                column: "OCPPLocalListVersionID");

            migrationBuilder.CreateIndex(
                name: "IX_OCPPLocalListVersions_ChargePointID",
                table: "OCPPLocalListVersions",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CardID",
                table: "Orders",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserID",
                table: "Orders",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_ImageID",
                table: "Partners",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_PartnerIdentificationNumber",
                table: "Partners",
                column: "PartnerIdentificationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingReports_RatingID",
                table: "RatingReports",
                column: "RatingID");

            migrationBuilder.CreateIndex(
                name: "IX_RatingReports_UserID",
                table: "RatingReports",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserID",
                table: "Ratings",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportImages_ImageID",
                table: "ReportImages",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportImages_ReportID",
                table: "ReportImages",
                column: "ReportID");

            migrationBuilder.CreateIndex(
                name: "IX_ReportReplies_ReportID",
                table: "ReportReplies",
                column: "ReportID");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ChargePointID",
                table: "Reports",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ConnectorID",
                table: "Reports",
                column: "ConnectorID");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UserID",
                table: "Reports",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionID",
                table: "RolePermissions",
                column: "PermissionID");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleID",
                table: "RolePermissions",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_States_CountryID",
                table: "States",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_SupportedKwhs_ChargePointModelID",
                table: "SupportedKwhs",
                column: "ChargePointModelID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportCommentImage_ImageID",
                table: "SystemReportCommentImage",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportCommentImage_SystemReportCommentID",
                table: "SystemReportCommentImage",
                column: "SystemReportCommentID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportComments_SystemReportID",
                table: "SystemReportComments",
                column: "SystemReportID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportComments_UserID",
                table: "SystemReportComments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportImage_ImageID",
                table: "SystemReportImage",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReportImage_SystemReportID",
                table: "SystemReportImage",
                column: "SystemReportID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_AssignedID",
                table: "SystemReports",
                column: "AssignedID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_CardID",
                table: "SystemReports",
                column: "CardID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_ChargePointID",
                table: "SystemReports",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_ConnectorID",
                table: "SystemReports",
                column: "ConnectorID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_ResolvedByID",
                table: "SystemReports",
                column: "ResolvedByID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemReports_UserID",
                table: "SystemReports",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ChargePointID",
                table: "Transactions",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ChargingSessionID",
                table: "Transactions",
                column: "ChargingSessionID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ConnectorID",
                table: "Transactions",
                column: "ConnectorID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_StartCardID",
                table: "Transactions",
                column: "StartCardID");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_StopCardID",
                table: "Transactions",
                column: "StopCardID");

            migrationBuilder.CreateIndex(
                name: "IX_UserInfoDownloadRequests_UserID",
                table: "UserInfoDownloadRequests",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ElectricVehicleModelID",
                table: "Users",
                column: "ElectricVehicleModelID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ImageID",
                table: "Users",
                column: "ImageID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PartnerID",
                table: "Users",
                column: "PartnerID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administrators");

            migrationBuilder.DropTable(
                name: "CardExpirationNotifications");

            migrationBuilder.DropTable(
                name: "ChargePointFeaturess");

            migrationBuilder.DropTable(
                name: "ChargePointIntegrations");

            migrationBuilder.DropTable(
                name: "ChargePointUptimes");

            migrationBuilder.DropTable(
                name: "ChargeTags");

            migrationBuilder.DropTable(
                name: "ChargingStationImages");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "CommentImages");

            migrationBuilder.DropTable(
                name: "CommentReplies");

            migrationBuilder.DropTable(
                name: "ConfigurationItemVariableAttributes");

            migrationBuilder.DropTable(
                name: "ConnectorStatuses");

            migrationBuilder.DropTable(
                name: "ConnectorUptimes");

            migrationBuilder.DropTable(
                name: "DebitCards");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "MessageLogs");

            migrationBuilder.DropTable(
                name: "Notices");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "OcppComponents");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationVariableAttributes");

            migrationBuilder.DropTable(
                name: "OCPPDisplayMessageInfos");

            migrationBuilder.DropTable(
                name: "OCPPLocalListItems");

            migrationBuilder.DropTable(
                name: "OcppVariableComponents");

            migrationBuilder.DropTable(
                name: "OcppVariables");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "RatingReports");

            migrationBuilder.DropTable(
                name: "ReportImages");

            migrationBuilder.DropTable(
                name: "ReportReplies");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SupportedKwhs");

            migrationBuilder.DropTable(
                name: "SystemReportCommentImage");

            migrationBuilder.DropTable(
                name: "SystemReportImage");

            migrationBuilder.DropTable(
                name: "UserInfoDownloadRequests");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ChargePointConfigurationItems");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "NotificationTypes");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationItems");

            migrationBuilder.DropTable(
                name: "OCPPDisplayMessageContents");

            migrationBuilder.DropTable(
                name: "OCPPLocalListVersions");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SystemReportComments");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "ChargingSessions");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationComponents");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationVariableCharacteristics");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationVariables");

            migrationBuilder.DropTable(
                name: "SystemReports");

            migrationBuilder.DropTable(
                name: "OCPPConfigurationEVSEs");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "Connectors");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ChargePoints");

            migrationBuilder.DropTable(
                name: "ElectricVehicleModels");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ChargePointModels");

            migrationBuilder.DropTable(
                name: "ChargingStations");

            migrationBuilder.DropTable(
                name: "ChargePointBrands");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
