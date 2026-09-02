# MisterTicket

Seat-booking web application for theatre performances.
.NET 8 Web API (EF Core, JWT, SignalR) + Angular front-end.

## Requirements

- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio)
- Node.js 20+ and the Angular CLI (`npm install -g @angular/cli`)

## 1. Back-end

From the `MisterTicketApi` folder:

```bash
dotnet restore
```

### Create the database

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Or directly into the package manager console:

```bash
Add-Migration InitialCreate
Update-Database
```

### Run

The API starts on `https://localhost:7041` and opens Swagger. On the first run
with an empty database, the seeder fills it with demo data (see below).

## 2. Front-end

The app runs on `http://localhost:4200`.

If your API uses a different port, change it in `src/app/api.config.ts`:

```ts
export const API_BASE_URL = 'https://localhost:7041/api';
export const HUB_BASE_URL = 'https://localhost:7041';
```

The API must be running before the front-end is opened, otherwise the event
list stays empty.

## 3. Demo accounts

Created by the seeder on the first run. All four share the same password:

| Email | Password | Role | Can do |
|---|---|---|---|
| `admin@misterticket.be` | `Password123!` | Admin | everything, including the dashboard for all events |
| `orga@misterticket.be` | `Password123!` | Organizer | dashboard for their own events, CRUD on events / venues / zones |
| `client@misterticket.be` | `Password123!` | Client | browse, book, pay, download tickets |
| `bram@misterticket.be` | `Password123!` | Client | a second client, useful to test two people booking at once |

Registering through the app always creates a **Client**. Organizer and Admin
accounts are created by the seeder.

### What else the seeder creates

- 3 pricing zones: VIP (€75), Orchestre (€45), Balcon (€28)
- 2 venues: Theatre Royal in Gent (156 seats), Salle Moliere in Bruxelles (144 seats)
- 3 upcoming performances
- 2 reservations on the first event: one paid, one still pending

The seeder only runs when the `Users` table is empty. To start over, drop the
database and run `dotnet ef database update` again.
