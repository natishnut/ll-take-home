# ll-take-home

This repo is what I built for the take-home: a small ASP.NET Core API that pulls current weather from Open-Meteo, stores snapshots in SQL Server with hand-written SQL (no ORM), and exposes a few GET endpoints so list and lookup behave like a local cache, database first, then the public API, then save and return.

---

## How I picked Open-Meteo

**[Open-Meteo](https://open-meteo.com/en/docs)** was an easy choice: it’s free, the documentation made sense on the first read, and integrating a `forecast` call with `current_weather` was simple. There’s no API key, the only secreted config I have in here is the SQL connection string.

---

## SQL on a Mac

I’m on macOS, and leaning on a classic “install SQL Server locally” flow wasn’t in the cards for me, so I ran SQL Server in Docker and pointed the app at `localhost:1433`. That’s part of my setup story, whoever runs this repo sorts out their own SQL surface however they prefer.

On startup the app creates the `LlTakeHome` database if it’s missing (when the name is valid and the login is allowed), then ensures `dbo.WeatherForecasts` exists. After that it’s just normal CRUD-style reads and inserts from the API layer.

---

## Libraries / frameworks (and why)

| What | Why |
|------|-----|
| **ASP.NET Core** (.NET 10 Web SDK) | It’s the straightforward way to host the API routing, DI, config, `dotnet run`. |
| **Microsoft.Data.SqlClient** | The assignment said no ORM and SQL only. SqlClient is the boring, official way to run parameterized T-SQL from .NET without pulling in Entity Framework or similar. |
| **`HttpClient` via `IHttpClientFactory`** | Typed client for Open-Meteo, base URL from config. It’s built into the stack no separate HTTP library. |

I didn’t write a novel about every package the Web SDK pulls in; those three are the ones that reflect actual decisions.

---

## Config that mattered to us

- `ConnectionStrings:DefaultConnection` — points at our SQL instance and database (I used `LlTakeHome` as the catalog name).  
- `OpenMeteo:BaseUrl` — optional; it defaults to `https://api.open-meteo.com/v1/` and never needed an API key.

I edited `appsettings` locally and sometimes used `ConnectionStrings__DefaultConnection` in the shell when I didn’t want secrets in the repo.

---

## How I build and run it

From the **repo root**:

```bash
dotnet restore
dotnet build
dotnet run --project src/LlTakeHome.Api
```

While developing, the app listened on `http://localhost:5020` (see `src/LlTakeHome.Api/Properties/launchSettings.json`).

Endpoints I exercised:

- `GET /` — quick “the host is up”  
- `GET /api/weather-forecasts` — everything we’d already cached  
- `GET /api/weather-forecasts/location/{latitude}/{longitude}` — cache-through (try SQL, else Open-Meteo, persist, return)  
- `GET /api/weather-forecasts/{id}` — by identity after at least one location insert  
