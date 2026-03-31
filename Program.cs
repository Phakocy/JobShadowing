using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using JobShadowing.Application.DTOs;
using JobShadowing.Application.Interfaces;
using JobShadowing.Application.Services;
using JobShadowing.Infrastructure.Data;
using JobShadowing.Infrastructure.Mappings;
using JobShadowing.Infrastructure.Services;
using JobShadowing.API.Middleware;
using JobShadowing.API.Hubs;
using JobShadowing.Models.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/taskflow-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting TaskFlow API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Enhanced Swagger Configuration
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "TaskFlow API",
            Description = "A production-ready Task Management API with Clean Architecture",
            Contact = new OpenApiContact
            {
                Name = "TaskFlow Support",
                Email = "support@taskflow.com"
            }
        });

        // Add JWT Authentication to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        options.EnableAnnotations();
    });

    // Add Memory Cache
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

    // Configure Rate Limiting
    var rateLimitSettings = builder.Configuration.GetSection("RateLimit").Get<RateLimitSettings>() ?? new RateLimitSettings();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("fixed", limiterOptions =>
        {
            limiterOptions.PermitLimit = rateLimitSettings.PermitLimit;
            limiterOptions.Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = rateLimitSettings.QueueLimit;
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            var response = new ErrorResponse
            {
                StatusCode = 429,
                Message = "Too many requests. Please try again later.",
                TraceId = context.HttpContext.TraceIdentifier
            };
            await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        };
    });

    // Configure CORS
    var corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowConfigured", policy =>
        {
            if (corsSettings.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(corsSettings.AllowedOrigins);
            }
            else
            {
                policy.AllowAnyOrigin();
            }

            policy.AllowAnyMethod()
                  .AllowAnyHeader();

            if (corsSettings.AllowCredentials && corsSettings.AllowedOrigins.Length > 0)
            {
                policy.AllowCredentials();
            }
        });
    });

    // Register services
    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<ITeamService, TeamService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // Configure JWT Settings
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

    // Configure File Storage Settings
    builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));

    // Configure Rate Limit Settings
    builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));

    // Configure Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    // Add SignalR
    builder.Services.AddSignalR();

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddAutoMapper(typeof(MappingProfile));

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

    var app = builder.Build();

    // Use Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
            options.DocumentTitle = "TaskFlow API Documentation";
        });
    }

    app.UseHttpsRedirection();

    // Use CORS
    app.UseCors("AllowConfigured");

    // Use Rate Limiting
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map Health Check endpoint
    app.MapHealthChecks("/health");

    // Map SignalR Hub
    app.MapHub<NotificationHub>("/hubs/notifications");

    app.MapControllers().RequireRateLimiting("fixed");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
