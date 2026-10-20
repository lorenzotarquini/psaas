# PLAN – Preparazione PSAAS.AdminPortal.Backend

Obiettivo: rendere `PSAAS.AdminPortal.Backend` (Web API .NET 10, presentation della clean architecture) pronto a implementare funzionalità. Il frontend autentica l'utente su Keycloak e chiama il backend con un access token (Bearer).

## Decisioni prese (default, modificabili)

- Endpoint: minimal API (come da template).
- Autenticazione: `PSAAS.Auth` in modalità `Api` (JWT Bearer), audience validata.
- Client Keycloak del backend: `adminportal-api` (parametro AppHost, da creare in Keycloak).
- CORS: origini consentite da configurazione (`Cors:AllowedOrigins`), vuota = nessuna origine.
- Migration: applicate a mano (`dotnet ef database update`), non all'avvio.
- Logging: NLog (`NLog.Web.AspNetCore`), configurazione in `appsettings.json` come il vecchio AdminPortal.
- Test: solo `PSAAS.AdminPortal.Backend.Tests` per ora (niente `PSAAS.Application.Tests`).

## Fasi

1. Referenze: Backend → Application, Infrastructure, Auth.
2. Pulizia template: via `WeatherForecast`; OpenAPI solo in Development.
3. Autenticazione/autorizzazione: `AddKeycloak`, `UseAuthentication`/`UseAuthorization`, sezione `Keycloak` in appsettings (`Mode: Api`, `ValidateAudience: true`), policy `Administrator`, gruppo `/api` con `RequireAuthorization("Administrator")`.
4. Database e servizi esterni: `AddPsaasInfrastructure`, `ItalianCities:BaseUrl` in appsettings.
5. Aspire: AppHost → Backend con `WithReference(psaasDb).WaitFor(psaasDb)`. La configurazione Keycloak sta in `appsettings.json` del backend (in modalità `Api` non ci sono segreti); i vecchi parametri `keycloak-*` dell'AppHost sono stati rimossi.
6. Trasversali: NLog, CORS, ProblemDetails + exception handler (conflitti → 409), `/health`.
7. Casi d'uso in Application: RIMANDATO, sono le prime funzionalità da implementare (CreateTenant, EditTenant, GetTenants, GetTenant + `AddPsaasApplication()`).
8. Test: `PSAAS.AdminPortal.Backend.Tests` con `WebApplicationFactory` (401 senza token, 403 senza ruolo, 200 con ruolo), nella cartella di solution `/Tests/`.

## Manuale (fuori dal codice)

- Keycloak: creare il client `adminportal-api`, il client pubblico del frontend (PKCE) con audience mapper verso `adminportal-api`, ruolo `Administrator`.
- Password Postgres in chiaro in `src/PSAAS.AppHost/appsettings.json`: spostarla negli user secrets e cambiarla.

## Stato

- [x] F1 Referenze Backend → Application, Infrastructure, Auth; NLog.Web.AspNetCore 6.2.1.
- [x] F2 Template ripulito; `.http` con `/health` e `/api/me`.
- [x] F3 Keycloak `Api` + policy `Administrator` sul gruppo `/api`; endpoint `GET /api/me` (nome e ruoli) per verificare il collegamento col frontend.
  Bugfix PSAAS.Auth: in modalità Api i ruoli Keycloak non venivano mappati (JwtBearer da .NET 8 produce `JsonWebToken`, il codice gestiva solo `JwtSecurityToken`): ogni policy con ruolo rispondeva 403.
- [x] F4 `AddPsaasInfrastructure`; `ItalianCities:BaseUrl` in appsettings.
  Bugfix Infrastructure: aggiunto `Microsoft.EntityFrameworkCore.Relational` 10.0.12, altrimenti a runtime arrivava EF 10.0.4 (via Npgsql) e l'avvio falliva.
- [x] F5 AppHost collegato al backend.
- [x] F6 NLog (JSON su console), CORS da config, ProblemDetails con 409 per i conflitti, `/health`.
- [ ] F7 Rimandata (prime funzionalità).
- [x] F8 `PSAAS.AdminPortal.Backend.Tests` (5 test: 401 senza token, 401 audience errata, 403 senza ruolo, 200 con ruolo, health anonimo). Suite: Backend 5/5, Auth 8/8, Infra 17/17.
  Avvio reale verificato senza DB e senza Keycloak: /health 200, /api/me 401, OpenAPI 200. Login reale con Keycloak non verificato.

## Funzionalità GET (controller)

Decisioni: niente endpoint dedicati a struttura, unità e profili del tenant (li restituisce il dettaglio); unicità solo dentro creazione/modifica; indirizzi solo italiani per ora; endpoint senza logica leggono direttamente i repository di lettura di Application (CQRS leggero), i casi d'uso arrivano con le scritture.

| Controller | Endpoint |
|---|---|
| TenantsController | `GET /api/tenants?search=&page=1&pageSize=20` (ricerca su ragione sociale normalizzata e P.IVA, ordine per ragione sociale, pageSize max 100) · `GET /api/tenants/{id}` (anagrafica, indirizzo, profili, unità produttive) |
| TenantUsersController | `GET /api/tenants/{tenantId}/users` · `GET /api/tenants/{tenantId}/users/{userId}` |
| ProfilesController | `GET /api/profiles` (tutti i tenant, con `tenantId`) |
| GeographyController | `GET /api/geography/cities?term=` (min 2 caratteri, 503 se la fonte non risponde) |
| MeController | `GET /api/me` (nome, ruoli, utente PSAAS o null) |

- [x] Minimal API sostituite da controller; tutti richiedono la policy `Administrator`.
- [x] Utenti persistiti: tabella `Users` (migration `Users`, non applicata). `KeycloakId` unico; tenant facoltativo (amministratore di piattaforma); cancellazione tenant → cascata, cancellazione profilo → profilo utente a null.
- [x] `ITenantReadRepository.GetAllAsync` (senza chiamanti) sostituito da `GetPageAsync`; `ProfileReadModel` ha ora `TenantId`.
- [x] Test: Backend 17/17 (repository finti), Infra 23/23 (paginazione e utenti su EF InMemory), Auth 8/8.
- Aperto: gli utenti non hanno ancora un modo per essere creati, quindi le liste saranno vuote finché non arriva la scrittura.
