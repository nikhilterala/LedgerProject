using LedgerProject.Data;
using LedgerProject.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<LedgerService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database Seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Ensure database is created (optional if using migrations, but helpful for first run)
    context.Database.EnsureCreated();

    // 1. Seed Roles
    var roles = new[] { "Admin", "Operator", "User" };
    foreach (var roleName in roles)
    {
        if (!context.Roles.Any(r => r.RoleName == roleName))
        {
            context.Roles.Add(new LedgerProject.Models.Role 
            { 
                RoleId = Guid.NewGuid(), 
                RoleName = roleName 
            });
        }
    }
    context.SaveChanges();

    // 2. Seed Default Admin User
    if (!context.Users.Any())
    {
        var adminRole = context.Roles.First(r => r.RoleName == "Admin");
        var adminUser = new LedgerProject.Models.User
        {
            UserId = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            IsActive = true
        };
        context.Users.Add(adminUser);
        context.UserRoles.Add(new LedgerProject.Models.UserRole
        {
            UserId = adminUser.UserId,
            RoleId = adminRole.RoleId
        });
        context.SaveChanges();
    }
}

app.Run();
