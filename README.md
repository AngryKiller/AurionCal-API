# AurionCal-API

A project to synchronize students' Aurion plannings (Junia and other schools), available at https://aurioncal.slabus.me

## Overview

AurionCal-API exposes a small REST API that:

- Registers a student with their school's Aurion credentials (the school is chosen at sign-up).
- Periodically fetches their planning from the school's Aurion, through the Mauria service.
- Stores events in a database.
- Exposes the planning as an iCal/ICS feed that can be added to calendar clients (Google Calendar, Outlook, Apple Calendar, etc.).

The backend is built with:

- **.NET 8** and **FastEndpoints** for the HTTP API.
- **Entity Framework Core** with **PostgreSQL** for data storage.
- A per-user refresh mechanism to keep the planning in sync with Aurion.
- A **school catalog** (`schools.json`) so that several Aurion-based schools can be served, each with its own event formatting rules.

---

## How it works

### 1. User registration

- The client first lists the available schools with `GET /api/schools`, and the user picks theirs.
- The client then calls the registration endpoint with:
  - Email.
  - Password.
  - `schoolId` (optional: the default school is used when it is missing).
- The API:
  - Validates the input.
  - Resolves the school and checks that the email domain is accepted by it.
  - Checks the credentials against the school's Aurion through Mauria.
  - Encrypts the password (see *Security* below).
  - Creates a new `User` in the database, linked to the school (`SchoolId`). Emails are stored trimmed and lowercase, and are unique across schools.
  - Generates a random `CalendarToken` (a GUID) that will be used to access the ICS feed.

At this point, no events are stored yet; the planning is fetched on demand.

### Login

`POST /api/check-aurion-auth` takes an email and a password. The user, and therefore the school, is found from the email alone, so the client never has to send the school again. The credentials are checked against the school's Aurion, and a JWT is returned.

### 2. Planning refresh

When a planning refresh is triggered for a user:

1. The API loads the user from the database.
2. It resolves the user's school from the catalog (the refresh is skipped and an error is logged if the school is unknown).
3. It decrypts the stored password.
4. It calls the **Mauria service** with the school's Aurion URL to fetch the latest planning.
5. It removes all existing `CalendarEvents` for this user.
6. It normalizes and deduplicates events coming from Aurion:
   - Trims IDs and titles.
   - Removes duplicate events by Aurion event ID.
7. It inserts the new list of events in the database.
8. It updates the user’s `LastUpdate` timestamp.

### 3. Calendar feed (ICS)

- Each user has a unique `CalendarToken` (GUID).
- The API exposes an endpoint that returns an **iCal/ICS** feed for a given token.
- Calendar clients (Google Calendar, Outlook, Apple Calendar, etc.) can subscribe to this URL.
- Events are converted to iCal by the **event formatter of the user's school** (see *Schools* below). The file is named `Planning <school name>.ics`.
- For schools that support it (`SupportsExamAccommodations`), users can enable the extra-time exam schedule with `PATCH /api/user/exam-accommodations`; the feed then uses the extra-time hours found in exam titles.
- The feed is **read-only**:
  - AurionCal-API never accepts write operations through ICS.
  - All write operations go through the HTTP API and Aurion itself.

---

## Schools

Supported schools are declared in `schools.json` and validated at startup (the API refuses to start on an invalid configuration).

```json
{
  "Schools": {
    "DefaultSchoolId": "junia",
    "Items": [
      {
        "Id": "junia",
        "Name": "Junia",
        "AurionBaseUrl": "https://aurion.junia.com",
        "EmailDomains": [ "junia.com", "student.junia.com" ],
        "SupportsExamAccommodations": true,
        "ParserId": "junia"
      }
    ]
  }
}
```

| Field | Purpose |
|---|---|
| `DefaultSchoolId` | School used when a client does not send a `schoolId` (backward compatibility). |
| `Id` | Stable identifier stored on each user (`User.SchoolId`). Do not change it once users exist. |
| `Name` | Display name, also used in the `.ics` file name. |
| `AurionBaseUrl` | Aurion URL sent to Mauria (`baseUrl` field) with every login and planning request. It only comes from this file, never from a client. |
| `EmailDomains` | Accepted email domains. A `*.` prefix accepts subdomains (`*.school.edu`, but not `school.edu`). |
| `SupportsExamAccommodations` | Whether the extra-time exam schedule option is available (defaults to `false`). |
| `ParserId` | Event formatter to use (defaults to `default`). |
| `Parsing.ClassNames` | Optional overrides of the Aurion `ClassName` -> event type mapping. |

### Endpoints

| Endpoint | Description |
|---|---|
| `GET /api/schools` | Public list of schools (`id`, `name`, `emailDomains`, `supportsExamAccommodations`) and `defaultSchoolId`. The Aurion URL is not exposed. |
| `POST /api/register` | Takes `email`, `password` and an optional `schoolId`. Errors: `UNKNOWN_SCHOOL`, `EMAIL_DOMAIN_NOT_ALLOWED` (400), `ACCOUNT_ALREADY_EXISTS` (409). |
| `POST /api/check-aurion-auth` | Takes `email` and `password`. The school is found from the user. |
| `GET /api/user/profile` | Includes `schoolId` and `supportsExamAccommodations`. |
| `PATCH /api/user/exam-accommodations` | Rejected (400) when the user's school does not support the option. |

### Event formatting

Titles coming from Aurion do not have the same layout in every school, so formatting is a strategy chosen per school (`Services/Formatting`):

- `IEventFormatter` turns a raw event into an iCal event; `IEventFormatterFactory` returns the formatter of a school, based on its `ParserId`.
- `EventFormatterBase` holds what is shared (title splitting, iCal construction) and one extension point per event type (course, exam, make-up exam, conference). By default every type falls back to the generic course format.
- `JuniaEventFormatter` implements Junia's conventions, including the `Horaire TT` extra-time line of exams.
- `DefaultEventFormatter` is the generic fallback, also used when a `ParserId` is unknown (a warning is logged).

### Adding a school

1. Add an entry to `schools.json`.
2. If its event titles follow an existing layout, reuse that `ParserId`. Otherwise, subclass `EventFormatterBase`, override the formats that differ, and register the class in `EventFormatterFactory`.
3. The Mauria service must support the `baseUrl` field for this school.

---

## Tests

```bash
dotnet test AurionCal.Tests/AurionCal.Tests.csproj
```

The unit tests cover the formatters, the school catalog and its validation (including the shipped `schools.json`), and the event type mapping. They run on every pull request towards `main` (`.github/workflows/tests.yml`).

---

## Security

AurionCal-API is designed to protect user credentials.

### Credential encryption

Users passwords are **never stored in plain text**.

- The API uses the `IEncryptionService` abstraction for all encryption/decryption operations.
- At startup, the implementation is selected based on configuration.

- If Azure Key Vault is configured (non-empty `KeyVault:KeyVaultUrl`) the API registers: `IEncryptionService => KeyVaultService`
Otherwise (the typical development setup, as in appsettings.Development.json), or Doppler secrets it falls back to: `IEncryptionService => LocalEncryptionService`
which uses the symmetric key defined in Encryption:Key.

Passwords are decrypted **only when** the API needs to call the Aurion service to refresh a user’s planning.


## Acknowledgments

A huge thanks to https://github.com/MauriaApp for their scrapping API, used in this project.
