# KickOff

KickOff is a two-part project:

- `KickOffAPI`: ASP.NET Core Web API backed by SQL Server and Entity Framework Core
- `KickOffClient`: Angular client that talks to the API through a local dev proxy

This guide is the handoff setup path for opening, running, and testing the project on a clean machine.

## Tested Prerequisites

These are the exact tool versions verified in this workspace:

- Windows with PowerShell
- .NET SDK `10.0.202`
- Node.js `v24.12.0`
- npm `10.9.4`
- A SQL Server instance reachable from the connection string

Notes:

- `KickOffClient` uses `npm ci` and the committed `package-lock.json`.
- The Angular dev server proxies API traffic to `https://localhost:5001`.
- If HTTPS localhost certificates are not trusted on the machine, run `dotnet dev-certs https --trust`.

## Project Structure

- `KickOffAPI/`: backend, EF Core migrations, seed data, local config template
- `KickOffClient/`: frontend, Angular source, proxy configuration
- `.gitignore`: excludes secrets, build outputs, `node_modules`, and generated artifacts that should not be archived
- `scripts/check-no-secrets.ps1`: tracked-file safety check that fails if local secret files or obvious credentials are committed

## Quick Start

### 1. Configure the backend

Use `KickOffAPI/appsettings.Local.example.json` as the template for `KickOffAPI/appsettings.Local.json`.

Required configuration groups:

- `ConnectionStrings:AppDb`
  The SQL Server connection string for the application database.
- `Jwt`
  Required for API authentication. `Key`, `Issuer`, and `Audience` must be set.
- `DevelopmentSeed`
  Passwords used when the app seeds development users.
- `AzureBlob`
  Required for profile/project image upload features.
- `Sendbird`
  Required for chat features.

Optional configuration groups:

- `Smtp`
  Leave `Enabled` as `false` if email delivery is not available locally. The app can still run without SMTP.
- `Auth` and `ProjectNotifications`
  Use `http://localhost:4200` for local frontend links.

### 2. Restore and run the backend

From the repository root:

```powershell
cd .\KickOffAPI
dotnet restore
dotnet run
```

The default local API URLs come from `KickOffAPI/Properties/launchSettings.json`:

- `https://localhost:5001`
- `http://localhost:5000`

### 3. Restore and run the frontend

From the repository root in a second terminal:

```powershell
cd .\KickOffClient
npm ci
npm start
```

Open the client at:

- `http://localhost:4200`

### 4. Sign in with the seeded admin account

When the API is running in the `Development` environment with seed data enabled, use:

- Email: `admin@kickoff.test`
- Password: the private `DevelopmentSeed:AdminPassword` value from `KickOffAPI/appsettings.Local.json`

## Secret Safety

The repository is intended to keep credentials out of Git:

- local secret files such as `KickOffAPI/appsettings.Local.json`, `KickOffAPI/.env`, and `KickOffClient/.env` are gitignored
- GitHub deployment credentials stay in GitHub Actions secrets, not in tracked files
- the example config file only contains placeholders

Before pushing, run:

```powershell
./scripts/check-no-secrets.ps1
```

That script fails if tracked files include local secret file paths, private key blocks, publish profiles, or obvious inline credentials.

## Azure Hosting

The project is hosted in Azure with the following production resources:

- Frontend: Azure Static Web Apps `kickoff-client-prod`
- Frontend URL: `https://wonderful-moss-014a43803.7.azurestaticapps.net`
- API: Azure App Service `kickoff-api-prod`
- API URL: `https://kickoff-api-prod.azurewebsites.net`
- Database: Azure SQL server `kickoffserver` with database `KickOffDB`
- Blob Storage: storage account `kickoffblob` with container `media`

### Hosted Backend Settings

The deployed API is expected to use the hosted frontend URL for generated links:

- `Auth:ClientBaseUrl = https://wonderful-moss-014a43803.7.azurestaticapps.net`
- `ProjectNotifications:ClientBaseUrl = https://wonderful-moss-014a43803.7.azurestaticapps.net`

### Production Deployment Notes

- The production database is intended to start empty.
- The App Service deployment workflow uses the GitHub secret `AZURE_WEBAPP_PUBLISH_PROFILE`.
- After the first successful API deployment, run EF Core migrations for both contexts against `KickOffDB`.
- The development seed admin account is not created automatically in normal production startup.

### One-Time Production Bootstrap

If you want Azure production to create the admin account on first startup, set these App Service environment variables:

- `ProductionBootstrap__Enabled = true`
- `ProductionBootstrap__AdminEmail = admin@kickoff.test`
- `ProductionBootstrap__AdminPassword = <private bootstrap password>`

When that flag is enabled outside `Development`, the API will:

- apply EF Core migrations for both database contexts
- create the standard roles
- create the admin account if it does not already exist

After the admin has been created successfully, turn `ProductionBootstrap__Enabled` back to `false`.

## Backend Configuration Reference

`KickOffAPI/Program.cs` requires the database connection string and JWT values at startup. The API also binds the following feature settings:

- `AzureBlob`
  Storage account connection string and container name for uploaded images.
- `Sendbird`
  Chat app ID and server API token.
- `Smtp`
  Email delivery configuration. Safe to keep disabled for local review if email delivery is not needed.
- `Auth:ClientBaseUrl`
  Used when generating auth links that point back to the frontend.
- `ProjectNotifications:ClientBaseUrl`
  Used when generating frontend links from project notification emails.

Recommended local values:

- `Auth:ClientBaseUrl = http://localhost:4200`
- `ProjectNotifications:ClientBaseUrl = http://localhost:4200`

## Database Setup

The API uses two EF Core contexts against the same SQL Server database:

- `AppIdentityDbContext`
  Identity users, roles, refresh tokens, and user follow relationships
- `ProjectDbContext`
  Project, update, follow, and notification data

For local development and handoff runs, the API now applies pending migrations for both contexts automatically before creating roles and running seed data.

That means a clean machine only needs:

1. a reachable SQL Server instance,
2. a valid `ConnectionStrings:AppDb`,
3. `dotnet run` from `KickOffAPI`.

If you want to apply migrations manually instead, use:

```powershell
cd .\KickOffAPI
dotnet ef database update --context AppIdentityDbContext
dotnet ef database update --context ProjectDbContext
```

## Development Seed Data

When the API runs in the `Development` environment, it:

- creates the roles `Admin`, `Producer`, `Backer`, `User`, and `Guest`
- seeds the admin account `admin@kickoff.test`
- seeds additional demo users and demo projects

Demo credentials used in the current handoff setup:

- Admin email: `admin@kickoff.test`
- Admin password: the private `DevelopmentSeed:AdminPassword` value from `KickOffAPI/appsettings.Local.json`

Passwords come from `DevelopmentSeed` in `appsettings.Local.json`:

- `DevelopmentSeed:AdminPassword`
  Used for the admin account
- `DevelopmentSeed:UserPassword`
  Used for the non-admin seeded accounts

## External Service Notes

Some features depend on third-party services:

- Chat requires a working Sendbird app configuration
- Image upload/display requires a working Azure Blob Storage configuration
- Email delivery is optional for local review if SMTP stays disabled

If those services are not available to reviewers, the rest of the application can still be evaluated, but those specific features will not behave fully without valid credentials.

## Verification Commands

Backend:

```powershell
cd .\KickOffAPI
dotnet build
```

Frontend:

```powershell
cd .\KickOffClient
npm test -- --watch=false
npm run build
```

## Archive Handoff Notes

Before creating the final archive:

- include this repository root with source files
- exclude local secrets such as `KickOffAPI/appsettings.Local.json`
- do not include generated folders such as `KickOffClient/node_modules`, `KickOffClient/dist`, `KickOffAPI/bin`, and `KickOffAPI/obj`
- keep `KickOffAPI/appsettings.Local.example.json` in the archive so reviewers have the config template

The cleanest handoff is to archive a final committed version of the repository rather than a working directory full of local build outputs.
