using KickOffAPI.Data.Seeders;
using KickOffAPI.Extensions;
using KickOffAPI.Models;
using KickOffAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Text;

const string HttpsOpenCorsPolicy = "HttpsOpenCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);

var configuration = builder.Configuration;

var appDbConnectionString = configuration.GetConnectionString("AppDb");
if (string.IsNullOrWhiteSpace(appDbConnectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:AppDb is not configured. Provide it via appsettings.Local.json, environment variables, or dotnet user-secrets.");

var jwtKey = GetRequiredConfigurationValue(configuration, "Jwt:Key");
var jwtIssuer = GetRequiredConfigurationValue(configuration, "Jwt:Issuer");
var jwtAudience = GetRequiredConfigurationValue(configuration, "Jwt:Audience");

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(
        appDbConnectionString,
        b =>
        {
            b.MigrationsHistoryTable("__IdentityMigrationsHistory");
            b.EnableRetryOnFailure();
        }
    ));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseSqlServer(
        appDbConnectionString,
        b =>
        {
            b.MigrationsHistoryTable("__ProjectMigrationsHistory");
            b.EnableRetryOnFailure();
        }
    ));

// JWT Authentication
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var userId = context.Principal?.GetApplicationUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                context.Fail("The token did not include a valid user id.");
                return;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                context.Fail("The account no longer exists.");
                return;
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                context.Fail("The account is not currently active.");
            }
        }
    };
});

builder.Services.Configure<SendbirdModel>(
    configuration.GetSection("Sendbird"));
builder.Services.Configure<AuthOptions>(
    configuration.GetSection("Auth"));
builder.Services.Configure<SmtpOptions>(
    configuration.GetSection("Smtp"));
builder.Services.Configure<ProjectNotificationOptions>(
    configuration.GetSection("ProjectNotifications"));
builder.Services.Configure<ProductionBootstrapOptions>(
    configuration.GetSection("ProductionBootstrap"));
builder.Services.AddHttpClient<SendbirdService>();

// Services
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(HttpsOpenCorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped(typeof(IBaseRepository<,>), typeof(BaseRepository<,>));
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<UserRepository>();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<ProjectFollowService>();
builder.Services.AddScoped<ProjectNotificationService>();
builder.Services.AddScoped<ClientAppUrlResolver>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserFollowService, UserFollowService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<BlobService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var identityDbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    var projectDbContext = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    var productionBootstrapOptions = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<ProductionBootstrapOptions>>()
        .Value;

    if (app.Environment.IsDevelopment())
    {
        // Make first-run local setup reproducible on a clean machine.
        await PrepareIdentityDatabaseAsync(identityDbContext);
        await projectDbContext.Database.MigrateAsync();
    }
    else if (ShouldRunProductionBootstrap(app.Environment, productionBootstrapOptions))
    {
        await PrepareIdentityDatabaseAsync(identityDbContext);
        await projectDbContext.Database.MigrateAsync();
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Producer", "Backer", "User", "Guest" };

    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    if (app.Environment.IsDevelopment())
    {
        await UserDbSeeder.SeedAsync(scope.ServiceProvider);
        await UserFollowDbSeeder.SeedAsync(scope.ServiceProvider);
        await ProjectDbSeeder.SeedAsync(scope.ServiceProvider);
    }
    else if (ShouldRunProductionBootstrap(app.Environment, productionBootstrapOptions))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = GetRequiredBootstrapValue(productionBootstrapOptions.AdminEmail, "ProductionBootstrap:AdminEmail");
        var adminPassword = GetRequiredBootstrapValue(productionBootstrapOptions.AdminPassword, "ProductionBootstrap:AdminPassword");

        await AdminAccountBootstrapper.EnsureAdminUserAsync(userManager, adminEmail, adminPassword);
    }
}

app.UseHttpsRedirection();
app.UseWhen(
    context => context.Request.IsHttps,
    branch => branch.UseCors(HttpsOpenCorsPolicy));
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string GetRequiredConfigurationValue(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException(
            $"Configuration value '{key}' is required. Provide it via appsettings.Local.json, environment variables, or dotnet user-secrets.");

    return value;
}

static async Task PrepareIdentityDatabaseAsync(AppIdentityDbContext identityDbContext)
{
    const string initialIdentityMigrationId = "20260420122112_InitialIdentitySchema";
    const string productVersion = "10.0.2";

    string[] expectedIdentityTables =
    [
        "Roles",
        "Users",
        "UserRoles",
        "UserClaims",
        "UserLogins",
        "UserTokens",
        "RoleClaims",
        "RefreshTokens",
        "UserFollows"
    ];

    if (!await identityDbContext.Database.CanConnectAsync())
    {
        await identityDbContext.Database.MigrateAsync();
        return;
    }

    var appliedMigrations = (await identityDbContext.Database.GetAppliedMigrationsAsync())
        .ToHashSet(StringComparer.Ordinal);

    if (!appliedMigrations.Contains(initialIdentityMigrationId)
        && await AllTablesExistAsync(identityDbContext, expectedIdentityTables))
    {
        Console.WriteLine(
            "Detected an existing Identity schema without EF migration history. Recording the initial Identity migration as a baseline.");

        await EnsureIdentityHistoryTableExistsAsync(identityDbContext);

        await identityDbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            IF NOT EXISTS (
                SELECT 1
                FROM [__IdentityMigrationsHistory]
                WHERE [MigrationId] = {initialIdentityMigrationId}
            )
            BEGIN
                INSERT INTO [__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES ({initialIdentityMigrationId}, {productVersion})
            END
            """);
    }

    await identityDbContext.Database.MigrateAsync();
}

static async Task<bool> AllTablesExistAsync(DbContext dbContext, IEnumerable<string> tableNames)
{
    foreach (var tableName in tableNames)
    {
        if (!await TableExistsAsync(dbContext, tableName))
            return false;
    }

    return true;
}

static async Task EnsureIdentityHistoryTableExistsAsync(DbContext dbContext)
{
    if (await TableExistsAsync(dbContext, "__IdentityMigrationsHistory"))
        return;

    await dbContext.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE [__IdentityMigrationsHistory] (
            [MigrationId] nvarchar(150) NOT NULL,
            [ProductVersion] nvarchar(32) NOT NULL,
            CONSTRAINT [PK___IdentityMigrationsHistory] PRIMARY KEY ([MigrationId])
        )
        """);
}

static async Task<bool> TableExistsAsync(DbContext dbContext, string tableName)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldCloseConnection = connection.State != ConnectionState.Open;

    if (shouldCloseConnection)
        await connection.OpenAsync();

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM sys.tables
            WHERE [name] = @tableName
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return result is not null && result is not DBNull;
    }
    finally
    {
        if (shouldCloseConnection)
            await connection.CloseAsync();
    }
}

static bool ShouldRunProductionBootstrap(IWebHostEnvironment environment, ProductionBootstrapOptions options)
{
    return !environment.IsDevelopment() && options.Enabled;
}

static string GetRequiredBootstrapValue(string? value, string key)
{
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException(
            $"Configuration value '{key}' is required when production bootstrap is enabled.");

    return value.Trim();
}

public partial class Program;
