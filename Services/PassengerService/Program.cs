using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using PassengerService.Data;
using PassengerService.Repositories.Interfaces;
using PassengerService.Repositories.implementations;
using PassengerService.Services.Interfaces;
using PassengerService.Services.implementations;
using Shared.Configuration;
using Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId());


var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddDbContext<PassengerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));


builder.Services.AddScoped<IRewardRepository, RewardRepository>();
builder.Services.AddScoped<IRewardService, RewardServiceImpl>();
builder.Services.AddScoped<IPassengerProfileRepository, PassengerProfileRepository>();
builder.Services.AddScoped<IPassengerAuthService, PassengerAuthService>();
builder.Services.AddSingleton<ITokenService>(new JwtTokenService(
    jwtSettings.Key,
    jwtSettings.Issuer,
    jwtSettings.Audience,
    jwtSettings.ExpirationMinutes));

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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PassengerService Web API", Version = "v1" });

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

    options.OrderActionsBy(apiDesc =>
    {
        var path = apiDesc.RelativePath?.ToLower() ?? "";
        var method = apiDesc.HttpMethod?.ToUpper() ?? "";
        var controller = apiDesc.ActionDescriptor.RouteValues.TryGetValue("controller", out var c) ? c : "";

        int rank = 99;
        if (path.EndsWith("/login") || path.Contains("login")) rank = 10;
        else if (path.EndsWith("/register") || path.Contains("register")) rank = 20;
        else if (path.EndsWith("/verify") || path.Contains("/verify")) rank = 30;
        else if (path.Contains("resend-verification")) rank = 40;
        else if (path.Contains("force-verify")) rank = 50;
        else if (path.Contains("forgot-password")) rank = 60;
        else if (path.Contains("reset-password")) rank = 70;
        else if (path.Contains("change-password")) rank = 80;

        return $"{controller}_{rank:D2}_{path}_{method}";
    });
});

var corsSettings = builder.Configuration.GetSection("CorsSettings");
var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

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


app.UseMiddleware<Shared.Middleware.GlobalExceptionMiddleware>();

for (int i = 0; i < 10; i++)
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PassengerDbContext>();
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
    app.UseSwaggerUI(options =>
    {
        options.HeadContent = @"
<script>
(function() {
    function getRank(el) {
        var text = (el.innerText || el.textContent || '').toLowerCase();
        if (text.indexOf('/login') !== -1 || text.indexOf(' login ') !== -1 || text.indexOf('login') !== -1) {
            if (text.indexOf('resend') === -1 && text.indexOf('reset') === -1) return 10;
        }
        if (text.indexOf('/register') !== -1 || text.indexOf('register') !== -1) return 20;
        if (text.indexOf('resend-verification') !== -1 || text.indexOf('resend verification') !== -1) return 40;
        if (text.indexOf('force-verify') !== -1) return 35;
        if (text.indexOf('/verify') !== -1 || text.indexOf('verify') !== -1) return 30;
        if (text.indexOf('forgot-password') !== -1 || text.indexOf('forgot password') !== -1) return 50;
        if (text.indexOf('reset-password') !== -1 || text.indexOf('reset password') !== -1) return 60;
        if (text.indexOf('change-password') !== -1 || text.indexOf('change password') !== -1) return 70;
        return 999;
    }

    function sortOps() {
        var sections = document.querySelectorAll('.opblock-tag-section');
        sections.forEach(function(section) {
            var ops = Array.from(section.querySelectorAll('.opblock'));
            if (ops.length <= 1) return;
            var parent = ops[0].parentElement;
            if (!parent) return;

            var sorted = ops.slice().sort(function(a, b) {
                return getRank(a) - getRank(b);
            });

            var changed = false;
            for (var i = 0; i < ops.length; i++) {
                if (ops[i] !== sorted[i]) {
                    changed = true;
                    break;
                }
            }

            if (changed) {
                sorted.forEach(function(el) {
                    parent.appendChild(el);
                });
            }
        });
    }

    setInterval(sortOps, 250);
    window.addEventListener('DOMContentLoaded', sortOps);
    window.addEventListener('load', sortOps);
})();
</script>";
    });
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

