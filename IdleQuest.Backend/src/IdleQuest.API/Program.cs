using System.IdentityModel.Tokens.Jwt;
using System.Text;
using IdleQuest.API.Middleware;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Application.Services;
using IdleQuest.Infrastructure;
using IdleQuest.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "IdleQuest API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Authorization header **Bearer {token}**",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey
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

// ── Infrastructure (EF Core / SQLite / Redis / Repos / Auth) ────────────────
builder.Services.AddIdleQuestInfrastructure(builder.Configuration);

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IPlayerAppService,  PlayerAppService>();
builder.Services.AddScoped<ICombatService,     CombatService>();
builder.Services.AddScoped<IWorldService,      WorldService>();
builder.Services.AddScoped<IInventoryService,  InventoryService>();
builder.Services.AddScoped<IQuestAppService,   QuestAppService>();
builder.Services.AddScoped<ISaveLoadService,   SaveLoadService>();
builder.Services.AddScoped<INpcAppService,     NpcAppService>();

// ── SignalR (+ optional Redis backplane) ─────────────────────────────────────
var signalr = builder.Services.AddSignalR();
var redisHub = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisHub))
    signalr.AddStackExchangeRedis(redisHub);

builder.Services.AddScoped<IGameHubNotifier, GameHubNotifier>();

// ── JWT authentication ───────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:SecretKey"]
             ?? throw new InvalidOperationException(
                 "Jwt:SecretKey is not configured. " +
                 "Run: dotnet user-secrets set \"Jwt:SecretKey\" \"<your-32+-char-secret>\"");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType            = JwtRegisteredClaimNames.Sub
        };

        // Allow JWT in query-string for SignalR WebSocket upgrade
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────────────────────
var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
              ?? new[] { "http://localhost:5500", "http://127.0.0.1:5500" };

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// ── Rate limiting ─────────────────────────────────────────────────────────────
// Protects auth endpoints from brute-force and combat from bot farming.
builder.Services.AddRateLimiter(options =>
{
    // Auth: max 5 requests per 15 minutes per IP
    options.AddSlidingWindowLimiter("auth", opt =>
    {
        opt.PermitLimit         = 5;
        opt.Window              = TimeSpan.FromMinutes(15);
        opt.SegmentsPerWindow   = 3;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit          = 0;
    });

    // Combat: max 5 attacks per 2 seconds per user
    options.AddSlidingWindowLimiter("combat", opt =>
    {
        opt.PermitLimit         = 5;
        opt.Window              = TimeSpan.FromSeconds(2);
        opt.SegmentsPerWindow   = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit          = 0;
    });

    options.RejectionStatusCode = 429;
});

// ── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game");
app.MapHealthChecks("/health");

app.Run();
