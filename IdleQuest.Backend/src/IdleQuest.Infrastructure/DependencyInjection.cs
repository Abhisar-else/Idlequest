using IdleQuest.Application.Interfaces.Repositories;
using IdleQuest.Application.Interfaces.Services;
using IdleQuest.Infrastructure.Auth;
using IdleQuest.Infrastructure.Caching;
using IdleQuest.Infrastructure.Data;
using IdleQuest.Infrastructure.Events;
using IdleQuest.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdleQuest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdleQuestInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("IdleQuest") ?? "Data Source=idlequest.db";

        services.AddDbContext<IdleQuestDbContext>(o => o.UseSqlite(cs));

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IEnemyRepository, EnemyRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<ICombatSessionRepository, CombatSessionRepository>();
        services.AddScoped<ISaveGameRepository, SaveGameRepository>();
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<INpcRepository, NpcRepository>();

        var redisCs = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisCs))
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = redisCs);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IAuthService, JwtAuthService>();

        services.AddIdleQuestDomainEvents();

        services.AddHostedService<SeedDataHostedService>();

        return services;
    }
}
