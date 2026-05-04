using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace KickOffAPI.Data.Seeders;

public static class ProjectDbSeeder
{
    private static readonly ProjectSeedTemplate[] ProjectTemplates =
    [
        new(
            "Atlas AI Ops",
            "Build an AI operations workspace for support teams",
            "Atlas AI Ops helps support teams triage, summarize, and automate repetitive tickets without losing human oversight or compliance visibility.",
            "Artificial Intelligence",
            185000m,
            92000m,
            "Customer support teams lose hours every day manually routing, summarizing, and escalating repeat issues.",
            ["ai", "automation", "support-tech"],
            "Phase 1 covers shared inbox automation, a response playbook engine, and analytics for leadership reviews.",
            ProjectState.Active,
            150,
            3,
            5),
        new(
            "PocketCare",
            "Launch a proactive care companion for chronic patients",
            "PocketCare gives patients gentle medication reminders, symptom tracking, and clinician-ready summaries so care teams can intervene sooner.",
            "HealthTech",
            240000m,
            126000m,
            "Patients managing chronic conditions often juggle fragmented tools, leading to missed medication routines and low-quality progress notes.",
            ["health", "mobile", "care-coordination"],
            "The pilot focuses on diabetes and hypertension workflows with nurse dashboards for remote follow-up.",
            ProjectState.Active,
            210,
            2,
            6),
        new(
            "LedgerLoop",
            "Create a cashflow command center for founders",
            "LedgerLoop turns accounting feeds into simple runway, tax, and payment health insights so small teams can make faster operating decisions.",
            "FinTech",
            160000m,
            54000m,
            "Early-stage founders rarely have a clear, always-current view of runway, burn, and upcoming obligations.",
            ["finance", "analytics", "startup-tools"],
            "Current scope includes bank sync, runway scenarios, and weekly digest reports for founders and operators.",
            ProjectState.OnHold,
            120,
            2,
            4),
        new(
            "CivicGrid",
            "Build a city-ready civic engagement platform",
            "CivicGrid helps municipalities collect neighborhood proposals, prioritize them transparently, and keep residents informed as projects move forward.",
            "GovTech",
            300000m,
            118000m,
            "Residents struggle to see how local ideas are prioritized, funded, and delivered after they are submitted.",
            ["civic", "community", "public-sector"],
            "The initial rollout targets district budgeting, proposal voting, and milestone updates with accessibility-first design.",
            ProjectState.Proposed,
            240,
            4,
            5),
        new(
            "ForgeLearn",
            "Ship a skills-first cohort learning platform",
            "ForgeLearn combines guided sprints, peer feedback, and mentor check-ins to help learners finish real projects instead of passive coursework.",
            "EdTech",
            140000m,
            87000m,
            "Online learners often enroll enthusiastically but abandon programs when structure, accountability, and feedback are missing.",
            ["education", "community", "coaching"],
            "The curriculum launch includes design, frontend, and product strategy tracks with mentor office hours.",
            ProjectState.Active,
            175,
            3,
            5),
        new(
            "HarvestLink",
            "Connect local producers with nearby buyers",
            "HarvestLink shortens the gap between farms and restaurants by making inventory updates, ordering, and delivery coordination simple and reliable.",
            "AgriTech",
            125000m,
            125000m,
            "Small farms and local restaurants still rely on scattered calls and spreadsheets to coordinate supply and delivery windows.",
            ["supply-chain", "food", "marketplace"],
            "The completed MVP proved weekly ordering, route planning, and buyer alerts across two regional markets.",
            ProjectState.Completed,
            -30,
            2,
            7),
        new(
            "SignalNest",
            "Prototype a lightweight IoT safety network",
            "SignalNest links low-cost sensors to a monitoring dashboard so facilities teams can detect leaks, heat spikes, and equipment anomalies earlier.",
            "IoT",
            275000m,
            61000m,
            "Small facilities operators lack affordable, centralized visibility into environmental and equipment risks.",
            ["iot", "hardware", "monitoring"],
            "This prototype covers water, temperature, and vibration sensors with mobile alerts for on-call teams.",
            ProjectState.Inactive,
            95,
            2,
            4),
        new(
            "StudioSprint",
            "Rebuild freelance operations for creative studios",
            "StudioSprint gives creative teams one place to handle briefs, timelines, approvals, and invoicing without bouncing between disconnected tools.",
            "CreativeOps",
            190000m,
            47000m,
            "Small agencies lose margin and momentum when project handoffs, approvals, and invoices live in separate systems.",
            ["creative", "workflow", "collaboration"],
            "The cancellation review highlighted strong demand for client approvals but weak retention in invoicing workflows.",
            ProjectState.Cancelled,
            -12,
            3,
            3)
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var projectRepository = scope.ServiceProvider.GetRequiredService<ProjectRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if ((await projectRepository.GetAllAsync()).Any())
            return;

        var producers = await dbContext.Users
            .Where(u => dbContext.UserRoles
                .Any(ur => ur.UserId == u.Id && dbContext.Roles
                    .Any(r => r.Id == ur.RoleId && r.Name == "Producer")))
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var users = await dbContext.Users
            .Where(u => dbContext.UserRoles
                .Any(ur => ur.UserId == u.Id && dbContext.Roles
                    .Any(r => r.Id == ur.RoleId && r.Name != "Admin")))
            .OrderBy(u => u.UserName)
            .ToListAsync();

        if (!producers.Any())
            throw new Exception("No users found to assign as project owner");

        if (users.Count < 4)
            throw new Exception("Need at least four non-admin users to seed projects with collaborators and backers.");

        var random = new Random();
        var backedUserIds = new HashSet<string>(StringComparer.Ordinal);

        Console.WriteLine($"\nSeeding {ProjectTemplates.Length} projects with full non-image field coverage...\n");

        for (int i = 0; i < ProjectTemplates.Length; i++)
        {
            var template = ProjectTemplates[i];
            var owner = producers[i % producers.Count];

            var collaborators = users
                .Where(user => user.Id != owner.Id)
                .OrderBy(_ => random.Next())
                .Take(Math.Min(template.CollaboratorCount, users.Count - 1))
                .ToList();

            var extraBackers = users
                .Where(user => user.Id != owner.Id)
                .OrderBy(_ => random.Next())
                .Take(Math.Max(0, Math.Min(template.BackerCount - 1, users.Count - 1)))
                .Select(user => user.Id)
                .ToList();

            foreach (var backerId in extraBackers)
            {
                backedUserIds.Add(backerId);
            }

            var contacts = new List<string>
            {
                owner.Email ?? $"{owner.UserName}@kickoff.test",
                $"https://kickoff.test/projects/{CreateSlug(template.Headline)}"
            };

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Headline = template.Headline,
                ImageUrls = [],
                Tags = template.Tags.ToList(),
                Category = template.Category,
                Goal = template.Goal,
                FinancialGoal = template.FinancialGoal,
                FinancialRaised = template.FinancialRaised,
                Problem = template.Problem,
                Description = template.Description,
                OwnerId = owner.Id,
                CollaboratorsIdP = collaborators
                    .Select(user => user.PublicId.ToString())
                    .ToList(),
                Contacts = contacts,
                ExtraInfo = template.ExtraInfo,
                State = template.State,
                UpdatedAt = DateTime.UtcNow,
                EndsAt = DateTime.UtcNow.AddDays(template.EndOffsetDays),
                SettingsId = Guid.NewGuid(),
                BackerIds = new List<string> { owner.Id }
            };

            foreach (var backerId in extraBackers)
            {
                if (!project.BackerIds.Contains(backerId))
                    project.BackerIds.Add(backerId);
            }

            await projectRepository.AddAsync(project);

            Console.WriteLine($"+ Created project '{template.Headline}' assigned to {owner.UserName}");
        }

        await projectRepository.SaveChangesAsync();

        foreach (var backer in users.Where(user => backedUserIds.Contains(user.Id)))
        {
            var roles = await userManager.GetRolesAsync(backer);
            if (roles.Any(role => role.Equals("Backer", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var roleResult = await userManager.AddToRoleAsync(backer, "Backer");
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                throw new Exception($"Failed to assign Backer role to {backer.UserName}: {errors}");
            }
        }

        Console.WriteLine($"\nAll {ProjectTemplates.Length} projects seeded successfully.\n");
    }

    private static string CreateSlug(string value)
    {
        return value
            .ToLowerInvariant()
            .Replace(" ", "-", StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal);
    }

    private sealed record ProjectSeedTemplate(
        string Headline,
        string Goal,
        string Description,
        string Category,
        decimal FinancialGoal,
        decimal FinancialRaised,
        string Problem,
        string[] Tags,
        string ExtraInfo,
        ProjectState State,
        int EndOffsetDays,
        int CollaboratorCount,
        int BackerCount);
}
