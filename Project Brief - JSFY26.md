# TaskFlow API - Project Brief
## Building a Production-Ready Task Management API with Clean Architecture

---

## Project Overview

**What you're building:** A complete RESTful API for task management that follows Clean Architecture principles, implements authentication/authorization, supports team collaboration, and includes comprehensive unit tests.

**Technologies:**
- ASP.NET Core 8 Web API
- Entity Framework Core
- SQL Server or PostgreSQL
- JWT Authentication
- xUnit for testing
- AutoMapper
- FluentValidation (optional alternative to Data Annotations)

**Architecture:** Clean Architecture with clear separation between Domain, Application, Infrastructure, and API layers.

---

## Stage 3: Authentication & Authorization

### Problem Statement

Your API currently has no security. Anyone can create, read, update, or delete any task. You need to implement user authentication and ensure users can only access their own tasks.

### Objectives

1. Implement user registration with secure password hashing
2. Implement login functionality that returns JWT tokens
3. Protect all task endpoints - users must be authenticated
4. Ensure users can only see and modify their own tasks
5. Add role-based authorization (Admin vs Regular User)

### Requirements

**User Entity:**
- Email (unique identifier)
- Hashed password (never store plain text)
- Full name
- Role (Admin or User)
- Created/Updated timestamps

**Registration Endpoint:**
- POST /api/auth/register
- Validate email format and uniqueness
- Enforce password strength requirements (minimum 8 characters, must include uppercase, lowercase, number, special character)
- Hash password before storing
- Return success message (not the JWT token)

**Login Endpoint:**
- POST /api/auth/login
- Validate credentials against hashed password
- Generate JWT token with user claims (UserId, Email, Role)
- Return token and user info (excluding password)

**Protected Endpoints:**
- All task endpoints require valid JWT token
- Tasks must be scoped to the authenticated user
- Admin users can see all tasks

**Authorization Rules:**
- Regular users: CRUD operations only on their own tasks
- Admin users: Can view all tasks, delete any task
- Unauthenticated requests: Return 401 Unauthorized
- Insufficient permissions: Return 403 Forbidden

### Solving Approach Highlights

**Password Security:**
- Use BCrypt.Net-Next or ASP.NET Core Identity's PasswordHasher
- Never log or return passwords in responses
- Implement password complexity validation

**JWT Token Generation:**
- Include minimal claims: UserId, Email, Role
- Set appropriate expiration (15-60 minutes for access tokens)
- Sign with a secret key stored in configuration (not hardcoded)
- Consider refresh token strategy for production

**Middleware Configuration:**
- Register authentication middleware before authorization
- Configure JWT bearer token validation
- Set up authentication scheme and default challenge

**Data Scoping:**
- Add UserId foreign key to TaskItem entity
- Modify all queries to filter by current user
- Extract current user identity from HttpContext

**Testing Strategy:**
- Unit test password hashing and verification
- Test JWT token generation and validation
- Test authorization policies
- Mock authentication for controller tests

### Suggested Resources

**YouTube:**
- "JWT Authentication in ASP.NET Core" by Mohamad Lawand
- "ASP.NET Core Identity from Scratch" by Raw Coding
- "Role-Based Authorization" by Nick Chapsas
- "Password Hashing in .NET" by IAmTimCorey

**Documentation:**
- Microsoft Docs: Authentication and Authorization in ASP.NET Core
- JWT.io - Understanding JWT structure
- OWASP Password Storage Cheat Sheet

**NuGet Packages:**
- Microsoft.AspNetCore.Authentication.JwtBearer
- BCrypt.Net-Next
- System.IdentityModel.Tokens.Jwt

---

## Stage 4: Team Collaboration & Relationships

### Problem Statement

Tasks currently exist in isolation. You need to support projects and teams so users can collaborate on shared tasks while maintaining proper access control.

### Objectives

1. Implement Projects to group related tasks
2. Implement Teams where multiple users collaborate
3. Establish proper entity relationships
4. Handle cascade delete scenarios
5. Support team-based access control

### Requirements

**Project Entity:**
- Name, Description
- Owner (User who created it)
- Team (optional - projects can be personal or team-owned)
- Created/Updated timestamps

**Team Entity:**
- Name, Description
- Members (many-to-many with Users)
- Owner (User who created the team)
- Created timestamp

**Relationships:**
- User 1-to-Many Tasks (existing)
- User 1-to-Many Projects (as owner)
- User 1-to-Many Teams (as owner)
- User Many-to-Many Teams (as member)
- Project 1-to-Many Tasks
- Team 1-to-Many Projects (optional team projects)

**Access Control Rules:**
- Personal tasks: Only creator can access
- Personal projects: Only owner can access tasks within
- Team projects: All team members can access tasks within
- Team owners can add/remove members
- Deleting a project doesn't delete tasks (set ProjectId to null or implement soft delete)
- Removing user from team revokes access to team projects

**Endpoints Required:**
- POST /api/projects - Create project
- GET /api/projects - List user's projects (personal + team projects)
- GET /api/projects/{id}/tasks - Get tasks in a project
- POST /api/teams - Create team
- POST /api/teams/{id}/members - Add member to team
- DELETE /api/teams/{id}/members/{userId} - Remove member
- GET /api/teams/{id}/projects - Get team's projects

### Solving Approach Highlights

**Entity Framework Relationships:**
- Use navigation properties to define relationships
- Configure cascade behaviors in DbContext.OnModelCreating
- Use `.Include()` to eagerly load related entities
- Consider query performance with `.AsNoTracking()` for read-only operations

**Many-to-Many Implementation:**
- Create join entity (TeamMember) with UserId and TeamId
- Include additional properties (DateJoined, Role within team)
- Configure relationship in DbContext

**Authorization Complexity:**
- Create custom authorization handlers
- Implement resource-based authorization
- Check team membership before allowing access
- Consider creating a service layer for complex business rules

**Query Optimization:**
- Avoid N+1 queries by using `.Include()` and `.ThenInclude()`
- Project to DTOs early to limit data retrieval
- Add database indexes on foreign keys

**Testing Strategy:**
- Test relationship configurations
- Test cascade delete behaviors
- Test access control for different user-team-project scenarios
- Test edge cases (user removed from team, project deleted, etc.)

### Suggested Resources

**YouTube:**
- "Entity Framework Relationships" by Teddy Smith
- "Many-to-Many Relationships in EF Core" by Milan Jovanović
- "Resource-Based Authorization" by Nick Chapsas
- "LINQ Performance Tips" by Raw Coding

**Documentation:**
- EF Core Relationships Documentation
- ASP.NET Core Resource-Based Authorization
- EF Core Query Performance Best Practices

---

## Stage 5: Clean Architecture Refactoring

### Problem Statement

Your current code has controllers directly accessing DbContext, business logic mixed with API concerns, and no clear separation of responsibilities. This makes testing difficult and violates SOLID principles.

### Objectives

1. Restructure the solution into Clean Architecture layers
2. Implement Repository pattern
3. Introduce service layer for business logic
4. Apply Dependency Inversion principle
5. Make the codebase testable and maintainable

### Requirements

**Solution Structure:**
```
TaskFlowApi.Domain/          (Core business entities, enums, exceptions)
TaskFlowApi.Application/     (Interfaces, DTOs, business logic, validators)
TaskFlowApi.Infrastructure/  (EF Core, repositories, external services)
TaskFlowApi.API/            (Controllers, middleware, Program.cs)
TaskFlowApi.Tests/          (Unit and integration tests)
```

**Domain Layer (Innermost):**
- Contains entities: TaskItem, User, Project, Team, TeamMember
- Contains domain exceptions: TaskNotFoundException, UnauthorizedAccessException
- Contains enums: TaskStatus, UserRole
- No dependencies on other layers
- Pure business models with no infrastructure concerns

**Application Layer:**
- Contains interfaces: ITaskRepository, IUserRepository, IProjectRepository, ITeamRepository
- Contains service interfaces: ITaskService, IAuthService, IProjectService
- Contains DTOs: Request and Response models
- Contains validators: FluentValidation rules
- Contains business logic implementations
- Depends only on Domain layer

**Infrastructure Layer:**
- Contains DbContext and EF Core configurations
- Contains repository implementations
- Contains external service integrations (email, file storage, etc.)
- Depends on Domain and Application layers

**API Layer (Outermost):**
- Contains controllers (thin, only orchestration)
- Contains middleware
- Contains dependency injection configuration
- Depends on all other layers

**Dependency Flow:**
- API → Application → Domain
- Infrastructure → Application → Domain
- Domain has ZERO dependencies

**Repository Pattern Requirements:**
- Generic repository with common operations (GetById, GetAll, Add, Update, Delete)
- Specific repositories inherit from generic (TaskRepository, UserRepository)
- Repositories return domain entities, not DTOs
- Support for complex queries and filtering
- Async operations throughout

**Service Layer Requirements:**
- Services contain business logic, not repositories
- Services validate business rules
- Services coordinate between multiple repositories
- Services return DTOs to API layer
- Services throw domain exceptions on errors

**Key Refactoring Tasks:**
- Move entity classes to Domain project
- Create repository interfaces in Application
- Implement repositories in Infrastructure
- Create service layer for business operations
- Update controllers to use services instead of DbContext
- Configure dependency injection for all layers

### Solving Approach Highlights

**Project References:**
- API references Application and Infrastructure
- Application references only Domain
- Infrastructure references Application and Domain
- Tests reference all projects

**Repository Pattern:**
- Define generic base interface for common CRUD
- Create specific interfaces for specialized queries
- Implement using EF Core DbContext
- Keep repositories focused on data access only

**Service Layer:**
- Inject repositories via constructor
- Validate business rules before calling repository
- Map entities to DTOs using AutoMapper
- Handle exceptions and return appropriate responses

**Dependency Injection:**
- Register repositories as Scoped (per request lifetime)
- Register services as Scoped
- Register AutoMapper profiles
- Configure in Program.cs with extension methods

**Testing Benefits:**
- Mock repositories easily in service tests
- Mock services easily in controller tests
- Test business logic independently of infrastructure
- Test API layer independently of database

### Suggested Resources

**YouTube:**
- "Clean Architecture in ASP.NET Core" by Milan Jovanović (complete series)
- "Repository Pattern in .NET" by Teddy Smith
- "SOLID Principles Explained" by IAmTimCorey
- "Dependency Injection Deep Dive" by Nick Chapsas

**Documentation:**
- Clean Architecture by Robert C. Martin (book/articles)
- Microsoft Docs: Dependency Injection in ASP.NET Core
- Microsoft Docs: Project Structure Best Practices

**Articles:**
- "Clean Architecture with ASP.NET Core" on Microsoft DevBlogs
- "The Repository Pattern" by Martin Fowler
- "Screaming Architecture" by Uncle Bob

---

## Stage 6: Comprehensive Unit Testing

### Problem Statement

You have production code with no automated tests. You need comprehensive test coverage to ensure code quality, prevent regressions, and enable confident refactoring.

### Objectives

1. Write unit tests for all service layer business logic
2. Write unit tests for repository implementations
3. Write controller tests with mocked dependencies
4. Achieve minimum 80% code coverage
5. Implement test-driven development mindset

### Requirements

**Test Project Structure:**
```
TaskFlowApi.Tests/
├── Unit/
│   ├── Services/
│   │   ├── TaskServiceTests.cs
│   │   ├── AuthServiceTests.cs
│   │   ├── ProjectServiceTests.cs
│   │   └── TeamServiceTests.cs
│   ├── Repositories/
│   │   └── TaskRepositoryTests.cs
│   └── Controllers/
│       └── TasksControllerTests.cs
├── Integration/
│   └── TasksEndpointTests.cs
└── Helpers/
    └── TestDataBuilder.cs
```

**Testing Frameworks:**
- xUnit as test framework
- Moq for mocking dependencies
- FluentAssertions for readable assertions
- EF Core InMemory provider for repository tests

**Service Layer Tests:**
- Test business logic validation
- Test successful operations
- Test error handling and exceptions
- Test authorization checks
- Mock repository dependencies
- Verify correct repository methods are called

**Repository Tests:**
- Use InMemory database provider
- Test CRUD operations
- Test complex queries and filters
- Test relationship loading
- Test concurrent operations
- Verify data integrity

**Controller Tests:**
- Mock service layer
- Test HTTP status codes
- Test request/response DTOs
- Test model validation
- Test authorization attributes
- Verify service methods are called with correct parameters

**Test Categories to Cover:**

**Authentication Service:**
- Registration with valid data succeeds
- Registration with existing email fails
- Login with correct credentials returns token
- Login with wrong password fails
- Password hashing works correctly
- JWT token contains correct claims

**Task Service:**
- Create task assigns to current user
- User can only retrieve their own tasks
- Update task verifies ownership
- Delete task verifies ownership
- Admin can access all tasks
- Filtering and sorting work correctly

**Project Service:**
- Create project assigns owner
- Team members can access team projects
- Non-members cannot access team projects
- Deleting project handles tasks appropriately

**Team Service:**
- Add member to team succeeds
- Only owner can add/remove members
- Removing member revokes project access
- User can list their teams

**Test Data Builders:**
- Create reusable test data builders
- Build valid entities with sensible defaults
- Allow customization for specific test scenarios
- Use builder pattern for readability

### Solving Approach Highlights

**Mocking with Moq:**
- Mock interfaces, not concrete classes
- Set up expected method calls with specific parameters
- Verify methods were called correct number of times
- Return predefined data from mocked methods

**Arrange-Act-Assert Pattern:**
- Arrange: Set up test data, configure mocks
- Act: Execute the method under test
- Assert: Verify expected outcomes and behaviors

**Test Naming Convention:**
- MethodName_Scenario_ExpectedResult
- Example: CreateTask_ValidData_ReturnsTaskDto
- Example: UpdateTask_UnauthorizedUser_ThrowsException

**InMemory Database for Repositories:**
- Create new DbContext instance per test
- Seed test data
- Execute repository operation
- Query database to verify changes
- Dispose context after test

**Testing Async Code:**
- Use async test methods
- Await operations properly
- Test cancellation tokens where applicable

**FluentAssertions Examples:**
- result.Should().NotBeNull()
- result.Should().BeOfType<TaskResponseDto>()
- exception.Should().BeOfType<UnauthorizedException>()
- tasks.Should().HaveCount(3)
- result.Title.Should().Be("Expected Title")

**Code Coverage:**
- Use coverlet for coverage reporting
- Run: dotnet test /p:CollectCoverage=true
- Aim for 80%+ coverage in service and repository layers
- Focus on business logic, not boilerplate

### Suggested Resources

**YouTube:**
- "Unit Testing in C#" by IAmTimCorey (comprehensive)
- "Mocking with Moq" by Raw Coding
- "Testing ASP.NET Core APIs" by Nick Chapsas
- "TDD in .NET" by Milan Jovanović
- "FluentAssertions Tutorial" by Code Opinion

**Documentation:**
- xUnit Documentation
- Moq Quick Start Guide
- FluentAssertions Documentation
- Microsoft Docs: Testing in ASP.NET Core
- EF Core Testing Documentation

**Articles:**
- "Unit Testing Best Practices" by Microsoft
- "The Art of Unit Testing" (book by Roy Osherove)
- "Test Driven Development" by Kent Beck

**NuGet Packages:**
- xUnit
- xUnit.runner.visualstudio
- Moq
- FluentAssertions
- Microsoft.EntityFrameworkCore.InMemory
- coverlet.collector

---

## Stage 7: Advanced Features & Polish (Optional)

### Problem Statement

Your API works but lacks production-ready features like file uploads, real-time updates, performance optimization, and proper API documentation.

### Objectives

1. Add file attachment capability to tasks
2. Implement real-time notifications with SignalR
3. Optimize query performance
4. Add comprehensive API documentation with Swagger
5. Implement health checks and monitoring

### Requirements

**File Upload (Task Attachments):**
- Users can attach files to tasks (PDFs, images, documents)
- Store files in cloud storage (Azure Blob Storage or AWS S3)
- Store file metadata in database (filename, size, URL, upload date)
- Support multiple attachments per task
- Validate file types and size limits
- Return secure, time-limited URLs for downloads
- DELETE attachment when task is deleted

**Real-Time Notifications (SignalR):**
- Notify team members when task is created in shared project
- Notify when task status changes
- Notify when task is assigned to user
- Notify when comment is added (if implementing comments)
- Users receive notifications only for relevant events
- Support browser and mobile clients

**Performance Optimization:**
- Add database indexes on frequently queried columns
- Implement response caching for read-heavy endpoints
- Use pagination for all list endpoints
- Implement select projection to reduce data transfer
- Add query performance logging
- Consider Redis for distributed caching

**API Documentation:**
- Complete Swagger/OpenAPI documentation
- Document all endpoints with descriptions
- Include request/response examples
- Document error responses
- Group endpoints by feature area
- Add authentication documentation
- Version your API (v1, v2)

**Health Checks & Monitoring:**
- Database connectivity health check
- External services health check (blob storage, etc.)
- Memory and CPU usage monitoring
- Expose /health endpoint
- Log request/response times
- Implement application insights or similar

**Additional Features to Consider:**
- Task comments/discussion thread
- Task priority levels
- Recurring tasks
- Email notifications
- Activity audit log
- Export tasks to CSV/Excel
- Task templates

### Solving Approach Highlights

**File Upload:**
- Use IFormFile for file upload
- Validate file before uploading to cloud
- Generate unique filenames to prevent collisions
- Store cloud URL in database, not local path
- Implement streaming upload for large files
- Return CDN URLs for faster access

**SignalR Implementation:**
- Create SignalR Hub with typed methods
- Use groups for team-based broadcasting
- Add user to groups on connection
- Send notifications from service layer after data changes
- Handle connection lifecycle (connect, disconnect, reconnect)

**Database Indexing:**
- Index foreign keys automatically created by EF Core
- Add composite indexes for common query patterns
- Index columns used in WHERE, ORDER BY, JOIN
- Monitor slow queries and add indexes accordingly
- Use database profiling tools

**Caching Strategy:**
- Cache static/rarely changing data (user profiles, team info)
- Use cache-aside pattern
- Set appropriate expiration times
- Invalidate cache on updates
- Consider distributed cache for scaled deployments

**Swagger Documentation:**
- Use XML comments for endpoint descriptions
- Add ProducesResponseType attributes
- Configure Swagger UI theme
- Add authorization UI
- Include examples using SwaggerRequestExample

**Monitoring and Logging:**
- Use Serilog for structured logging
- Log at appropriate levels (Information, Warning, Error)
- Include correlation IDs for request tracking
- Never log sensitive data (passwords, tokens)
- Set up log aggregation (Azure App Insights, ELK stack)

### Suggested Resources

**YouTube:**
- "File Upload in ASP.NET Core" by Bhrugen Patel
- "Azure Blob Storage with .NET" by Les Jackson
- "SignalR in ASP.NET Core" by Raw Coding
- "Performance Optimization" by Nick Chapsas
- "Swagger/OpenAPI Documentation" by Milan Jovanović
- "Health Checks in ASP.NET Core" by IAmTimCorey

**Documentation:**
- SignalR Documentation (Microsoft)
- Azure Blob Storage SDK for .NET
- EF Core Performance Best Practices
- Swagger/OpenAPI Specification
- ASP.NET Core Health Checks

**NuGet Packages:**
- Microsoft.AspNetCore.SignalR
- Azure.Storage.Blobs (or AWSSDK.S3)
- Swashbuckle.AspNetCore
- Microsoft.Extensions.Diagnostics.HealthChecks
- Serilog.AspNetCore
- Microsoft.Extensions.Caching.StackExchangeRedis

---

## Final Project Deliverables

### Code Requirements

1. Complete solution following Clean Architecture
2. All compulsory stages implemented and integrated
3. Comprehensive unit test suite (80%+ coverage)
4. Well-structured Git repository with meaningful commits
5. README with setup instructions and API documentation

### Documentation Requirements

1. **README.md:**
   - Project overview and features
   - Technology stack
   - Setup instructions (database, configuration)
   - How to run the application
   - How to run tests
   - API endpoint summary

2. **API Documentation:**
   - Swagger UI accessible at /swagger
   - All endpoints documented with examples
   - Authentication flow explained

3. **Architecture Documentation:**
   - Solution structure diagram
   - Layer responsibilities
   - Data flow explanation
   - Entity relationship diagram

### Testing Requirements

1. Minimum 80% code coverage
2. Unit tests for all services
3. Repository tests with InMemory database
4. Controller tests with mocked dependencies

### Code Quality Standards

1. Follow C# naming conventions
2. No compiler warnings
3. Consistent code formatting
4. XML comments on public APIs
5. Proper async/await usage throughout
6. No hardcoded values (use configuration)
7. Proper error handling and logging

---
