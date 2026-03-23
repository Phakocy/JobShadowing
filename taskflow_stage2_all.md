# TaskFlow API - Stage 2 Deep Dive
## Data Validation, DTOs & Error Handling

---

## Stage Overview

**What you're building on:** In Stage 1, you created a working CRUD API. It works, but it's raw - it accepts any data, returns entire entities, and error messages are generic.

**What you'll add in Stage 2:**
- **DTOs (Data Transfer Objects)** - Control exactly what data goes in and comes out
- **Data Validation** - Ensure data integrity before it hits the database
- **Global Error Handling** - Centralized, consistent error responses
- **AutoMapper** - Automatically convert between entities and DTOs

**Why this matters:**
- **Security:** Never expose your database entities directly to clients
- **API Stability:** Change your database without breaking API contracts
- **User Experience:** Clear validation messages instead of database errors
- **Maintainability:** Separation of concerns between data layer and API layer

**Stage 2 deliverable:** A production-ready API with proper validation, clean request/response models, and professional error handling.

---

## Phase 1: Understanding DTOs (30 minutes)

### The Problem with Current Approach

Right now, your API returns `TaskItem` entities directly:

```csharp
// Current approach - PROBLEMS:
[HttpPost]
public async Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
{
    // Client can set Id, CreatedAt, UpdatedAt - they shouldn't!
    // Client must send all properties, even ones they shouldn't touch
    _context.Tasks.Add(taskItem);
    await _context.SaveChangesAsync();
    return CreatedAtAction(nameof(GetTask), new { id = taskItem.Id }, taskItem);
}
```

**Issues:**
1. Client can send `Id = 999` in POST request (should be auto-generated)
2. Client can manipulate `CreatedAt` and `UpdatedAt` timestamps
3. When you add `UserId` later, clients will see all users' data
4. If you add a `DeletedAt` field, clients will see soft-deleted records
5. Database schema changes break API contracts

### The DTO Solution

**DTO Pattern:**
- **Request DTOs:** What clients send TO your API
- **Response DTOs:** What your API sends BACK to clients
- **Entities:** What you store in the database

```
Client Request → CreateTaskDto → TaskItem Entity → Database
Database → TaskItem Entity → TaskResponseDto → Client Response
```

### Create the DTOs Folder Structure

```bash
mkdir DTOs
```

### Create Request DTOs

Create `DTOs/CreateTaskDto.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using TaskFlowApi.Models;

namespace TaskFlowApi.DTOs
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
        
        [EnumDataType(typeof(TaskStatus), ErrorMessage = "Invalid status value")]
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        
        [DataType(DataType.DateTime)]
        [FutureDate(ErrorMessage = "Due date must be in the future")]
        public DateTime? DueDate { get; set; }
    }
}
```

Create `DTOs/UpdateTaskDto.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using TaskFlowApi.Models;

namespace TaskFlowApi.DTOs
{
    public class UpdateTaskDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
        
        [EnumDataType(typeof(TaskStatus), ErrorMessage = "Invalid status value")]
        public TaskStatus Status { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }
    }
}
```

### Create Response DTOs

Create `DTOs/TaskResponseDto.cs`:

```csharp
using TaskFlowApi.Models;

namespace TaskFlowApi.DTOs
{
    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;  // Enum as readable string
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsOverdue { get; set; }  // Computed property
    }
}
```

Create `DTOs/TaskSummaryDto.cs`:

```csharp
using TaskFlowApi.Models;

namespace TaskFlowApi.DTOs
{
    public class TaskSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }
}
```

**Key observations:**
- No `Id` in `CreateTaskDto` - it's auto-generated
- No `CreatedAt`/`UpdatedAt` in request DTOs - server controls these
- Response DTOs can include computed properties like `IsOverdue`
- `StatusDisplay` makes enums more readable for frontend
- Summary DTO only returns essential fields for list views

---

## Phase 2: Custom Validation Attributes (20 minutes)

### Create Custom Validators Folder

```bash
mkdir Validators
```

### Create Future Date Validator

Create `Validators/FutureDateAttribute.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace TaskFlowApi.Validators
{
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                // Nullable dates are allowed
                return ValidationResult.Success;
            }
            
            if (value is DateTime dateTime)
            {
                if (dateTime.Date < DateTime.UtcNow.Date)
                {
                    return new ValidationResult(
                        ErrorMessage ?? "Date must be in the future");
                }
                return ValidationResult.Success;
            }
            
            return new ValidationResult("Invalid date format");
        }
    }
}
```

**How it works:**
- Inherits from `ValidationAttribute`
- `IsValid` method performs the validation logic
- Returns `ValidationResult.Success` if valid
- Returns `ValidationResult` with error message if invalid
- Respects the `ErrorMessage` property from the attribute usage

**Usage:**
```csharp
[FutureDate(ErrorMessage = "Due date must be in the future")]
public DateTime? DueDate { get; set; }
```

### Challenge: Create More Validators

Try creating these on your own:

**PastDateAttribute** - Ensures date is in the past:
```csharp
[PastDate(ErrorMessage = "Date must be in the past")]
public DateTime? CompletedDate { get; set; }
```

**NoWeekendAttribute** - Ensures date is not Saturday/Sunday:
```csharp
[NoWeekend(ErrorMessage = "Due date cannot be on a weekend")]
public DateTime? DueDate { get; set; }
```

---

## Phase 3: Install and Configure AutoMapper (30 minutes)

### Why AutoMapper?

**Without AutoMapper:**
```csharp
var responseDto = new TaskResponseDto
{
    Id = taskItem.Id,
    Title = taskItem.Title,
    Description = taskItem.Description,
    Status = taskItem.Status,
    StatusDisplay = taskItem.Status.ToString(),
    DueDate = taskItem.DueDate,
    CreatedAt = taskItem.CreatedAt,
    UpdatedAt = taskItem.UpdatedAt,
    IsOverdue = taskItem.DueDate.HasValue && taskItem.DueDate.Value < DateTime.UtcNow
};
```

**With AutoMapper:**
```csharp
var responseDto = _mapper.Map<TaskResponseDto>(taskItem);
```

Much cleaner, especially when you have dozens of DTOs!

### Install AutoMapper

```bash
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

### Create Mapping Profile

Create `Mappings/MappingProfile.cs`:

```csharp
using AutoMapper;
using TaskFlowApi.DTOs;
using TaskFlowApi.Models;

namespace TaskFlowApi.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity to Response DTO
            CreateMap<TaskItem, TaskResponseDto>()
                .ForMember(dest => dest.StatusDisplay, 
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.IsOverdue,
                    opt => opt.MapFrom(src => src.DueDate.HasValue && src.DueDate.Value < DateTime.UtcNow));
            
            // Entity to Summary DTO
            CreateMap<TaskItem, TaskSummaryDto>()
                .ForMember(dest => dest.IsOverdue,
                    opt => opt.MapFrom(src => src.DueDate.HasValue && src.DueDate.Value < DateTime.UtcNow));
            
            // Create DTO to Entity
            CreateMap<CreateTaskDto, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            
            // Update DTO to Entity
            CreateMap<UpdateTaskDto, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}
```

**Understanding the mapping:**
- `CreateMap<Source, Destination>()` - defines a mapping
- `ForMember()` - customizes how individual properties are mapped
- `opt.Ignore()` - don't map this property
- `opt.MapFrom()` - use custom logic to populate this property
- Properties with same names map automatically

### Register AutoMapper in DI

Open `Program.cs` and add:

```csharp
using TaskFlowApi.Mappings;

// Add after AddDbContext, before var app = builder.Build();
builder.Services.AddAutoMapper(typeof(MappingProfile));
```

**What this does:** Scans the assembly for classes inheriting from `Profile` and registers all mappings with the DI container.

---

## Phase 4: Global Error Handling (40 minutes)

### The Problem with Current Error Handling

Right now, errors look like this:

**Database error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 500,
  "errors": {
    "": ["SqlException: Cannot insert duplicate key..."]
  }
}
```

**Issues:**
- Exposes database internals
- Inconsistent format
- Not user-friendly
- No correlation IDs for debugging

### Create Error Response Models

Create `DTOs/ErrorResponse.cs`:

```csharp
namespace TaskFlowApi.DTOs
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? DetailedMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? TraceId { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
```

### Create Global Exception Handler Middleware

Create `Middleware/GlobalExceptionHandlerMiddleware.cs`:

```csharp
using System.Net;
using System.Text.Json;
using TaskFlowApi.DTOs;

namespace TaskFlowApi.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next, 
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new ErrorResponse
            {
                TraceId = context.TraceIdentifier
            };

            switch (exception)
            {
                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    response.DetailedMessage = _env.IsDevelopment() ? exception.Message : null;
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid request";
                    response.DetailedMessage = _env.IsDevelopment() ? exception.Message : null;
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An error occurred while processing your request";
                    response.DetailedMessage = _env.IsDevelopment() ? exception.Message : null;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
```

**Key concepts:**
- Middleware wraps the entire request pipeline in try-catch
- Different exception types map to different HTTP status codes
- Detailed errors only shown in Development environment
- All errors include TraceId for debugging
- Consistent JSON response format

### Register Middleware

Open `Program.cs` and add BEFORE other middleware:

```csharp
using TaskFlowApi.Middleware;

// Add this FIRST in the middleware pipeline, right after var app = builder.Build();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
```

**Order matters!** This middleware must be first to catch all exceptions.

### Handle Validation Errors

ASP.NET Core automatically validates DTOs, but we need to customize the response format.

Add to `Program.cs` in the services section:

```csharp
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value?.Errors.Select(x => x.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        var errorResponse = new ErrorResponse
        {
            StatusCode = 400,
            Message = "Validation failed",
            Errors = errors,
            TraceId = context.HttpContext.TraceIdentifier
        };

        return new BadRequestObjectResult(errorResponse);
    };
});
```

**Don't forget to add the using:**
```csharp
using Microsoft.AspNetCore.Mvc;
using TaskFlowApi.DTOs;
```

---

## Phase 5: Refactor Controller with DTOs (45 minutes)

### Update TasksController

Replace your entire `Controllers/TasksController.cs` with:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using TaskFlowApi.Data;
using TaskFlowApi.DTOs;
using TaskFlowApi.Models;

namespace TaskFlowApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            AppDbContext context, 
            IMapper mapper,
            ILogger<TasksController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/tasks
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TaskSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TaskSummaryDto>>> GetTasks(
            [FromQuery] TaskStatus? status = null,
            [FromQuery] bool? isOverdue = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "asc")
        {
            _logger.LogInformation("Getting tasks with filters - Status: {Status}, IsOverdue: {IsOverdue}", 
                status, isOverdue);

            var query = _context.Tasks.AsQueryable();

            // Apply filters
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (isOverdue.HasValue && isOverdue.Value)
            {
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow);
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(t => t.Title)
                    : query.OrderBy(t => t.Title),
                "duedate" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate),
                "status" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.Status)
                    : query.OrderBy(t => t.Status),
                _ => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt)
            };

            var tasks = await query.ToListAsync();
            var taskDtos = _mapper.Map<IEnumerable<TaskSummaryDto>>(tasks);

            return Ok(taskDtos);
        }

        // GET: api/tasks/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            _logger.LogInformation("Getting task with ID: {TaskId}", id);

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            var taskDto = _mapper.Map<TaskResponseDto>(task);
            return Ok(taskDto);
        }

        // POST: api/tasks
        [HttpPost]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskResponseDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            _logger.LogInformation("Creating new task: {Title}", createTaskDto.Title);

            var taskItem = _mapper.Map<TaskItem>(createTaskDto);
            
            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();

            var taskDto = _mapper.Map<TaskResponseDto>(taskItem);

            return CreatedAtAction(
                nameof(GetTask), 
                new { id = taskItem.Id }, 
                taskDto);
        }

        // PUT: api/tasks/5
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskResponseDto>> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            _logger.LogInformation("Updating task with ID: {TaskId}", id);

            var existingTask = await _context.Tasks.FindAsync(id);
            
            if (existingTask == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for update", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            // Map DTO to existing entity (AutoMapper updates the entity)
            _mapper.Map(updateTaskDto, existingTask);
            existingTask.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    throw new KeyNotFoundException($"Task with ID {id} not found");
                }
                throw;
            }

            var taskDto = _mapper.Map<TaskResponseDto>(existingTask);
            return Ok(taskDto);
        }

        // DELETE: api/tasks/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            _logger.LogInformation("Deleting task with ID: {TaskId}", id);

            var task = await _context.Tasks.FindAsync(id);
            
            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}
```

### What Changed?

**Before (Stage 1):**
- Accepted/returned `TaskItem` entities
- No filtering or sorting
- Generic error messages
- No logging

**After (Stage 2):**
- Uses DTOs for all inputs/outputs
- Query parameters for filtering and sorting
- Throws meaningful exceptions (caught by middleware)
- Structured logging
- `ProducesResponseType` attributes for Swagger documentation
- AutoMapper handles all conversions

---

## Phase 6: Test Your Enhanced API (30 minutes)

### Run and Test

```bash
dotnet run
```

Visit Swagger: `https://localhost:<port>/swagger`

### Test Validation

**1. Try creating task with empty title:**

```json
{
  "title": "",
  "description": "This should fail",
  "status": 0
}
```

**Expected Response (400):**
```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "timestamp": "2025-01-23T10:30:00Z",
  "traceId": "0HN1234567890",
  "errors": {
    "Title": ["Title must be between 1 and 200 characters"]
  }
}
```

**2. Try creating task with past due date:**

```json
{
  "title": "Valid Title",
  "dueDate": "2020-01-01T00:00:00Z"
}
```

**Expected Response (400):**
```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "errors": {
    "DueDate": ["Due date must be in the future"]
  }
}
```

**3. Try title that's too long:**

```json
{
  "title": "This is a very long title that exceeds the maximum allowed length of 200 characters... [continue to 250+ chars]"
}
```

**Expected:** Validation error for max length

### Test Filtering and Sorting

**1. Create multiple tasks with different statuses:**

```json
// Task 1
{
  "title": "Task A - Todo",
  "status": 0,
  "dueDate": "2025-02-01T00:00:00Z"
}

// Task 2
{
  "title": "Task B - In Progress",
  "status": 1,
  "dueDate": "2025-01-25T00:00:00Z"
}

// Task 3
{
  "title": "Task C - Done",
  "status": 2
}
```

**2. Test filtering:**
- `GET /api/tasks?status=1` - Only returns InProgress tasks
- `GET /api/tasks?isOverdue=true` - Only returns overdue tasks

**3. Test sorting:**
- `GET /api/tasks?sortBy=title&sortOrder=asc` - Alphabetically A-Z
- `GET /api/tasks?sortBy=dueDate&sortOrder=desc` - Latest due date first

**4. Combine filters:**
- `GET /api/tasks?status=0&sortBy=dueDate&sortOrder=asc`

### Test Error Handling

**1. Request non-existent task:**

`GET /api/tasks/999`

**Expected Response (404):**
```json
{
  "statusCode": 404,
  "message": "Resource not found",
  "detailedMessage": "Task with ID 999 not found",
  "timestamp": "2025-01-23T10:35:00Z",
  "traceId": "0HN1234567891"
}
```

**2. Update non-existent task:**

`PUT /api/tasks/999`

**Expected:** Same 404 error with trace ID

### Test Response Structure

**Create a task and verify response:**

```json
{
  "title": "Test Response Structure",
  "description": "Checking the response DTO",
  "status": 0,
  "dueDate": "2025-02-15T00:00:00Z"
}
```

**Expected Response (201):**
```json
{
  "id": 1,
  "title": "Test Response Structure",
  "description": "Checking the response DTO",
  "status": 0,
  "statusDisplay": "Todo",
  "dueDate": "2025-02-15T00:00:00Z",
  "createdAt": "2025-01-23T10:40:00Z",
  "updatedAt": "2025-01-23T10:40:00Z",
  "isOverdue": false
}
```

**Verify:**
- ✅ `statusDisplay` shows "Todo" (enum converted to string)
- ✅ `isOverdue` is computed correctly
- ✅ `createdAt` and `updatedAt` are auto-generated
- ✅ All timestamps in UTC

---

## Common Issues & Solutions

### Issue 1: AutoMapper throws "Missing type map configuration"

**Cause:** Forgot to register AutoMapper or mapping profile is incorrect.

**Solution:**
```bash
# Verify registration in Program.cs
builder.Services.AddAutoMapper(typeof(MappingProfile));

# Ensure MappingProfile inherits from Profile
public class MappingProfile : Profile
```

### Issue 2: Validation not triggering

**Cause:** Missing `[ApiController]` attribute or validation attributes.

**Solution:**
```csharp
[ApiController]  // This is required for automatic validation
[Route("api/[controller]")]
public class TasksController : ControllerBase
```

### Issue 3: Errors still showing database details

**Cause:** Exception handler not registered first in pipeline.

**Solution:** Ensure this is FIRST in `Program.cs`:
```csharp
var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();  // MUST BE FIRST
app.UseSwagger();
// ... other middleware
```

### Issue 4: "The instance of entity type cannot be tracked"

**Cause:** Trying to update a detached entity.

**Solution:** Fetch existing entity first, then map:
```csharp
var existingTask = await _context.Tasks.FindAsync(id);
_mapper.Map(updateTaskDto, existingTask);  // Map to existing tracked entity
```

### Issue 5: Custom validation attribute not working

**Cause:** Forgot to use the attribute or incorrect implementation.

**Solution:**
```csharp
// In DTO
[FutureDate(ErrorMessage = "Due date must be in the future")]
public DateTime? DueDate { get; set; }

// In attribute
protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
{
    // Your logic
}
```

---

## Challenges to Deepen Learning

### Challenge 1: Add Pagination

Implement pagination for GET /api/tasks:
- `GET /api/tasks?page=1&pageSize=10`
- Return `PagedResponse<TaskSummaryDto>` with metadata:

```csharp
public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
```

**Hint:** Use `.Skip()` and `.Take()` with LINQ

### Challenge 2: Add Search Functionality

Add text search across title and description:
- `GET /api/tasks?search=meeting`

**Hint:** Use `.Where(t => t.Title.Contains(search) || t.Description.Contains(search))`

### Challenge 3: Create a PATCH Endpoint

Instead of PUT (full replacement), implement PATCH (partial update):
- Only update fields that are provided
- Use JSON Patch or custom logic