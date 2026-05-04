using System.Text;
using Microsoft.EntityFrameworkCore;

public class ProjectNotificationService(
    ProjectDbContext projectDbContext,
    AppIdentityDbContext identityDbContext,
    IEmailService emailService,
    ClientAppUrlResolver clientAppUrlResolver,
    ILogger<ProjectNotificationService> logger)
{
    private readonly ProjectDbContext _projectDbContext = projectDbContext;
    private readonly AppIdentityDbContext _identityDbContext = identityDbContext;
    private readonly IEmailService _emailService = emailService;
    private readonly ClientAppUrlResolver _clientAppUrlResolver = clientAppUrlResolver;
    private readonly ILogger<ProjectNotificationService> _logger = logger;

    public async Task NotifyProjectUpdatePublishedAsync(
        Project project,
        ProjectUpdate update,
        CancellationToken cancellationToken = default)
    {
        var follows = await _projectDbContext.ProjectFollows
            .AsNoTracking()
            .Where(follow =>
                follow.ProjectId == project.Id &&
                follow.FollowerUserId != update.AuthorUserId)
            .ToListAsync(cancellationToken);

        if (follows.Count == 0)
            return;

        var recipientIds = follows
            .Select(follow => follow.FollowerUserId)
            .Distinct()
            .ToList();

        var recipients = await _identityDbContext.Users
            .AsNoTracking()
            .Where(user => recipientIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                user.Email,
                UserName = user.UserName ?? "there"
            })
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        if (recipients.Count == 0)
            return;

        var projectName = GetProjectName(project);
        var authorName = await ResolveAuthorNameAsync(update.AuthorUserId, cancellationToken);
        var notificationTitle = $"New update from {projectName}";
        var notificationMessage = BuildNotificationMessage(update);

        var inAppNotifications = follows
            .Where(follow =>
                follow.ReceiveInAppNotifications &&
                recipients.ContainsKey(follow.FollowerUserId))
            .Select(follow => new ProjectNotification
            {
                ProjectId = project.Id,
                ProjectUpdateId = update.Id,
                RecipientUserId = follow.FollowerUserId,
                Title = notificationTitle,
                Message = notificationMessage
            })
            .ToList();

        if (inAppNotifications.Count > 0)
        {
            await _projectDbContext.ProjectNotifications.AddRangeAsync(inAppNotifications, cancellationToken);
            await _projectDbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var follow in follows.Where(existingFollow => existingFollow.ReceiveEmailNotifications))
        {
            if (!recipients.TryGetValue(follow.FollowerUserId, out var recipient) ||
                string.IsNullOrWhiteSpace(recipient.Email))
            {
                continue;
            }

            try
            {
                var emailBody = BuildEmailBody(
                    recipient.UserName,
                    authorName,
                    projectName,
                    project.Id,
                    update);

                await _emailService.SendAsync(
                    recipient.Email,
                    notificationTitle,
                    emailBody,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send project update email for project {ProjectId} to follower {FollowerUserId}.",
                    project.Id,
                    follow.FollowerUserId);
            }
        }
    }

    public async Task<ProjectNotificationFeedDto> GetNotificationsAsync(
        string recipientUserId,
        int take = 12,
        CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 30);

        var unreadCount = await _projectDbContext.ProjectNotifications
            .AsNoTracking()
            .CountAsync(notification =>
                notification.RecipientUserId == recipientUserId &&
                !notification.IsRead,
                cancellationToken);

        var notifications = await _projectDbContext.ProjectNotifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(normalizedTake)
            .Select(notification => new ProjectNotificationDto
            {
                Id = notification.Id,
                ProjectId = notification.ProjectId,
                ProjectName = notification.Project!.Headline ?? notification.Project!.Goal,
                ProjectUpdateId = notification.ProjectUpdateId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ProjectNotificationFeedDto
        {
            Notifications = notifications,
            UnreadCount = unreadCount
        };
    }

    public async Task MarkAsReadAsync(
        string notificationId,
        string recipientUserId,
        CancellationToken cancellationToken = default)
    {
        var notificationGuid = ParseGuid(notificationId, "Invalid notification id format.");
        var notification = await _projectDbContext.ProjectNotifications
            .FirstOrDefaultAsync(existingNotification =>
                existingNotification.Id == notificationGuid &&
                existingNotification.RecipientUserId == recipientUserId,
                cancellationToken);

        if (notification == null)
            throw new KeyNotFoundException("Project notification not found.");

        if (notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _projectDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        string recipientUserId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _projectDbContext.ProjectNotifications
            .Where(notification =>
                notification.RecipientUserId == recipientUserId &&
                !notification.IsRead)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
            return;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _projectDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ResolveAuthorNameAsync(string authorUserId, CancellationToken cancellationToken)
    {
        var authorName = await _identityDbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == authorUserId)
            .Select(user => user.UserName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(authorName) ? "The project owner" : authorName;
    }

    private string BuildEmailBody(
        string recipientName,
        string authorName,
        string projectName,
        Guid projectId,
        ProjectUpdate update)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Hi {recipientName},");
        builder.AppendLine();
        builder.AppendLine($"{authorName} published a new update for \"{projectName}\" on KickOff.");
        builder.AppendLine();
        builder.AppendLine($"Title: {update.Title.Trim()}");
        builder.AppendLine();
        builder.AppendLine(update.Content.Trim());

        var projectLink = BuildProjectLink(projectId);
        if (!string.IsNullOrWhiteSpace(projectLink))
        {
            builder.AppendLine();
            builder.AppendLine($"Open the project: {projectLink}");
        }

        builder.AppendLine();
        builder.AppendLine("You are receiving this because you enabled email updates for a followed project.");

        return builder.ToString().Trim();
    }

    private string? BuildProjectLink(Guid projectId)
    {
        var baseUrl = _clientAppUrlResolver.Resolve();
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        return $"{baseUrl}/project/{projectId}";
    }

    private static string GetProjectName(Project project)
    {
        return string.IsNullOrWhiteSpace(project.Headline)
            ? project.Goal
            : project.Headline;
    }

    private static string BuildNotificationMessage(ProjectUpdate update)
    {
        var title = update.Title.Trim();
        var content = update.Content
            .Trim()
            .ReplaceLineEndings(" ");

        if (string.IsNullOrWhiteSpace(content))
            return title;

        var summary = content.Length > 180
            ? $"{content[..177]}..."
            : content;

        return $"{title}: {summary}";
    }

    private static Guid ParseGuid(string value, string errorMessage)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException(errorMessage);

        return guid;
    }
}
