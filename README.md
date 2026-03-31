# TaskFlow API

A production-ready Task Management RESTful API built with ASP.NET Core 8, following Clean Architecture principles.

## Features

- **Authentication & Authorization** - JWT-based auth with role-based access control
- **Task Management** - Full CRUD with filtering, pagination, and sorting
- **Projects** - Organize tasks into personal or team projects
- **Team Collaboration** - Create teams, manage members, share projects
- **File Attachments** - Upload and manage task attachments
- **Real-Time Notifications** - SignalR hub for live updates
- **Health Monitoring** - Built-in health checks endpoint
- **Rate Limiting** - Configurable request throttling
- **Structured Logging** - Serilog with file and console sinks

## Technology Stack

| Category | Technology |
|----------|------------|
| Framework | ASP.NET Core 8 |
| Database | SQL Server (LocalDB for dev) |
| ORM | Entity Framework Core 8 |
| Authentication | JWT Bearer Tokens |
| Password Hashing | BCrypt |
| Object Mapping | AutoMapper |
| Validation | Data Annotations + FluentValidation |
| Real-Time | SignalR |
| Logging | Serilog |
| Documentation | Swagger/OpenAPI |
| Testing | xUnit, Moq, FluentAssertions |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB included with Visual Studio, or SQL Server Express)
- Visual Studio 2022 / VS Code / Rider

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/taskflow-api.git
cd taskflow-api
```

### 2. Configure the Database

Update the connection string in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskFlowDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 3. Apply Database Migrations

```bash
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7xxx`
- HTTP: `http://localhost:5xxx`

### 5. Access Swagger UI

Open your browser and navigate to:
```
https://localhost:7xxx/swagger
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --verbosity normal

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Test Results:** 93 tests covering services and controllers

## Project Structure

```
JobShadowing/
├── Domain/                 # Core business entities and rules
│   ├── Entities/          # TaskItem, User, Project, Team, etc.
│   ├── Enums/             # UserTaskStatus, UserRole, TeamRole
│   └── Exceptions/        # NotFoundException, ForbiddenException
├── Application/           # Business logic and interfaces
│   ├── Interfaces/        # ITaskService, IAuthService, etc.
│   ├── Services/          # Service implementations
│   ├── DTOs/              # Request/Response models
│   └── Validators/        # FluentValidation rules
├── Infrastructure/        # External concerns
│   ├── Data/              # AppDbContext, configurations
│   ├── Mappings/          # AutoMapper profiles
│   ├── Migrations/        # EF Core migrations
│   └── Services/          # MemoryCacheService
├── API/                   # Web API layer
│   ├── Controllers/       # API endpoints
│   ├── Middleware/        # Exception handling
│   └── Hubs/              # SignalR hubs
├── Models/Settings/       # Configuration classes
├── Tests/                 # Unit tests
└── Program.cs             # Application entry point
```

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and get JWT token |

### Tasks (Requires Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | List tasks (paginated, filtered) |
| GET | `/api/tasks/{id}` | Get task by ID |
| POST | `/api/tasks` | Create a new task |
| PUT | `/api/tasks/{id}` | Update a task |
| PATCH | `/api/tasks/{id}` | Partial update |
| DELETE | `/api/tasks/{id}` | Delete a task |

**Query Parameters for GET /api/tasks:**
- `status` - Filter by status (Pending, InProgress, Completed)
- `search` - Search in title/description
- `sortBy` - Sort field (title, dueDate, createdAt)
- `sortDescending` - Sort direction (true/false)
- `page` - Page number (default: 1)
- `pageSize` - Items per page (default: 10, max: 50)

### Projects (Requires Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/projects` | List user's projects |
| GET | `/api/projects/{id}` | Get project details |
| POST | `/api/projects` | Create a project |
| PUT | `/api/projects/{id}` | Update a project |
| DELETE | `/api/projects/{id}` | Delete a project |
| GET | `/api/projects/{id}/tasks` | Get project tasks |

### Teams (Requires Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/teams` | List user's teams |
| GET | `/api/teams/{id}` | Get team details |
| POST | `/api/teams` | Create a team |
| GET | `/api/teams/{id}/members` | List team members |
| POST | `/api/teams/{id}/members` | Add member to team |
| DELETE | `/api/teams/{id}/members/{userId}` | Remove member |
| GET | `/api/teams/{id}/projects` | List team projects |

### Attachments (Requires Authentication)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks/{taskId}/attachments` | List task attachments |
| POST | `/api/tasks/{taskId}/attachments` | Upload attachment |
| GET | `/api/tasks/{taskId}/attachments/{id}/download` | Download file |
| DELETE | `/api/tasks/{taskId}/attachments/{id}` | Delete attachment |

### System

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check status |
| WS | `/hubs/notifications` | SignalR notification hub |

## Authentication

### Register a User

```bash
curl -X POST https://localhost:7xxx/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!",
    "fullName": "John Doe"
  }'
```

### Login

```bash
curl -X POST https://localhost:7xxx/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!"
  }'
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2026-03-31T15:00:00Z",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "fullName": "John Doe",
    "role": "User"
  }
}
```

### Using the Token

Include the token in the Authorization header:

```bash
curl -X GET https://localhost:7xxx/api/tasks \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your connection string"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyAtLeast32Characters!",
    "Issuer": "TaskFlowApi",
    "Audience": "TaskFlowClient",
    "ExpirationMinutes": 60
  },
  "FileStorage": {
    "StoragePath": "uploads",
    "MaxFileSize": 10485760,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".txt", ".png", ".jpg"]
  },
  "RateLimit": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 10
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowCredentials": true
  }
}
```

## SignalR Notifications

Connect to the SignalR hub at `/hubs/notifications` with your JWT token:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications", {
        accessTokenFactory: () => yourJwtToken
    })
    .build();

// Join a team group
await connection.invoke("JoinTeamGroup", teamId);

// Listen for notifications
connection.on("TaskCreated", (data) => {
    console.log("New task created:", data);
});

connection.on("TaskUpdated", (data) => {
    console.log("Task updated:", data);
});
```

## Error Responses

All errors return a consistent format:

```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "errors": {
    "Email": ["The Email field is required."]
  },
  "traceId": "00-abc123..."
}
```

| Status Code | Description |
|-------------|-------------|
| 400 | Bad Request - Validation errors |
| 401 | Unauthorized - Missing or invalid token |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource doesn't exist |
| 429 | Too Many Requests - Rate limit exceeded |
| 500 | Internal Server Error |

## Logging

Logs are written to:
- **Console** - Real-time development logs
- **File** - `logs/taskflow-{date}.log` (30-day retention)

Log levels can be configured in `appsettings.json`.

## License

This project is licensed under the MIT License.
