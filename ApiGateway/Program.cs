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
    app.UseSwaggerForOcelotUI(
        options => { options.PathToSwaggerGenerator = "/swagger/docs"; },
        uiOptions =>
        {
            uiOptions.HeadContent = @"
<script>
(function() {
    function getTagRank(section) {
        var tagHeader = section.querySelector('.opblock-tag');
        if (!tagHeader) return 999;
        var text = (tagHeader.innerText || tagHeader.textContent || '').trim().toLowerCase();
        if (text.indexOf('flight') !== -1) return 10;
        if (text.indexOf('booking') !== -1) return 20;
        if (text.indexOf('checkin') !== -1 || text.indexOf('check-in') !== -1) return 30;
        if (text.indexOf('auth') !== -1) return 5;
        if (text.indexOf('passenger') !== -1) return 15;
        if (text.indexOf('payment') !== -1) return 10;
        if (text.indexOf('backoffice') !== -1) return 25;
        return 500;
    }

    function sortTagSections() {
        var sections = Array.from(document.querySelectorAll('.opblock-tag-section'));
        if (sections.length <= 1) return;
        var parent = sections[0].parentElement;
        if (!parent) return;

        var sorted = sections.slice().sort(function(a, b) {
            return getTagRank(a) - getTagRank(b);
        });

        var changed = false;
        for (var i = 0; i < sections.length; i++) {
            if (sections[i] !== sorted[i]) {
                changed = true;
                break;
            }
        }

        if (changed) {
            sorted.forEach(function(el) {
                parent.appendChild(el);
            });
        }
    }

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
        sortTagSections();
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
        }
    );
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.UseOcelot();

app.Run();

