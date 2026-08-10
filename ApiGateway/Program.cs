using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId());


builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("ocelot.SwaggerEndPoints.json", optional: false, reloadOnChange: true);

var jwtKey = builder.Configuration["JwtSettings__Key"] 
    ?? builder.Configuration["JwtSettings:Key"]
    ?? "ThisIsA256BitSecretKeyForAirlineProject123456";
var jwtIssuer = builder.Configuration["JwtSettings__Issuer"]
    ?? builder.Configuration["JwtSettings:Issuer"]
    ?? "AirlineIdentityService";
var jwtAudience = builder.Configuration["JwtSettings__Audience"]
    ?? builder.Configuration["JwtSettings:Audience"]
    ?? "AirlineManagementSystem";

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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


app.UseCors("DefaultPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI(options =>
    {
        options.PathToSwaggerGenerator = "/swagger/docs";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.UseOcelot();

app.Run();

