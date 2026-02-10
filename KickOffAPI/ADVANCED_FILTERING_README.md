# Option C Foundation - Advanced Filtering System

This foundation provides the core components for building a scalable, advanced filtering system with pagination, sorting, and caching.

## Components Created

### 1. **FilterSpecification<T>** (`Specifications/FilterSpecification.cs`)
Base class for building complex queries fluently.

**Key Features:**
- Multiple filter predicates support
- Include navigation properties (eager loading)
- Multi-level sorting
- Pagination with skip/take
- Entity tracking control
- Read-only query optimization

**Key Methods:**
- `AddFilter()` - Add WHERE clause predicates
- `AddInclude()` - Include related entities
- `AddOrderByAscending/Descending()` - Add sorting
- `ApplyPaging()` - Enable pagination
- `DisableTracking()` - Optimize for read-only queries

---

### 2. **ProjectFilterSpecification** (`Specifications/ProjectFilterSpecification.cs`)
Domain-specific specification for Project queries with fluent API.

**Available Filters:**
- `FilterByState()` - Filter by ProjectState enum
- `FilterByOwner()` - Filter by owner email
- `FilterByDateRange()` - Filter by creation date
- `FilterByStates()` - Filter by multiple states

**Available Sorting:**
- `SortByGoalAscending/Descending()`
- `SortByNewest/Oldest()`

**Configuration Methods:**
- `SetPaging()` - Enable pagination
- `OptimizeForReadOnly()` - Disable tracking for read-only queries

---

### 3. **Updated BaseRepository** (`Repositories/BaseRepository.cs`)
Enhanced with specification support.

**New Methods:**
- `GetBySpecificationAsync()` - Execute specification query
- `GetPaginatedBySpecificationAsync()` - Execute with automatic pagination

**Internal:**
- `ApplySpecification()` - Applies filters, sorting, includes, and paging

---

### 4. **PaginatedResult<T>** (`Models/PaginatedResult.cs`)
Wrapper for paginated responses with metadata.

**Properties:**
- `Data` - The items for current page
- `PageNumber`, `PageSize`, `TotalCount`
- `TotalPages` - Calculated
- `HasPreviousPage`, `HasNextPage` - Navigation helpers

---

### 5. **CacheService** (`Services/CacheService.cs`)
Generic distributed caching service.

**Methods:**
- `GetAsync<T>()` - Retrieve cached value
- `SetAsync<T>()` - Store with default/custom expiration
- `RemoveAsync()` - Remove from cache
- `GenerateKey()` - Helper to create consistent cache keys

---

### 6. **Updated ProjectService** (`Services/ProjectService.cs`)
Service layer showing integration patterns.

**New Methods:**
- `GetProjectsByStateAsync()` - Filter by state
- `GetProjectsAsync()` - Advanced query with all filters + pagination
- `SearchProjectsAsync()` - Full-text search by goal

---

## Usage Patterns

### Pattern 1: Simple Filter
```csharp
var spec = new ProjectFilterSpecification();
spec.FilterByState(ProjectState.Active);
spec.OptimizeForReadOnly();

var projects = await repository.GetBySpecificationAsync(spec);
```

### Pattern 2: Complex Query
```csharp
var spec = new ProjectFilterSpecification("marketplace");
spec.FilterByState(ProjectState.Active);
spec.FilterByOwner("user@example.com");
spec.SortByNewest();
spec.OptimizeForReadOnly();

var projects = await repository.GetBySpecificationAsync(spec);
```

### Pattern 3: Paginated Query
```csharp
var spec = new ProjectFilterSpecification();
spec.FilterByState(ProjectState.Active);
spec.SortByNewest();
spec.OptimizeForReadOnly();

var result = await repository.GetPaginatedBySpecificationAsync(spec, pageNumber: 1, pageSize: 20);

// result.Data - items
// result.HasNextPage - navigation
// result.TotalPages - pagination info
```

### Pattern 4: With Caching
```csharp
var cacheKey = CacheService.GenerateKey("projects:active");

var cached = await cache.GetAsync<List<Project>>(cacheKey);
if (cached != null) return cached;

var spec = new ProjectFilterSpecification();
spec.FilterByState(ProjectState.Active);
spec.OptimizeForReadOnly();

var projects = await repository.GetBySpecificationAsync(spec);

await cache.SetAsync(cacheKey, projects, TimeSpan.FromMinutes(60));
return projects;
```

---

## How to Build On This Foundation

### Next Steps:

1. **Create Domain-Specific Specifications**
   - Extend `ProjectFilterSpecification` for common queries
   - Example: `UserActiveProjectsSpecification`, `AllProjectsByOwnerSpecification`

2. **Implement Cache Invalidation**
   - Add cache clearing when entities are updated/deleted
   - Link to repository Update/Delete methods

3. **Add More Filters to ProjectFilterSpecification**
   - `FilterByDescription()` - Search description text
   - `FilterByCollaborator()` - Filter by collaborators
   - `FilterByCreatedAfter()` - Time-based filters

4. **Add Advanced Sorting**
   - Custom sort orders (by number of collaborators, etc.)
   - Implement with `AddOrderByAscending/Descending()`

5. **Implement DTOs**
   - Map `Project` → `ProjectCatalogueDto` at service layer
   - Use `.Select()` in specification or service

6. **Add Request Validation**
   - Validate page numbers, page sizes
   - Validate filter parameters

7. **Error Handling**
   - Handle invalid enums in filter
   - Return proper HTTP responses

8. **Caching Strategy**
   - Cache read-heavy queries
   - Invalidate on write operations
   - Consider cache warming for popular queries

---

## Integration with Program.cs

Add these registrations:

```csharp
// In Program.cs
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<CacheService>();

// For distributed caching (optional)
builder.Services.AddStackExchangeRedisCache(options => 
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));
```

---

## Files Created/Modified

**New Files:**
- `Specifications/FilterSpecification.cs` - Base specification class
- `Specifications/ProjectFilterSpecification.cs` - Project-specific specification
- `Models/PaginatedResult.cs` - Pagination wrapper
- `Services/CacheService.cs` - Caching service
- `Examples/AdvancedFilteringExamples.cs` - Usage examples

**Modified Files:**
- `Repositories/IBaseRepository.cs` - Added specification methods
- `Repositories/BaseRepository.cs` - Implemented specification support
- `Services/ProjectService.cs` - Added example methods

---

## Benefits of This Foundation

✅ **Flexible** - Add new filters/sorts easily  
✅ **Testable** - Specifications can be unit tested independently  
✅ **Maintainable** - Clear separation of concerns  
✅ **Scalable** - Pagination prevents large data transfers  
✅ **Cacheable** - Integrates with caching services  
✅ **Type-safe** - Compile-time checking with expressions  
✅ **Performant** - AsNoTracking() for read-only queries  

---

## Example: Extending With New Features

### Add Date Range Filter to ProjectFilterSpecification:
```csharp
public void FilterByCreatedBetween(DateTime from, DateTime to)
{
    AddFilter(p => p.CreatedAt >= from && p.CreatedAt <= to);
}
```

### Add to Controller:
```csharp
[HttpGet("projects/search")]
public async Task<IActionResult> SearchProjects(
    int pageNumber = 1,
    int pageSize = 20,
    string? state = null,
    string? keyword = null,
    string? owner = null)
{
    var projectState = string.IsNullOrEmpty(state) 
        ? null 
        : Enum.Parse<ProjectState>(state);
    
    var result = await _projectService.GetProjectsAsync(
        pageNumber, pageSize, projectState, keyword, owner);
    
    return Ok(result);
}
```

This foundation is ready to scale with your filtering requirements!
