using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using KickOffAPI.Exceptions;
using KickOffAPI.Models;
using KickOffAPI.Services;
using KickOffAPI.Specifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class ProjectService(
    ProjectRepository projectRepository,
    UserManager<ApplicationUser> userManager,
    AppIdentityDbContext identityDbContext,
    ProjectDbContext projectDbContext,
    BlobService blobService,
    ProjectNotificationService projectNotificationService,
    CacheService cacheService,
    ILogger<ProjectService> logger)
{
    private static readonly string[] AllowedCreatorRoles = ["producer", "admin"];
    private const int MaxProjectImageCount = 6;
    private const long MaxProjectImageSizeBytes = 8 * 1024 * 1024;
    private const string ProjectCacheVersionKey = "projects:cache-version";
    private static readonly TimeSpan ProjectCacheVersionLifetime = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions ProjectJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record ProjectUserInfo(string UserName, Guid PublicId);

    private readonly AppIdentityDbContext _identityDbContext = identityDbContext;
    private readonly ProjectDbContext _projectDbContext = projectDbContext;
    private readonly BlobService _blobService = blobService;
    private readonly ProjectNotificationService _projectNotificationService = projectNotificationService;
    private readonly CacheService _cacheService = cacheService;
    private readonly ILogger<ProjectService> _logger = logger;

    public async Task<ProjectDto> CreateFromFormAsync(
        string projectJson,
        string currentUserId,
        List<IFormFile>? imageFiles = null)
    {
        var dto = DeserializeProject(projectJson);
        ValidateProjectModel(dto);
        ValidateProjectRequest(dto, imageFiles);

        var createdProject = await CreateAsync(dto, currentUserId, imageFiles);
        await InvalidateProjectCacheAsync();
        return createdProject;
    }

    public async Task<ProjectDto> UpdateFromFormAsync(
        string projectId,
        string projectJson,
        string currentUserId,
        List<IFormFile>? imageFiles = null)
    {
        var dto = DeserializeProject(projectJson);
        ValidateProjectModel(dto);
        ValidateProjectRequest(dto, imageFiles);

        var updatedProject = await UpdateAsync(projectId, dto, currentUserId, imageFiles);
        await InvalidateProjectCacheAsync();
        return updatedProject;
    }

    public async Task<List<ProjectCatalogueDto>> GetCachedCatalogueAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = await BuildProjectCacheKeyAsync("projects:all", cancellationToken);

        var cached = await _cacheService.GetAsync<List<ProjectCatalogueDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var projects = await GetCatalogueAsync();
        await _cacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(30), cancellationToken);
        return projects;
    }

    public async Task<List<ProjectCatalogueDto>> GetCachedProjectsByStateAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        var parsedState = ParseProjectState(state);
        var cacheKey = await BuildProjectCacheKeyAsync("projects:state", cancellationToken, parsedState);

        var cached = await _cacheService.GetAsync<List<ProjectCatalogueDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var projects = await GetProjectsByStateAsync(parsedState);
        await _cacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(60), cancellationToken);
        return projects;
    }

    public async Task<PaginatedResult<ProjectCatalogueDto>> SearchCachedProjectsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? state = null,
        string? keyword = null,
        string? owner = null,
        bool sortNewest = true,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        state = NormalizeOptionalFilter(state);
        keyword = NormalizeOptionalFilter(keyword);
        owner = NormalizeOptionalFilter(owner);

        ProjectState? parsedState = state == null ? null : ParseProjectState(state);

        var cacheKey = await BuildProjectCacheKeyAsync(
            "projects:search",
            cancellationToken,
            pageNumber,
            pageSize,
            state ?? "all",
            keyword ?? "any",
            owner ?? "any",
            sortNewest ? "newest" : "oldest");

        var cached = await _cacheService.GetAsync<PaginatedResult<ProjectCatalogueDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var result = await GetProjectsAsync(
            pageNumber,
            pageSize,
            parsedState,
            keyword,
            owner,
            sortNewest);

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), cancellationToken);
        return result;
    }

    public async Task<List<ProjectCatalogueDto>> SearchByGoalCachedAsync(
        string keyword,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            throw new ArgumentException("Keyword must be at least 2 characters.");

        keyword = keyword.Trim();
        if (keyword.Length < 2)
            throw new ArgumentException("Keyword must be at least 2 characters.");

        state = NormalizeOptionalFilter(state);
        ProjectState? parsedState = state == null ? null : ParseProjectState(state);

        var cacheKey = await BuildProjectCacheKeyAsync(
            "projects:search-goal",
            cancellationToken,
            keyword.ToLowerInvariant(),
            state ?? "all");

        var cached = await _cacheService.GetAsync<List<ProjectCatalogueDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var projects = await SearchProjectsAsync(keyword, parsedState);
        await _cacheService.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(45), cancellationToken);
        return projects;
    }

    public async Task<PaginatedResult<ProjectCatalogueDto>> GetValidatedPaginatedAsync(
        string? state = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        ValidatePagination(pageNumber, pageSize);

        var normalizedState = NormalizeOptionalFilter(state);
        ProjectState? parsedState = normalizedState == null
            ? null
            : ParseProjectState(normalizedState);

        return await GetProjectsAsync(pageNumber, pageSize, parsedState);
    }

    public Task ClearCachedQueriesAsync(CancellationToken cancellationToken = default)
    {
        return InvalidateProjectCacheAsync(cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, string currentUserId, IEnumerable<IFormFile>? imageFiles = null)
    {
        var user = await GetRequiredUserAsync(currentUserId);
        await EnsureCanCreateProjectAsync(user);

        var uploadedBlobNames = new List<string>();

        try
        {
            uploadedBlobNames = await UploadProjectImagesAsync(imageFiles);
            var finalImageBlobNames = dto.ImageUrls
                .Concat(uploadedBlobNames)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToList();

            EnsureProjectImageCount(finalImageBlobNames.Count);

            var project = new Project
            {
                Headline = dto.Headline,
                ImageUrls = finalImageBlobNames,
                Tags = dto.Tags.ToList(),
                Category = dto.Category,
                Goal = dto.Goal,
                FinancialGoal = dto.FinancialGoal,
                FinancialRaised = 0,
                Problem = dto.Problem,
                Description = dto.Description,
                OwnerId = currentUserId,
                CollaboratorsIdP = dto.CollaboratorsIdP.ToList(),
                Contacts = dto.Contacts.ToList(),
                ExtraInfo = dto.ExtraInfo,
                State = dto.State,
                EndsAt = dto.EndsAt,
                SettingsId = dto.SettingsId
            };

            await projectRepository.AddAsync(project);
            await projectRepository.SaveChangesAsync();

            var userMap = await GetUserMapByIdsAsync([project.OwnerId]);
            var follow = await GetProjectFollowDtoAsync(project.Id, currentUserId);
            return ToDto(project, userMap, [], follow);
        }
        catch
        {
            await CleanupUploadedImagesAsync(uploadedBlobNames);
            throw;
        }
    }

    public async Task<ProjectDto> UpdateAsync(string id, CreateProjectDto dto, string currentUserId, IEnumerable<IFormFile>? imageFiles = null)
    {
        var project = await GetRequiredProjectAsync(id);
        await EnsureCanManageProjectAsync(project, currentUserId);

        var uploadedBlobNames = new List<string>();

        try
        {
            uploadedBlobNames = await UploadProjectImagesAsync(imageFiles);
            var finalImageBlobNames = dto.ImageUrls
                .Concat(uploadedBlobNames)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToList();

            EnsureProjectImageCount(finalImageBlobNames.Count);

            project.Headline = dto.Headline;
            project.ImageUrls = finalImageBlobNames;
            project.Tags = dto.Tags.ToList();
            project.Category = dto.Category;
            project.Goal = dto.Goal;
            project.FinancialGoal = dto.FinancialGoal;
            project.Problem = dto.Problem;
            project.Description = dto.Description;
            project.CollaboratorsIdP = dto.CollaboratorsIdP.ToList();
            project.Contacts = dto.Contacts.ToList();
            project.ExtraInfo = dto.ExtraInfo;
            project.State = dto.State;
            project.EndsAt = dto.EndsAt;
            project.SettingsId = dto.SettingsId;
            project.UpdatedAt = DateTime.UtcNow;

            projectRepository.Update(project);
            await projectRepository.SaveChangesAsync();

            var userMap = await GetUserMapByIdsAsync([project.OwnerId]);
            var updates = await GetProjectUpdateDtosAsync(project.Id);
            var follow = await GetProjectFollowDtoAsync(project.Id, currentUserId);
            return ToDto(project, userMap, updates, follow);
        }
        catch
        {
            await CleanupUploadedImagesAsync(uploadedBlobNames);
            throw;
        }
    }

    public async Task<ProjectDto> GetByIdAsync(string id, string? currentUserId = null)
    {
        var project = await GetRequiredProjectAsync(id);
        var userMap = await GetUserMapByIdsAsync([project.OwnerId]);
        var updates = await GetProjectUpdateDtosAsync(project.Id);
        var follow = await GetProjectFollowDtoAsync(project.Id, currentUserId);

        return ToDto(project, userMap, updates, follow);
    }

    public async Task<List<ProjectUpdateDto>> GetUpdatesAsync(string projectId)
    {
        var project = await GetRequiredProjectAsync(projectId);
        return await GetProjectUpdateDtosAsync(project.Id);
    }

    public async Task<ProjectUpdateDto> CreateUpdateAsync(string projectId, SaveProjectUpdateDto dto, string currentUserId)
    {
        var project = await GetRequiredProjectAsync(projectId);
        await EnsureCanManageProjectAsync(project, currentUserId);

        var update = new ProjectUpdate
        {
            ProjectId = project.Id,
            AuthorUserId = currentUserId,
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        await _projectDbContext.ProjectUpdates.AddAsync(update);
        await _projectDbContext.SaveChangesAsync();
        await _projectNotificationService.NotifyProjectUpdatePublishedAsync(project, update);

        var userMap = await GetUserMapByIdsAsync([currentUserId]);
        return ToProjectUpdateDto(update, userMap);
    }

    public async Task<ProjectUpdateDto> UpdateProjectUpdateAsync(string projectId, string updateId, SaveProjectUpdateDto dto, string currentUserId)
    {
        var project = await GetRequiredProjectAsync(projectId);
        await EnsureCanManageProjectAsync(project, currentUserId);

        var parsedUpdateId = ParseGuid(updateId, "Invalid update id format.");
        var update = await _projectDbContext.ProjectUpdates
            .FirstOrDefaultAsync(existingUpdate => existingUpdate.Id == parsedUpdateId);

        if (update == null || update.ProjectId != project.Id)
            throw new KeyNotFoundException("Project update not found.");

        update.Title = dto.Title.Trim();
        update.Content = dto.Content.Trim();
        update.UpdatedAt = DateTime.UtcNow;

        await _projectDbContext.SaveChangesAsync();

        var userMap = await GetUserMapByIdsAsync([update.AuthorUserId]);
        return ToProjectUpdateDto(update, userMap);
    }

    public async Task DeleteProjectUpdateAsync(string projectId, string updateId, string currentUserId)
    {
        var project = await GetRequiredProjectAsync(projectId);
        await EnsureCanManageProjectAsync(project, currentUserId);

        var parsedUpdateId = ParseGuid(updateId, "Invalid update id format.");
        var update = await _projectDbContext.ProjectUpdates
            .FirstOrDefaultAsync(existingUpdate => existingUpdate.Id == parsedUpdateId);

        if (update == null || update.ProjectId != project.Id)
            throw new KeyNotFoundException("Project update not found.");

        var relatedNotifications = await _projectDbContext.ProjectNotifications
            .Where(notification => notification.ProjectUpdateId == parsedUpdateId)
            .ToListAsync();

        foreach (var notification in relatedNotifications)
            notification.ProjectUpdateId = null;

        _projectDbContext.ProjectUpdates.Remove(update);
        await _projectDbContext.SaveChangesAsync();
    }

    public async Task<List<ProjectCatalogueDto>> GetCatalogueAsync(string? filter = null)
    {
        var projects = !string.IsNullOrWhiteSpace(filter)
            ? await projectRepository.FilterByStateAsync(filter)
            : await projectRepository.GetAllAsync();

        var userMap = await GetUserMapByIdsAsync(projects
            .Select(project => project.OwnerId)
            .Where(ownerId => !string.IsNullOrWhiteSpace(ownerId)));

        return projects
            .Select(project => ToCatalogueDto(project, userMap))
            .ToList();
    }

    public async Task<List<ProjectCatalogueDto>> GetProjectsByStateAsync(ProjectState state)
    {
        var spec = new ProjectFilterSpecification();
        spec.FilterByState(state);
        spec.SortByNewest();
        spec.OptimizeForReadOnly();

        var projects = await projectRepository.GetBySpecificationAsync(spec);
        var userMap = await GetUserMapByIdsAsync(projects
            .Select(project => project.OwnerId)
            .Where(ownerId => !string.IsNullOrWhiteSpace(ownerId)));

        return projects
            .Select(project => ToCatalogueDto(project, userMap))
            .ToList();
    }

    public async Task<PaginatedResult<ProjectCatalogueDto>> GetProjectsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        ProjectState? state = null,
        string? goalKeyword = null,
        string? owner = null,
        bool sortNewest = true)
    {
        var spec = new ProjectFilterSpecification(goalKeyword);

        if (state.HasValue)
            spec.FilterByState(state.Value);

        if (!string.IsNullOrWhiteSpace(owner))
            spec.FilterByOwner(owner);

        if (sortNewest)
            spec.SortByNewest();
        else
            spec.SortByOldest();

        spec.SetPaging(pageNumber, pageSize);
        spec.OptimizeForReadOnly();

        var paged = await projectRepository
            .GetPaginatedBySpecificationAsync(spec, pageNumber, pageSize);

        var userMap = await GetUserMapByIdsAsync(paged.Data
            .Select(project => project.OwnerId)
            .Where(ownerId => !string.IsNullOrWhiteSpace(ownerId)));

        return new PaginatedResult<ProjectCatalogueDto>
        {
            Data = paged.Data
                .Select(project => ToCatalogueDto(project, userMap))
                .ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount
        };
    }

    public async Task<List<ProjectCatalogueDto>> SearchProjectsAsync(
        string goalKeyword,
        ProjectState? state = null)
    {
        var spec = new ProjectFilterSpecification(goalKeyword);

        if (state.HasValue)
            spec.FilterByState(state.Value);

        spec.SortByGoalAscending();
        spec.OptimizeForReadOnly();

        var projects = await projectRepository.GetBySpecificationAsync(spec);
        var userMap = await GetUserMapByIdsAsync(projects
            .Select(project => project.OwnerId)
            .Where(ownerId => !string.IsNullOrWhiteSpace(ownerId)));

        return projects
            .Select(project => ToCatalogueDto(project, userMap))
            .ToList();
    }

    private static CreateProjectDto DeserializeProject(string projectJson)
    {
        var dto = JsonSerializer.Deserialize<CreateProjectDto>(projectJson, ProjectJsonOptions);
        if (dto == null)
            throw new JsonException("Invalid project payload.");

        return dto;
    }

    private static void ValidateProjectModel(CreateProjectDto dto)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);

        if (Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true))
            return;

        var errors = validationResults
            .SelectMany(validationResult =>
            {
                var memberNames = validationResult.MemberNames.Any()
                    ? validationResult.MemberNames
                    : [string.Empty];

                return memberNames.Select(memberName => new
                {
                    MemberName = memberName,
                    ErrorMessage = validationResult.ErrorMessage ?? "The value is invalid."
                });
            })
            .GroupBy(entry => entry.MemberName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(entry => entry.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        throw new RequestValidationException(errors);
    }

    private static void ValidateProjectRequest(CreateProjectDto dto, List<IFormFile>? files)
    {
        var existingImageCount = dto.ImageUrls
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .Count();

        var newImageCount = files?.Count ?? 0;
        if (existingImageCount + newImageCount > MaxProjectImageCount)
            throw new InvalidOperationException($"A project can have at most {MaxProjectImageCount} images.");

        if (files == null || files.Count == 0)
            return;

        foreach (var file in files)
        {
            if (file.Length == 0)
                throw new InvalidOperationException("One of the files is empty.");

            if (file.Length > MaxProjectImageSizeBytes)
                throw new InvalidOperationException("Each image must be 8MB or smaller.");

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only image uploads are allowed.");
        }
    }

    private async Task<Project> GetRequiredProjectAsync(string id)
    {
        var guid = ParseGuid(id, "Invalid id format.");
        var project = await projectRepository.GetByIdAsync(guid);

        if (project == null)
            throw new KeyNotFoundException("Project not found.");

        return project;
    }

    private static Guid ParseGuid(string value, string errorMessage)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException(errorMessage);

        return guid;
    }

    private static ProjectState ParseProjectState(string value)
    {
        if (!Enum.TryParse<ProjectState>(value, true, out var parsedState))
            throw new ArgumentException("Invalid state value.");

        return parsedState;
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Invalid pagination parameters.");
    }

    private static string? NormalizeOptionalFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private async Task<ApplicationUser> GetRequiredUserAsync(string currentUserId)
    {
        var user = await userManager.FindByIdAsync(currentUserId);

        if (user == null)
        {
            _logger.LogWarning("Project operation failed because user {UserId} could not be loaded.", currentUserId);
            throw new UnauthorizedAccessException("User not found.");
        }

        return user;
    }

    private async Task EnsureCanCreateProjectAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var canCreateProject = roles.Any(role =>
            AllowedCreatorRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

        if (!canCreateProject)
        {
            _logger.LogWarning(
                "Project creation forbidden for user {UserId}. Roles={Roles}",
                user.Id,
                string.Join(", ", roles));
            throw new UnauthorizedAccessException("Only producers and admins can create projects.");
        }
    }

    private async Task EnsureCanManageProjectAsync(Project project, string currentUserId)
    {
        var currentUser = await GetRequiredUserAsync(currentUserId);
        var roles = await userManager.GetRolesAsync(currentUser);
        var isAdmin = roles.Any(role => role.Equals("admin", StringComparison.OrdinalIgnoreCase));
        var isOwner = string.Equals(project.OwnerId, currentUserId, StringComparison.Ordinal);

        if (!isOwner && !isAdmin)
        {
            _logger.LogWarning(
                "Project management forbidden for project {ProjectId}. User={UserId}, Owner={OwnerId}, Roles={Roles}",
                project.Id,
                currentUserId,
                project.OwnerId,
                string.Join(", ", roles));
            throw new UnauthorizedAccessException("Only the project owner or an admin can manage this project.");
        }
    }

    private async Task<Dictionary<string, ProjectUserInfo>> GetUserMapByIdsAsync(IEnumerable<string> userIds)
    {
        var ownerIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (ownerIds.Count == 0)
            return new Dictionary<string, ProjectUserInfo>();

        var userMap = await _identityDbContext.Users
            .AsNoTracking()
            .Where(user => ownerIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                UserName = user.UserName ?? "Unknown",
                user.PublicId
            })
            .ToDictionaryAsync(
                user => user.Id,
                user => new ProjectUserInfo(user.UserName, user.PublicId));

        foreach (var ownerId in ownerIds)
            userMap.TryAdd(ownerId, new ProjectUserInfo("Unknown", Guid.Empty));

        return userMap;
    }

    private async Task<List<string>> UploadProjectImagesAsync(IEnumerable<IFormFile>? imageFiles)
    {
        var uploadedBlobNames = new List<string>();

        if (imageFiles == null)
            return uploadedBlobNames;

        foreach (var file in imageFiles)
        {
            var blobName = await _blobService.UploadProjectImage(file);
            uploadedBlobNames.Add(blobName);
        }

        return uploadedBlobNames;
    }

    private async Task CleanupUploadedImagesAsync(IEnumerable<string> uploadedBlobNames)
    {
        foreach (var blobName in uploadedBlobNames)
            await _blobService.Delete(blobName);
    }

    private async Task<List<ProjectUpdateDto>> GetProjectUpdateDtosAsync(Guid projectId)
    {
        var updates = await _projectDbContext.ProjectUpdates
            .AsNoTracking()
            .Where(update => update.ProjectId == projectId)
            .OrderByDescending(update => update.CreatedAt)
            .ThenByDescending(update => update.Id)
            .ToListAsync();

        var userMap = await GetUserMapByIdsAsync(updates.Select(update => update.AuthorUserId));

        return updates
            .Select(update => ToProjectUpdateDto(update, userMap))
            .ToList();
    }

    private async Task<ProjectFollowDto> GetProjectFollowDtoAsync(Guid projectId, string? currentUserId)
    {
        var followersCount = await _projectDbContext.ProjectFollows
            .AsNoTracking()
            .CountAsync(follow => follow.ProjectId == projectId);

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return new ProjectFollowDto
            {
                FollowersCount = followersCount
            };
        }

        var follow = await _projectDbContext.ProjectFollows
            .AsNoTracking()
            .FirstOrDefaultAsync(existingFollow =>
                existingFollow.ProjectId == projectId &&
                existingFollow.FollowerUserId == currentUserId);

        return new ProjectFollowDto
        {
            FollowersCount = followersCount,
            IsFollowing = follow != null,
            ReceiveInAppNotifications = follow?.ReceiveInAppNotifications ?? true,
            ReceiveEmailNotifications = follow?.ReceiveEmailNotifications ?? true
        };
    }

    private static void EnsureProjectImageCount(int imageCount)
    {
        if (imageCount > MaxProjectImageCount)
            throw new InvalidOperationException($"A project can have at most {MaxProjectImageCount} images.");
    }

    private ProjectCatalogueDto ToCatalogueDto(Project project, Dictionary<string, ProjectUserInfo> userMap)
    {
        var firstImageBlobName = project.ImageUrls.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        var ownerInfo = userMap.TryGetValue(project.OwnerId, out var resolvedOwner)
            ? resolvedOwner
            : new ProjectUserInfo("Unknown", Guid.Empty);

        return new ProjectCatalogueDto
        {
            Id = project.Id,
            Name = project.Goal,
            Description = project.Description,
            Owner = ownerInfo.UserName,
            State = project.State.ToString(),
            FinancialGoal = project.FinancialGoal,
            FinancialRaised = project.FinancialRaised,
            EndDate = project.EndsAt,
            ImageUrl = firstImageBlobName != null
                ? _blobService.GetReadSasUrl(firstImageBlobName)
                : null
        };
    }

    private ProjectDto ToDto(
        Project project,
        Dictionary<string, ProjectUserInfo> userMap,
        ICollection<ProjectUpdateDto> updates,
        ProjectFollowDto follow)
    {
        var ownerInfo = userMap.TryGetValue(project.OwnerId, out var resolvedOwner)
            ? resolvedOwner
            : new ProjectUserInfo("Unknown", Guid.Empty);

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Headline ?? project.Goal,
            Headline = project.Headline,
            Goal = project.Goal,
            Description = project.Description,
            State = project.State,
            Owner = ownerInfo.UserName,
            OwnerId = project.OwnerId,
            OwnerPublicId = ownerInfo.PublicId,
            Category = project.Category,
            FinancialGoal = project.FinancialGoal,
            Problem = project.Problem,
            CollaboratorsIdP = project.CollaboratorsIdP.ToList(),
            Contacts = project.Contacts.ToList(),
            ImageUrls = project.ImageUrls
                .Select(_blobService.GetReadSasUrl)
                .ToList(),
            ImageBlobNames = project.ImageUrls.ToList(),
            Tags = project.Tags.ToList(),
            BackerIds = project.BackerIds.ToList(),
            Updates = updates,
            Follow = follow,
            ExtraInfo = project.ExtraInfo,
            StartDate = project.CreatedAt,
            EndDate = project.EndsAt,
            SettingsId = project.SettingsId
        };
    }

    private static ProjectUpdateDto ToProjectUpdateDto(ProjectUpdate update, Dictionary<string, ProjectUserInfo> userMap)
    {
        var authorInfo = userMap.TryGetValue(update.AuthorUserId, out var resolvedAuthor)
            ? resolvedAuthor
            : new ProjectUserInfo("Unknown", Guid.Empty);

        return new ProjectUpdateDto
        {
            Id = update.Id,
            ProjectId = update.ProjectId,
            Title = update.Title,
            Content = update.Content,
            AuthorUserId = update.AuthorUserId,
            AuthorName = authorInfo.UserName,
            CreatedAt = update.CreatedAt,
            UpdatedAt = update.UpdatedAt,
            IsEdited = update.UpdatedAt > update.CreatedAt.AddSeconds(1)
        };
    }

    private async Task<string> BuildProjectCacheKeyAsync(
        string prefix,
        CancellationToken cancellationToken,
        params object[] values)
    {
        var version = await GetProjectCacheVersionAsync(cancellationToken);
        var keyValues = new object[values.Length + 1];
        keyValues[0] = version;
        Array.Copy(values, 0, keyValues, 1, values.Length);
        return CacheService.GenerateKey(prefix, keyValues);
    }

    private async Task<string> GetProjectCacheVersionAsync(CancellationToken cancellationToken)
    {
        var version = await _cacheService.GetAsync<string>(ProjectCacheVersionKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(version))
            return version;

        version = Guid.NewGuid().ToString("N");
        await _cacheService.SetAsync(ProjectCacheVersionKey, version, ProjectCacheVersionLifetime, cancellationToken);
        return version;
    }

    private Task InvalidateProjectCacheAsync(CancellationToken cancellationToken = default)
    {
        var nextVersion = Guid.NewGuid().ToString("N");
        return _cacheService.SetAsync(ProjectCacheVersionKey, nextVersion, ProjectCacheVersionLifetime, cancellationToken);
    }
}
