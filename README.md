# IdleQuest

Idle RPG with a **.NET 8** backend (clean architecture: Domain → Application → Infrastructure → API) and a **vanilla front-end** (`index.html` + Tailwind) in the repo root.

---

## Repository layout

```
├── index.html                 # UI (client-side demo state; not wired to API by default)
├── api-client.js              # Browser helper `idleQuestApi` for REST calls (set API_BASE_URL)
├── IdleQuest.Backend/
│   ├── IdleQuest.sln
│   ├── global.json            # SDK: .NET 8 (roll-forward)
│   ├── docker-compose.yml     # Optional local Redis
│   ├── DEPENDENCIES.txt       # What to install (SDK, Docker, etc.)
│   ├── run-api.cmd            # Windows: start API
│   └── src/
│       ├── IdleQuest.Domain/          # Aggregates, value objects, domain events
│       ├── IdleQuest.Application/     # DTOs, interfaces, application services
│       ├── IdleQuest.Infrastructure/  # EF Core, JWT, cache, SignalR, seed
│       └── IdleQuest.API/             # Controllers, middleware, Program.cs
```

---

## What you need installed

| Requirement | Notes |
|-------------|--------|
| **.NET 8 SDK** | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) — `dotnet --version` should show `8.0.x`. |
| **NuGet packages** | Restored automatically on `dotnet build` / `dotnet run` (no manual install). |

**Optional:** **Docker** + `docker compose up -d` in `IdleQuest.Backend` for **Redis** (see `docker-compose.yml`). If `ConnectionStrings:Redis` is empty, the API uses **in-memory** distributed cache and still runs on a single machine.

**Optional:** `dotnet dev-certs https --trust` for trusted HTTPS in development.

Details: `IdleQuest.Backend/DEPENDENCIES.txt`.

---

## Run the API

```bash
cd IdleQuest.Backend
dotnet run --project src/IdleQuest.API
```

Or on Windows: double-click `IdleQuest.Backend/run-api.cmd`.

- **HTTP / HTTPS** ports are shown in the console; defaults are in `src/IdleQuest.API/Properties/launchSettings.json` (e.g. `http://localhost:5098`, `https://localhost:7098`).
- **Swagger** (OpenAPI) is enabled only in **Development** — open `/swagger` on the HTTPS or HTTP base URL.
- **Health:** `GET /health` (no auth).
- **Database:** **SQLite** file `idlequest.db` is created in the **current working directory** when the API runs (EF Core `EnsureCreated` + **seed** on startup: zones, enemies, items, merchant NPC, sample quests).

Configuration: `IdleQuest.Backend/src/IdleQuest.API/appsettings.json` (`ConnectionStrings:IdleQuest`, `Jwt`, `AllowedOrigins`).

---

## Architecture (implemented)

```
┌─────────────────────────┐
│  Browser UI (optional) │   index.html / Live Server / VS Code
└────────────┬────────────┘
             │  HTTPS + JWT (Bearer)     │  WebSocket (SignalR)
             ▼                             ▼
┌────────────────────────────────────────────────────────────┐
│  IdleQuest.API — controllers, JWT, CORS, Swagger (dev),    │
│  ExceptionHandlingMiddleware → DomainException → HTTP 400   │
└────────────────────────────┬───────────────────────────────┘
                             │
┌────────────────────────────▼───────────────────────────────┐
│  Application — PlayerAppService, CombatService, WorldService,
│  InventoryService, QuestAppService, SaveLoadService, NpcAppService,
│  IDomainEventDispatcher, DTOs                               │
└──────────────┬─────────────────────────────┬─────────────┘
               │                               │
┌──────────────▼──────────────┐   ┌────────────▼──────────────┐
│  Domain                     │   │  Infrastructure          │
│  Player, Enemy, Quest, …    │   │  EF Core SQLite          │
│  StatBlock, DomainEvent     │   │  JWT + BCrypt            │
│                             │   │  Redis or memory cache    │
│                             │   │  SignalR GameHub + notifier │
│                             │   │  Repositories, seed       │
└─────────────────────────────┘   └───────────────────────────┘
```

**SignalR:** Hub URL `/hubs/game`. Pass the JWT as query **`access_token`** (same token as `Authorization: Bearer`).

**Redis:** If `ConnectionStrings:Redis` is set, StackExchange Redis is used for **distributed cache** and optionally **SignalR scale-out** (`AddStackExchangeRedis` on the SignalR builder in `Program.cs`).

**Design notes**

- **DDD-style** aggregates and **domain events** (`PlayerLeveledUp`, `PrestigeCompleted`) with handlers that push SignalR notifications where wired.
- **`StatBlock`** composes with **`Add()`** for effective stats.
- **Idle rewards:** `POST /api/combat/idle-rewards` uses `LastLoginAt`, capped at **8 hours** of simulated offline gains (no separate background “auto-save every 2 minutes” worker in this repo).

---

## API endpoints

Base URL example: `http://localhost:5098/api` (adjust to your run output).

| Method | Path | Auth |
|--------|------|------|
| POST | `/auth/register` | Body: `username`, `password`, `heroName`, **`class`** (JSON property name is `class`) |
| POST | `/auth/login` | Returns JWT + `refreshToken` |
| POST | `/auth/refresh` | Body: `{ "refreshToken": "..." }` |
| GET | `/player/me` | Bearer |
| PATCH | `/player/me/rename` | Body: `{ "name": "..." }` |
| PATCH | `/player/me/class` | Body: `{ "class": "Warrior" }` |
| POST | `/player/me/prestige` | Bearer |
| GET | `/player/leaderboard` | Public |
| GET | `/inventory` | Bearer |
| POST | `/inventory/equip/{itemId}` | Bearer |
| DELETE | `/inventory/equip/{slot}` | `slot` = `weapon` or `armor` |
| POST | `/inventory/{itemId}/sell` | Bearer |
| GET | `/quests/available` | Bearer |
| POST | `/quests/{id}/accept` | Bearer |
| POST | `/quests/{id}/complete` | Bearer |
| POST | `/combat/start/{zoneId}` | Bearer |
| POST | `/combat/{sessionId}/attack` | Bearer |
| POST | `/combat/{sessionId}/flee` | Bearer |
| POST | `/combat/idle-rewards` | Bearer |
| GET | `/npcs/zone/{zoneId}` | Public |
| POST | `/npcs/{id}/dialogue` | Bearer |
| GET | `/npcs/{id}/shop` | Bearer |
| POST | `/npcs/{npcId}/shop/{itemId}` | Bearer |
| GET | `/world/zones` | Bearer |
| POST | `/world/zones/{zoneId}/travel` | Bearer |
| GET | `/saves` | Bearer |
| POST | `/saves/{slot}` | Slot enum name: `Auto`, `Manual1`, … |
| POST | `/saves/{slot}/load` | Bearer |

### SignalR (server → client) — current wiring

| Event | When |
|--------|------|
| `LevelUp` | Domain handler after level-up |
| `Prestige` | Domain handler after prestige |
| `CombatStarted` | Combat started |
| `CombatUpdate` | After attack / flee (also sent to caller from hub) |
| `CombatVictory` | Enemy defeated |
| `PlayerEnteredZone` | Hub `JoinZone` |
| `WorldState` | Hub `RequestWorldState` |

---

## Front-end and `api-client.js`

`api-client.js` exposes a global **`idleQuestApi`** object (no bundler). Set **`idleQuestApi.API_BASE_URL`** to match your API (scheme + host + port + `/api`).

The stock **`index.html`** is a self-contained demo and is **not** automatically connected to the API. To integrate, load `api-client.js` and call the API from your own script (or a separate page) using the same origin/CORS rules as in `appsettings.json` → `AllowedOrigins`.

### Example: login (fetch)

```js
const res = await fetch('http://localhost:5098/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ username: 'hero1', password: 'yourPassword' })
});
const data = await res.json();
if (data.token) localStorage.setItem('token', data.token);
```

### Example: SignalR (JavaScript client)

Add `@microsoft/signalr` or a CDN script, then connect with the same base URL as the site and pass the token:

```js
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5098/hubs/game', {
    accessTokenFactory: () => localStorage.getItem('token')
  })
  .withAutomaticReconnect()
  .build();
connection.on('LevelUp', payload => console.log(payload));
await connection.start();
```

---

## NuGet packages (reference)

Versions are pinned in the `.csproj` files; restore is automatic.

**IdleQuest.Infrastructure**

- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design` (private assets / tooling)
- `Microsoft.AspNetCore.SignalR.StackExchangeRedis`
- `Microsoft.Extensions.Caching.StackExchangeRedis`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `BCrypt.Net-Next`

**IdleQuest.API**

- `Swashbuckle.AspNetCore`
- `Microsoft.AspNetCore.SignalR.StackExchangeRedis`

---

## Moving to SQL Server or EF migrations

Default setup uses **SQLite** and **`EnsureCreated`** + seed for a friction-free clone-and-run workflow.

For production you typically:

1. Switch **`UseSqlite`** to **`UseSqlServer`** (or another provider) in `IdleQuest.Infrastructure` registration.
2. Replace **`EnsureCreated`** with **EF Core migrations** (`dotnet ef migrations add`, `dotnet ef database update`) and move seed data to migrations or an explicit seed runner.

---

## Scaling ideas (roadmap)

| Goal | Direction |
|------|-----------|
| Multiple API instances | Redis for SignalR backplane + shared cache (already optional when Redis is configured) |
| Stronger persistence | SQL Server / Postgres + migrations |
| Heavy combat load | Queue-backed processing |
| Analytics | Publish domain events to a bus / warehouse |
