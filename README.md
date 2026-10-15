# RaceDay

**Portfolio of Evidence — PROG6212 (Programming 2B)**
Michael-John Uren · ST10465421

RaceDay is a full-stack event management platform for the South African road
running, walking and cycling community. Organizers create and manage events,
categories and results; participants browse and enter events, track their
personal performance history, and prepare for race day using live weather
and route information. The finished platform will be a containerized,
cloud-aware, API-driven **ASP.NET Core** application.

## Progress

### Part 1 — Database design (complete)

A relational database design for the platform, produced before any
application or API code:

| Deliverable | File |
|---|---|
| Entity relationship diagram + schema notes | [`database/RaceDay-ERD.png`](database/RaceDay-ERD.png) |
| SQL Server schema + seed data script | [`database/RaceDay-Schema.sql`](database/RaceDay-Schema.sql) |
| RESTful API endpoint specification | [`documents/API-specification.docx`](documents/API-specification.docx) |


The schema is a normalized (3NF), 10-table design (`Roles`, `Clubs`,
`Users`, `UserRoles`, `Venues`, `Routes`, `Events`, `Categories`,
`Entries`, `Results`) covering organizers, events, categories, entries and
results. Live weather is fetched from an external API rather than stored.

To create and seed the database: open `database/RaceDay-Schema.sql` in SQL
Server Management Studio while connected to a SQL Server instance, then
press F5 to run the whole script. It (re)creates a `RaceDayDB` database with
all tables, constraints and representative sample data.

### Part 2 — ASP.NET Core Web API (this branch: `part-2`)

A RESTful **ASP.NET Core Web API (.NET 10)** backed by **SQL Server** through
**Entity Framework Core 10 (Code-First)**, with JWT authentication, server-side
session management, role-based access control, Swagger and an xUnit test suite
that runs on every push through GitHub Actions.

#### Functionality

| Area | Organizer | Participant | Anonymous |
|---|---|---|---|
| Register / log in / refresh / log out | ✔ | ✔ | register & log in |
| View & update own profile | ✔ | ✔ | |
| Create, update, publish, delete own events | ✔ | | |
| View events | ✔ (incl. own drafts) | ✔ | ✔ (published) |
| Define age / distance categories | ✔ (own events) | | |
| View categories | ✔ | ✔ | ✔ |
| Enter an event by choosing a category | | ✔ | |
| View all entries (roster) for own events | ✔ | | |
| Capture / correct finish times and positions | ✔ (own events) | | |
| View own results & performance history | | ✔ | |
| Clubs, venues, routes | create venues/routes | view | view |

Admins (seeded account only) can manage any event, clubs, venue deletion and grant roles.
Every endpoint in [`documents/API-specification.docx`](documents/API-specification.docx) is implemented
except the two live-weather endpoints, which are out of scope for this part.

#### Authentication and sessions

* `POST /api/auth/register` — personal details plus `role`: `Organizer` (or `Organiser`) or `Participant`.
  Admin cannot be self-assigned.
* Passwords are hashed with ASP.NET Core Identity's `PasswordHasher` (PBKDF2-HMAC-SHA512, 100 000
  iterations, per-password salt). Plain-text passwords are never stored.
* `POST /api/auth/login` creates a row in `UserSessions` and returns a short-lived JWT access token
  (30 min) bound to that session, plus a refresh token (7 days, stored only as a SHA-256 hash).
* `POST /api/auth/refresh-token` rotates the refresh token; `POST /api/auth/logout` revokes the session.
* Every authenticated request re-checks the session, so a logged-out, expired or deactivated session's
  access token is rejected immediately (401) even before the JWT itself expires.
* A global fallback policy requires authentication everywhere except endpoints explicitly marked
  anonymous; organizer/participant/admin rules are enforced with role-based `[Authorize]` attributes
  and ownership checks (an organizer can only manage their own events).

#### Database (Code-First)

The entity configurations in `src/RaceDay.Api/Data/Configurations` reproduce
`database/RaceDay-Schema.sql`: table and column names, `nvarchar` lengths, `date` / `time(0)` /
`time(3)` / `datetime2` / `decimal(p,s)` types, named `PK_` / `FK_` / `UQ_` / `CHK_` / `DF_`
constraints, the same `ON DELETE` behaviours and the same `IX_` indexes. The script's seed rows are
applied with `HasData`, using identical identity values. The migrations in
`src/RaceDay.Api/Migrations` were generated with `dotnet ef`: `InitialCreate` (the ten scripted
tables) and `AddUserSessions`.

Two deliberate notes:

1. **`UserSessions`** is the only table added beyond the Part 1 script. It is required for session
   management (refresh tokens and logout).
2. **Event distance.** The Part 1 schema stores distance on each *category* (and route), not on the
   event, because one race day usually offers several distances. The API therefore returns an event's
   distances as `distancesKm` (one value per category) rather than adding a column that would break
   parity with the script. An event's location is its venue; its type is the `discipline`
   (`Running`, `Walking`, `Cycling`, `Multisport` — `run`, `walk` and `cycle` are accepted).

The routes table has no owner column, so an organizer may edit or delete a route unless it is used by
another organizer's event.

#### Running locally (Visual Studio 2026 / .NET 10 SDK)

1. Open `RaceDay.sln`.
2. The connection string in `src/RaceDay.Api/appsettings.json` points at
   `(localdb)\MSSQLLocalDB`, database `RaceDayDB`. Change it if you use another SQL Server instance.
3. Create the database from the migrations, either in the Package Manager Console:
   ```
   Update-Database -Project RaceDay.Api -StartupProject RaceDay.Api
   ```
   or from a terminal:
   ```
   dotnet tool install --global dotnet-ef
   dotnet ef database update --project src/RaceDay.Api
   ```
   (In the Development environment the API also applies pending migrations on startup.)
4. Press **F5**. The browser opens Swagger UI at `/swagger`.
5. Call `POST /api/auth/login`, copy `accessToken`, click **Authorize** and paste it.

Seeded accounts all use the demo password **`RaceDay@2026`** (development only):

| Email | Role |
|---|---|
| `admin@raceday.co.za` | Admin |
| `sarah.vdm@raceday.co.za` | Organizer (event 1) |
| `thabo.nkosi@raceday.co.za` | Organizer (event 2) |
| `lindiwe.d@example.co.za` | Participant |
| `johan.botha@example.co.za` | Participant |
| `aisha.patel@example.co.za` | Organizer + Participant |

The JWT signing key in `appsettings.json` is a development placeholder. Override `Jwt__SigningKey`
(environment variable or user secrets) outside development.

#### Tests

`tests/RaceDay.Tests` (xUnit) hosts the real API pipeline in memory with `WebApplicationFactory`
and an isolated EF Core in-memory database per test class, so authentication, session validation
and authorization run exactly as in production. Coverage includes:

* registration and login — success and failure (duplicate email, invalid role, weak password,
  wrong password, unknown email, deactivated account, refresh and logout);
* every protected endpoint returning 401 without a token;
* role enforcement — participants rejected from organizer features, organizers rejected from
  participant features, non-admins rejected from admin features, organizers rejected from other
  organizers' events;
* event management — create, update, publish, delete/cancel and validation errors;
* categories, enrolments (the stored participant–event–category link, duplicates, capacity, age and
  gender rules, closed registration), results, profiles, reference data and the Swagger document;
* unit tests for password hashing, JWT creation and discipline mapping.

```
dotnet test RaceDay.sln
```

#### CI/CD

`.github/workflows/workflow.yml` runs on every push to any branch, on pull requests into `main`,
and on demand. It restores and builds the solution, fails if the EF Core migrations have drifted
from the entity model (`dotnet ef migrations has-pending-model-changes`), runs the full test suite
(159 tests) and uploads the results as a build artifact.

## Repository structure

```
database/                  SQL Server schema + seed data script & entity relationship diagram
documents/                 RESTful API endpoint specification
src/RaceDay.Api/           ASP.NET Core Web API
  Controllers/             Auth, Users, Clubs, Venues, Routes, Events, Categories, Entries, Results
  Data/                    DbContext, Fluent API configurations, seed data
  Dtos/                    Request and response models
  Migrations/              EF Core Code-First migrations
  Models/                  Entities
  Services/                Password hashing and JWT token services
tests/RaceDay.Tests/       xUnit integration and unit tests
.github/workflows/         GitHub Actions build-and-test pipeline
```

## AI Usage Disclosure

- I used Claude to assist with the creation of the database schema. The AI provided suggestions for table structures, relationships, which I then reviewed and modified to fit the specific requirements of the RaceDay platform. It was also used to generate the database seed data, and formatting the README file.
- Part 2: I used Claude (Claude Code) to help scaffold the Web API, the EF Core entity configurations, the authentication and session design, the controllers, the unit tests and the GitHub Actions workflow, which I then reviewed and tested.

## Tech stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core 10 (Code-First) with SQL Server
- JWT bearer authentication with server-side sessions
- Swagger / OpenAPI (Swashbuckle)
- xUnit, WebApplicationFactory, EF Core InMemory
- GitHub Actions
