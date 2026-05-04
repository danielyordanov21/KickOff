using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KickOffAPI.Data.Projects.Migrations
{
    [DbContext(typeof(ProjectDbContext))]
    [Migration("20260405153000_ProjectFollowersAndNotifications")]
    public partial class ProjectFollowersAndNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectFollows",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReceiveInAppNotifications = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveEmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFollows", x => new { x.ProjectId, x.FollowerUserId });
                    table.ForeignKey(
                        name: "FK_ProjectFollows_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectUpdateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNotifications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectNotifications_ProjectUpdates_ProjectUpdateId",
                        column: x => x.ProjectUpdateId,
                        principalTable: "ProjectUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollows_FollowerUserId",
                table: "ProjectFollows",
                column: "FollowerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotifications_ProjectId",
                table: "ProjectNotifications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotifications_ProjectUpdateId",
                table: "ProjectNotifications",
                column: "ProjectUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotifications_RecipientUserId_IsRead_CreatedAt",
                table: "ProjectNotifications",
                columns: new[] { "RecipientUserId", "IsRead", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectFollows");

            migrationBuilder.DropTable(
                name: "ProjectNotifications");
        }

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("BackerIds")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Category")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CollaboratorsIdP")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Contacts")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(5000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("EndsAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("ExtraInfo")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("FinancialGoal")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("FinancialRaised")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Goal")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Headline")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrls")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OwnerId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Problem")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("SettingsId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("ProjectFollow", b =>
                {
                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("FollowerUserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("ReceiveEmailNotifications")
                        .HasColumnType("bit");

                    b.Property<bool>("ReceiveInAppNotifications")
                        .HasColumnType("bit");

                    b.HasKey("ProjectId", "FollowerUserId");

                    b.HasIndex("FollowerUserId");

                    b.ToTable("ProjectFollows");
                });

            modelBuilder.Entity("ProjectNotification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ProjectUpdateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("ReadAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("RecipientUserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.HasIndex("ProjectUpdateId");

                    b.HasIndex("RecipientUserId", "IsRead", "CreatedAt");

                    b.ToTable("ProjectNotifications");
                });

            modelBuilder.Entity("ProjectUpdate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("AuthorUserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("nvarchar(4000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("ProjectUpdates");
                });

            modelBuilder.Entity("ProjectFollow", b =>
                {
                    b.HasOne("Project", "Project")
                        .WithMany("Followers")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("ProjectNotification", b =>
                {
                    b.HasOne("Project", "Project")
                        .WithMany("Notifications")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ProjectUpdate", "ProjectUpdate")
                        .WithMany()
                        .HasForeignKey("ProjectUpdateId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.Navigation("Project");

                    b.Navigation("ProjectUpdate");
                });

            modelBuilder.Entity("ProjectUpdate", b =>
                {
                    b.HasOne("Project", "Project")
                        .WithMany("Updates")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Project", b =>
                {
                    b.Navigation("Followers");

                    b.Navigation("Notifications");

                    b.Navigation("Updates");
                });
#pragma warning restore 612, 618
        }
    }
}
