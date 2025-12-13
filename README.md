# AurionCal-API

A project to synchronize Junia's student plannings, available at https://aurioncal.slabus.me

## Overview

AurionCal-API exposes a small REST API that:

- Registers a student with their Junia Aurion credentials.
- Periodically fetches their planning from the Junia web services.
- Stores events in a database.
- Exposes the planning as an iCal/ICS feed that can be added to calendar clients (Google Calendar, Outlook, Apple Calendar, etc.).

The backend is built with:

- **.NET 8** and **FastEndpoints** for the HTTP API.
- **Entity Framework Core** with **PostgreSQL** for data storage.
- A per-user refresh mechanism to keep the planning in sync with Aurion.

---

## How it works

### 1. User registration

- The client calls the registration endpoint with:
  - Junia email.
  - Junia password.
- The API:
  - Validates the input.
  - Encrypts the password (see *Security* below).
  - Creates a new `User` in the database.
  - Generates a random `CalendarToken` (a GUID) that will be used to access the ICS feed.

At this point, no events are stored yet; the planning is fetched on demand.

### 2. Planning refresh

When a planning refresh is triggered for a user:

1. The API loads the user from the database.
2. It decrypts the stored Junia password.
3. It calls the **Mauria service** to fetch the latest planning.
4. It removes all existing `CalendarEvents` for this user.
5. It normalizes and deduplicates events coming from Aurion:
   - Trims IDs and titles.
   - Removes duplicate events by Aurion event ID.
6. It inserts the new list of events in the database.
7. It updates the user’s `LastUpdate` timestamp.

### 3. Calendar feed (ICS)

- Each user has a unique `CalendarToken` (GUID).
- The API exposes an endpoint that returns an **iCal/ICS** feed for a given token.
- Calendar clients (Google Calendar, Outlook, Apple Calendar, etc.) can subscribe to this URL.
- The feed is **read-only**:
  - AurionCal-API never accepts write operations through ICS.
  - All write operations go through the HTTP API and Aurion itself.

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
