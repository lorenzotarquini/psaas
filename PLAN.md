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
