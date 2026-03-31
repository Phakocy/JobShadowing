# TaskFlow API - Architecture Documentation

## Overview

TaskFlow API follows **Clean Architecture** principles, ensuring separation of concerns, testability, and maintainability. The architecture is organized into four distinct layers with clear dependency rules.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ Controllers │  │  Middleware │  │  SignalR    │              │
│  │             │  │             │  │  Hubs       │              │
│  └──────┬──────┘  └─────────────┘  └─────────────┘              │
│         │                                                        │
│         ▼                                                        │
├─────────────────────────────────────────────────────────────────┤
│                     Application Layer                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  Services   │  │    DTOs     │  │ Validators  │              │
│  │             │  │             │  │             │              │
│  └──────┬──────┘  └─────────────┘  └─────────────┘              │
│         │                                                        │
│         ▼                                                        │
├─────────────────────────────────────────────────────────────────┤
│                       Domain Layer                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  Entities   │  │    Enums    │  │ Exceptions  │              │
│  │             │  │             │  │             │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │  DbContext  │  │  Mappings   │  │  External   │              │
│  │             │  │             │  │  Services   │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────────────────────────────────────────────────────────────┘
```

## Dependency Flow

```
        ┌─────────┐
        │   API   │
        └────┬────┘
             │
             ▼
     ┌───────────────┐
     │  Application  │◄────────┐
     └───────┬───────┘         │
             │                 │
             ▼                 │
       ┌──────────┐    ┌───────┴───────┐
       │  Domain  │◄───│Infrastructure │
       └──────────┘    └───────────────┘
```

**Rules:**
- Domain has **zero** dependencies
- Application depends only on Domain
- Infrastructure depends on Application and Domain
- API depends on all layers

## Layer Responsibilities

### 1. Domain Layer (`Domain/`)

The innermost layer containing enterprise business rules.

```
Domain/
├── Entities/
│   ├── TaskItem.cs        # Task entity with properties
│   ├── User.cs            # User entity with auth info
│   ├── Project.cs         # Project grouping entity
│   ├── Team.cs            # Team entity
│   ├── TeamMember.cs      # Join table for User-Team
│   └── TaskAttachment.cs  # File attachment entity
├── Enums/
│   ├── UserTaskStatus.cs  # Pending, InProgress, Completed
│   ├── UserRole.cs        # Admin, User
│   └── TeamRole.cs        # Owner, Admin, Member
└── Exceptions/
    ├── NotFoundException.cs
    ├── ForbiddenException.cs
    └── ConflictException.cs
```

**Responsibilities:**
- Define core business entities
- Define domain-specific enums
- Define domain exceptions
- No external dependencies

### 2. Application Layer (`Application/`)

Contains application business rules and orchestration logic.

```
Application/
├── Interfaces/
│   ├── ITaskService.cs
│   ├── IAuthService.cs
│   ├── IProjectService.cs
│   ├── ITeamService.cs
│   ├── INotificationService.cs
│   └── ICacheService.cs
├── Services/
│   ├── TaskService.cs
│   ├── AuthService.cs
│   ├── ProjectService.cs
│   ├── TeamService.cs
│   └── NotificationService.cs
├── DTOs/
│   ├── Auth/
│   │   ├── RegisterRequestDto.cs
│   │   ├── LoginRequestDto.cs
│   │   └── AuthResponseDto.cs
│   ├── Task/
│   │   ├── CreateTaskDto.cs
│   │   ├── UpdateTaskDto.cs
│   │   └── TaskResponseDto.cs
│   ├── Project/
│   │   └── ...
│   └── Team/
│       └── ...
└── Validators/
    ├── CreateTaskDtoValidator.cs
    └── RegisterRequestDtoValidator.cs
```

**Responsibilities:**
- Define service interfaces
- Implement business logic
- Define DTOs for API contracts
- Validation rules
- Depends only on Domain

### 3. Infrastructure Layer (`Infrastructure/`)

Contains implementations for external concerns.

```
Infrastructure/
├── Data/
│   └── AppDbContext.cs       # EF Core DbContext
├── Mappings/
│   └── MappingProfile.cs     # AutoMapper profiles
├── Migrations/
│   └── *.cs                  # EF Core migrations
└── Services/
    └── MemoryCacheService.cs # Cache implementation
```

**Responsibilities:**
- Database access (EF Core)
- Entity configurations
- AutoMapper profiles
- External service implementations
- Depends on Application and Domain

### 4. API Layer (`API/`)

The outermost layer handling HTTP concerns.

```
API/
├── Controllers/
│   ├── AuthController.cs
│   ├── TasksController.cs
│   ├── ProjectsController.cs
│   ├── TeamsController.cs
│   └── AttachmentsController.cs
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs
└── Hubs/
    └── NotificationHub.cs
```

**Responsibilities:**
- HTTP request/response handling
- Route definitions
- Authentication/Authorization attributes
- Exception handling middleware
- SignalR hubs
- Depends on all layers

## Entity Relationship Diagram

```
┌──────────────────┐       ┌──────────────────┐
│      User        │       │      Team        │
├──────────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)          │
│ Email            │       │ Name             │
│ PasswordHash     │◄──────│ OwnerId (FK)     │
│ FullName         │   1:N │ Description      │
│ Role             │       │ CreatedAt        │
│ CreatedAt        │       └────────┬─────────┘
│ UpdatedAt        │                │
└────────┬─────────┘                │
         │                          │
         │ 1:N                      │ 1:N
         ▼                          ▼
┌──────────────────┐       ┌──────────────────┐
│    TaskItem      │       │   TeamMember     │
├──────────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)          │
│ Title            │       │ TeamId (FK)      │
│ Description      │       │ UserId (FK)      │
│ DueDate          │       │ Role             │
│ Status           │       │ JoinedAt         │
│ UserId (FK)      │       └──────────────────┘
│ ProjectId (FK)   │
│ CreatedAt        │
│ UpdatedAt        │
└────────┬─────────┘
         │
         │ 1:N
         ▼
┌──────────────────┐
│  TaskAttachment  │
├──────────────────┤
│ Id (PK)          │
│ TaskId (FK)      │
│ FileName         │
│ StoredFileName   │
│ ContentType      │
│ FileSize         │
│ UploadedAt       │
│ UploadedByUserId │
└──────────────────┘

┌──────────────────┐
│     Project      │
├──────────────────┤
│ Id (PK)          │
│ Name             │
│ Description      │
│ OwnerId (FK)     │──────► User
│ TeamId (FK)      │──────► Team (optional)
│ CreatedAt        │
│ UpdatedAt        │
└──────────────────┘
```

## Relationships Summary

| Relationship | Type | Description |
|--------------|------|-------------|
| User → Tasks | 1:N | User owns many tasks |
| User → Projects | 1:N | User owns many projects |
| User → Teams (Owner) | 1:N | User can own many teams |
| User ↔ Teams (Member) | N:M | Users can be members of many teams (via TeamMember) |
| Project → Tasks | 1:N | Project contains many tasks |
| Team → Projects | 1:N | Team can have many projects |
| Task → Attachments | 1:N | Task can have many attachments |

## Data Flow

### Request Flow (Create Task Example)

```
1. HTTP Request
       │
       ▼
┌──────────────────┐
│  TasksController │ ◄── Validates JWT, extracts UserId
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│   TaskService    │ ◄── Business validation, authorization
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│   AppDbContext   │ ◄── EF Core saves to database
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  AutoMapper      │ ◄── Entity → DTO mapping
└────────┬─────────┘
         │
         ▼
   HTTP Response
```

### Authentication Flow

```
┌─────────┐     ┌──────────────┐     ┌─────────────┐
│ Client  │────►│AuthController│────►│ AuthService │
└─────────┘     └──────────────┘     └──────┬──────┘
                                            │
                      ┌─────────────────────┘
                      │
                      ▼
               ┌─────────────┐
               │  Validate   │
               │ Credentials │
               └──────┬──────┘
                      │
                      ▼
               ┌─────────────┐
               │ Generate    │
               │ JWT Token   │
               └──────┬──────┘
                      │
                      ▼
               ┌─────────────┐
               │  Return     │
               │  Response   │
               └─────────────┘
```

## Middleware Pipeline

```
Request
   │
   ▼
┌─────────────────────────────────┐
│  GlobalExceptionHandler         │ ◄── Catches all exceptions
└─────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────┐
│  Serilog Request Logging        │ ◄── Logs request/response
└─────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────┐
│  CORS                           │ ◄── Cross-origin handling
└─────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────┐
│  Rate Limiter                   │ ◄── Throttles requests
└─────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────┐
│  Authentication                 │ ◄── Validates JWT
└─────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────┐
│  Authorization                  │ ◄── Checks permissions
└─────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────┐
│  Routing / Controllers          │ ◄── Handles request
└─────────────────────────────────┘
   │
   ▼
Response
```

## Service Registration

```csharp
// Program.cs - Dependency Injection Setup

// Application Services (Scoped - per request)
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Infrastructure Services
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddDbContext<AppDbContext>(options => ...);
builder.Services.AddAutoMapper(typeof(MappingProfile));
```

## Security Architecture

### JWT Token Structure

```
Header: { "alg": "HS256", "typ": "JWT" }
Payload: {
  "sub": "1",                    // User ID
  "email": "user@example.com",
  "role": "User",                // User role
  "exp": 1234567890,             // Expiration
  "iss": "TaskFlowApi",          // Issuer
  "aud": "TaskFlowClient"        // Audience
}
Signature: HMACSHA256(...)
```

### Authorization Levels

| Level | Description | Example |
|-------|-------------|---------|
| Anonymous | No auth required | Health check |
| Authenticated | Valid JWT required | List own tasks |
| Resource Owner | Must own resource | Update own task |
| Team Member | Must be in team | Access team project |
| Admin | Admin role required | View all tasks |

## Caching Strategy

```
┌─────────────────┐
│   Controller    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     Cache Hit?     ┌─────────────┐
│  CacheService   │────────────────────│   Return    │
└────────┬────────┘        Yes         │   Cached    │
         │                             └─────────────┘
         │ No
         ▼
┌─────────────────┐
│    Service      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Database      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Store in Cache │
└─────────────────┘
```

**Cache Settings:**
- Default TTL: 5 minutes
- Pattern-based invalidation via `RemoveByPrefix()`

## Real-Time Notifications (SignalR)

```
┌─────────────┐     ┌─────────────────┐     ┌─────────────┐
│   Client    │◄───►│ NotificationHub │◄────│   Service   │
└─────────────┘     └─────────────────┘     └─────────────┘
                           │
                    ┌──────┴──────┐
                    │   Groups    │
                    ├─────────────┤
                    │ team_{id}   │
                    │ project_{id}│
                    │ user_{id}   │
                    └─────────────┘
```

**Notification Events:**
- `TaskCreated` - New task in project
- `TaskUpdated` - Task modified
- `TaskDeleted` - Task removed
- `TaskAssigned` - Task assigned to user
- `MemberAdded` - New team member
- `MemberRemoved` - Member left team

## Testing Architecture

```
Tests/
├── Unit/
│   ├── Services/          # Service logic tests
│   │   ├── AuthServiceTests.cs
│   │   ├── ProjectServiceTests.cs
│   │   └── TeamServiceTests.cs
│   └── Controllers/       # Controller tests with mocked services
│       ├── AuthControllerTests.cs
│       ├── ProjectsControllerTests.cs
│       └── TeamsControllerTests.cs
└── Helpers/
    ├── TestDataBuilder.cs       # Factory for test entities
    └── TestDbContextFactory.cs  # InMemory DB for tests
```

**Testing Patterns:**
- **Unit Tests** - Mock dependencies with Moq
- **Service Tests** - Use InMemory database
- **Controller Tests** - Mock service layer
- **Naming** - `MethodName_Scenario_ExpectedResult`

## Configuration Structure

```
appsettings.json
├── ConnectionStrings
│   └── DefaultConnection
├── JwtSettings
│   ├── SecretKey
│   ├── Issuer
│   ├── Audience
│   └── ExpirationMinutes
├── FileStorage
│   ├── StoragePath
│   ├── MaxFileSize
│   └── AllowedExtensions
├── RateLimit
│   ├── PermitLimit
│   ├── WindowSeconds
│   └── QueueLimit
├── Cors
│   ├── AllowedOrigins
│   └── AllowCredentials
└── Serilog
    └── MinimumLevel
```

## Scalability Considerations

| Concern | Current | Production Ready |
|---------|---------|------------------|
| Database | LocalDB | SQL Server / Azure SQL |
| Caching | In-Memory | Redis |
| File Storage | Local disk | Azure Blob / S3 |
| Logging | File + Console | App Insights / ELK |
| SignalR | In-process | Azure SignalR Service |
