using System.IdentityModel.Tokens.Jwt;
using System.Text;
using IdleQuest.API.Middleware;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Services;
using IdleQuest.Infrastructure;
using IdleQuest.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "IdleQuest API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Authorization header **Bearer {token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddIdleQuestInfrastructure(builder.Configuration);

builder.Services.AddScoped<IPlayerAppService, PlayerAppService>();
builder.Services.AddScoped<ICombatService, CombatService>();
builder.Services.AddScoped<IWorldService, WorldService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IQuestAppService, QuestAppService>();
builder.Services.AddScoped<ISaveLoadService, SaveLoadService>();
builder.Services.AddScoped<INpcAppService, NpcAppService>();

var signalr = builder.Services.AddSignalR();
var redisHub = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisHub))
    signalr.AddStackExchangeRedis(redisHub);

builder.Services.AddScoped<IGameHubNotifier, GameHubNotifier>();

var jwtKey = builder.Configuration["Jwt:SecretKey"]
             ?? throw new InvalidOperationException("Configure Jwt:SecretKey (min ~32 characters).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
              ?? new[] { "http://localhost:5500", "http://127.0.0.1:5500" };

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game");
app.MapHealthChecks("/health");

app.Run();
