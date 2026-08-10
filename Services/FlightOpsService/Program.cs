using Serilog;
using FlightOpsService.Data;
using FlightOpsService.Repositories;
using FlightOpsService.Services;
using FlightOpsService.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog Setup ─────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId());

// ── JWT Settings ──────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

// ── DbContext Setup ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<FlightOpsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ── Repositories Registration ──────────────────────────────────────────────────
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();
builder.Services.AddScoped<IBaggageRepository, BaggageRepository>();

// ── Services Registration ─────────────────────────────────────────────────────
builder.Services.AddScoped<IFlightService, FlightServiceImpl>();
builder.Services.AddScoped<IFlightScheduleService, FlightScheduleService>();
builder.Services.AddScoped<IBookingService, BookingServiceImpl>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<ICheckInService, CheckInServiceImpl>();
builder.Services.AddScoped<IBaggageService, BaggageServiceImpl>();

// ── Hosted Services ───────────────────────────────────────────────────────────
builder.Services.AddHostedService<ScheduleCompletionWorker>();

// ── Authentication & Authorization ────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger Configuration ─────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FlightOpsService API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter token as: Bearer {your token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
            new List<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ── CORS Configuration ────────────────────────────────────────────────────────
var corsSettings = builder.Configuration.GetSection("CorsSettings");
var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "*" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// ── Database Migration Retry Loop ─────────────────────────────────────────────
for (int i = 0; i < 10; i++)
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FlightOpsDbContext>();
            dbContext.Database.Migrate();
        }
        Console.WriteLine("Database migrated successfully.");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration attempt {i + 1} failed: {ex.Message}. Retrying in 5s...");
        Thread.Sleep(5000);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
