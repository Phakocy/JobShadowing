# TaskFlow API - Stage 1
## Project Setup & Basic CRUD Operations

---

## Project Overview: TaskFlow API

**What you're building:** A RESTful API for managing tasks - think of it as the backend for a simple todo/project management app like Todoist or Asana, but stripped down to essentials.

**The core concept:** Users (eventually) can create tasks, view their tasks, update task details, and delete tasks. Right now in Stage 1, we're ignoring users entirely - just focusing on tasks as standalone entities.

**Why this project:**
- Small enough to build quickly, complex enough to learn deeply
- Natural progression from simple to complex (you'll add users, teams, projects later)
- Walks you through .NET conventions

**Stage 1 deliverable:** A working API with 5 endpoints that can create, read, update, and delete tasks, backed by a real database.

---

## Phase 1: Environment Setup (You should already have some of these)

### Install Required Tools

1. **.NET 8 SDK** (latest LTS version)
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: Open terminal and run `dotnet --version`

2. **IDE Choice** (pick one):
   - **Visual Studio 2022 Community**
   - **VS Code** with C# Dev Kit extension (recommended)

3. **Database** (pick one):
   - **SQL Server Express** (most common in .NET world) - LocalDB is fine for now
   - **PostgreSQL** (if you are open small challenges with setup)

4. **API Testing Tool:**
   - Swagger will be built-in (for .Net 8)
   - Postman or Thunder Client (VS Code extension)

### Create the Project

```bash
# Navigate to where you want the project
cd ~/projects

# Create solution (like a Maven/Gradle multi-module project container)
dotnet new sln -n TaskFlowApi

# Create the Web API project (-n is the name of the project, -f is framework, -controllers instead of minimal API-more on this later)
dotnet new webapi -n TaskFlowApi -f net8.0 -controllers

# Add project to solution
dotnet sln add TaskFlowApi/TaskFlowApi.csproj

# Navigate into project
cd TaskFlowApi

# Run it to verify setup
dotnet run
```

**What just happened:**
- `sln` = solution file (like a workspace, can contain multiple projects)
- `webapi` template gives you a pre-configured ASP.NET Core API with Swagger

Visit `https://localhost:<port>/swagger` - you should see a working Swagger UI with a sample WeatherForecast endpoint.

**First checkpoint:** Can you hit the swagger page and see the sample API? Yes? Move on.

---

## Phase 2: Understanding the Project Structure

Open the project and explore what the template gave you:

```
TaskFlowApi/
├── Controllers/
│   └── WeatherForecastController.cs   # Sample controller - you'll create similar
├── Properties/
│   └── launchSettings.json            # Like application.properties but for launch configs
├── appsettings.json                   # THIS is like application.properties
├── appsettings.Development.json       # Environment-specific settings
├── Program.cs                         # Entry point - EVERYTHING starts here
└── TaskFlowApi.csproj                 # Like pom.xml or build.gradle
```

### Key Differences from Spring Boot

| Spring Boot | ASP.NET Core |
|-------------|--------------|
| application.properties | appsettings.json |
| @SpringBootApplication | Program.cs with WebApplication.CreateBuilder() |
| Maven/Gradle deps | NuGet packages in .csproj |
| Annotations everywhere | Minimal - most config in Program.cs |

### Understanding Program.cs

**Open Program.cs** - This is your new best friend. In Java you'd have a main class with `@SpringBootApplication`, here everything bootstraps in this single file:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to DI container (like Spring's @Bean configs)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware pipeline (like Spring's filters)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Mental model:** 
- Top half (before `builder.Build()`) = registering services with DI container
- Bottom half (after) = configuring the HTTP request pipeline

---

## Phase 3: Add Entity Framework Core

### Install Required Packages

```bash
# EF Core packages (like JPA/Hibernate dependencies)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design

# If you chose PostgreSQL instead:
# dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**What these do:**
- `SqlServer` = Database provider (like JDBC driver + Hibernate dialect)
- `Tools` = CLI commands for migrations (like Flyway/Liquibase commands)
- `Design` = Design-time support for EF Core

### Create Your First Entity

Create folder structure (C# convention uses PascalCase for folders):
```bash
mkdir Models
```

Create `Models/TaskItem.cs`:

```csharp
namespace TaskFlowApi.Models
{
    public class TaskItem
    {
        public int Id { get; set; }  // EF Core recognizes "Id" as primary key by convention
        
        public string Title { get; set; } = string.Empty;  // C# 11+ feature: required to avoid nullability warnings
        
        public string? Description { get; set; }  // ? means nullable - new to C# if coming from Java
        
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        
        public DateTime? DueDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum TaskStatus
    {
        Todo = 0,
        InProgress = 1,
        Done = 2
    }
}
```

**Key C# concepts here:**
- `{ get; set; }` = auto-properties (no need for explicit getter/setter methods)
- `string?` = nullable reference type (C# 8+ feature for null safety)
- `= string.Empty` = default value (prevents null warnings)
- Enums use `PascalCase` not `UPPER_SNAKE_CASE` like Java
- No `@Entity` annotation needed - EF Core uses convention over configuration

**Why `TaskItem` not `Task`?** `Task` is a reserved type in C# (used for async operations - you'll see this constantly). Avoid that naming collision.

### Create the DbContext

Create folder and file:
```bash
mkdir Data
```

Create `Data/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TaskFlowApi.Models;

namespace TaskFlowApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        public DbSet<TaskItem> Tasks { get; set; }  // Like @Repository in Spring Data JPA
        
        // Optional: Configure entity behavior
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                
                // Create index on Status for faster queries
                entity.HasIndex(e => e.Status);
            });
        }
    }
}
```

**Java comparison:**
- `DbContext` ≈ EntityManager + Repository combined
- `DbSet<TaskItem>` ≈ JpaRepository<TaskItem, Integer>
- `OnModelCreating` ≈ `@Table`, `@Column` annotations, but centralized and Validations

### Configure Database Connection

Open `appsettings.Development.json` and add:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskFlowDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

**For PostgreSQL, use:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=TaskFlowDb;Username=postgres;Password=yourpassword"
}
```

### Register DbContext in DI Container

Open `Program.cs` and add **before** `var app = builder.Build();`:

```csharp
using Microsoft.EntityFrameworkCore;
using TaskFlowApi.Data;

// Add this line in the services section
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// For PostgreSQL use:
// options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**What this does:** Registers your DbContext with the DI container so it can be injected into controllers (like `@Autowired` in Spring).

### Create and Run Migration

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

**What just happened:**
- EF Core looked at your `TaskItem` entity and DbContext config
- Generated SQL scripts to create the database schema
- Applied those scripts to create the actual database

**Checkpoint:** 
- Did the commands run without errors? 
- Check the `Migrations` folder - you should see generated C# files
- Your database now exists with a `Tasks` table

---

## Phase 4: Build Your First Controller

### Create the Controller

Create `Controllers/TasksController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowApi.Data;
using TaskFlowApi.Models;

namespace TaskFlowApi.Controllers
{
    [Route("api/[controller]")]  // Creates route: /api/tasks
    [ApiController]              // Enables automatic model validation, binding, etc.
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        // Constructor injection (like @Autowired but cleaner)
        public TasksController(AppDbContext context)
        {
            _context = context;
        }
        
        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return await _context.Tasks.ToListAsync();
        }
        
        // GET: api/tasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            
            if (task == null)
            {
                return NotFound();
            }
            
            return task;
        }
        
        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
        {
            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetTask), new { id = taskItem.Id }, taskItem);
        }
        
        // PUT: api/tasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return BadRequest();
            }
            
            taskItem.UpdatedAt = DateTime.UtcNow;
            _context.Entry(taskItem).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            
            return NoContent();
        }
        
        // DELETE: api/tasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
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

### Understanding the Code - Key C# Concepts

#### Async/Await (CRITICAL - different from Java)

```csharp
public async Task<ActionResult<TaskItem>> GetTask(int id)
```

- `async` keyword = this method performs asynchronous operations
- `Task<T>` = like `CompletableFuture<T>` in Java, but more intuitive
- `await` = "pause here until the async operation completes, but don't block the thread"
- In .NET, async is EVERYWHERE in I/O operations (DB, HTTP, files)

**Why async matters:** 
- Spring MVC uses thread-per-request by default
- ASP.NET Core uses async I/O - one thread can handle many requests
- You MUST use async for scalability (not optional like in Java)

#### ActionResult Return Types

```csharp
ActionResult<TaskItem>  // Can return TaskItem OR an HTTP result (NotFound, BadRequest, etc.)
IActionResult           // Returns only HTTP results (no typed data)
```

#### Routing

- `[Route("api/[controller]")]` → replaces `[controller]` with "tasks" (minus "Controller" suffix)
- `[HttpGet("{id}")]` → maps to GET /api/tasks/5
- No need for `@RequestMapping`, `@GetMapping` - cleaner IMO

#### Dependency Injection

```csharp
private readonly AppDbContext _context;

public TasksController(AppDbContext context)
{
    _context = context;
}
```

- Constructor injection is the standard (no `@Autowired` needed)
- `private readonly` = immutable field (like `private final` in Java)
- Underscore prefix `_context` = C# convention for private fields

#### EF Core Methods

- `.FindAsync(id)` = find by primary key (like `findById()` in Spring Data)
- `.ToListAsync()` = execute query and materialize to list
- `.SaveChangesAsync()` = persist changes (like `save()` in JPA)
- `.Remove()` = mark for deletion
- `.Entry().State = Modified` = tell EF to track changes

---

## Phase 5: Test Your API

### Run the Application

```bash
dotnet run
```

Visit: `https://localhost:<port>/swagger`

### Test Each Endpoint

#### 1. Create a Task (POST /api/tasks)

```json
{
  "title": "Learn ASP.NET Core",
  "description": "Complete Stage 1 of TaskFlow API",
  "status": 0,
  "dueDate": "2025-01-15T00:00:00Z"
}
```

**Expected:** 201 Created with the task object including generated `id`

#### 2. Get All Tasks (GET /api/tasks)

**Expected:** 200 OK with array containing your task

#### 3. Get Single Task (GET /api/tasks/1)

**Expected:** 200 OK with the task object

#### 4. Update Task (PUT /api/tasks/1)

```json
{
  "id": 1,
  "title": "Learn ASP.NET Core",
  "description": "Complete Stage 1 of TaskFlow API - UPDATED",
  "status": 1,
  "dueDate": "2025-01-15T00:00:00Z",
  "createdAt": "2025-01-01T10:00:00Z",
  "updatedAt": "2025-01-01T10:00:00Z"
}
```

**Expected:** 204 No Content

#### 5. Delete Task (DELETE /api/tasks/1)

**Expected:** 204 No Content

---

## Common Issues & Solutions

### Issue 1: "Cannot connect to database"

**Solution:** 
- SQL Server: Ensure LocalDB is installed with Visual Studio
- Check connection string matches your setup

### Issue 2: "Migration failed"

**Solution:**
```bash
# Remove migrations
dotnet ef migrations remove

# Delete database
dotnet ef database drop

# Start fresh
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Issue 3: "Null reference warnings everywhere"

**Solution:** This is C#'s nullable reference types. Either:
- Use `string?` for nullable strings
- Use `= string.Empty` for non-nullable strings
- Or disable in .csproj: `<Nullable>disable</Nullable>`

### Issue 4: PUT returns 400 Bad Request

**Cause:** The `id` in the URL doesn't match the `id` in the request body.

**Solution:** Ensure both IDs match when testing.

---

## Challenges to Deepen Learning

Once your API works, try these:

### Challenge 1: Add Filtering

Add query parameters to GET /api/tasks:
- Filter by status: `/api/tasks?status=1`
- Filter by due date range: `/api/tasks?dueBefore=2025-02-01`

**Hint:** Use `[FromQuery]` parameter binding

### Challenge 2: Add Sorting

- `/api/tasks?sortBy=dueDate&sortOrder=desc`

**Hint:** Build dynamic LINQ queries with `.OrderBy()` or `.OrderByDescending()`

### Challenge 3: Add Basic Validation

- Title is required and max 200 characters
- Description max 1000 characters
- DueDate must be in the future for new tasks

**Hint:** Use Data Annotations: `[Required]`, `[MaxLength]`, custom validation attributes

### Challenge 4: Return Different Response Shapes

Create a DTO (Data Transfer Object) that returns only specific fields:

```csharp
public class TaskSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public TaskStatus Status { get; set; }
}
```

Return this instead of full `TaskItem` for the GET all tasks endpoint.

---

## Key Takeaways for Stage 1

### What You Learned

1.  ASP.NET Core project structure and conventions
2.  Entity Framework Core for ORM
3.  Controller-based routing and HTTP verbs
4.  Async/await patterns (critical in .NET)
5.  Dependency injection without annotations
6.  Database migrations

### C# vs Java Mindset Shifts

- Async is non-negotiable for I/O operations
- Less annotation noise, more explicit configuration in Program.cs
- Properties (`{ get; set; }`) instead of getters/setters
- Nullable reference types for better null safety
- LINQ will become your best friend (you'll see more in later stages)

### What You Have Now

A working, database-backed REST API with full CRUD operations. This is your foundation - everything from here builds on this.

---

## Next Steps

Before moving to Stage 2:

1.  Ensure all 5 endpoints work flawlessly
2.  Understand every line of code you wrote (no copy-paste fog)
3.  Complete all Challenges

---

## YouTube Resources for Stage 1

### Core Tutorials

- [**ASP.NET Core Web API Tutorial**](https://www.youtube.com/watch?v=6YIRKBsRWVI)
