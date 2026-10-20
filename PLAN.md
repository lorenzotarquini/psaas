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

## Creazione tenant

Decisioni: tutti i dati del tenant obbligatori, duplicati controllati nel server e segnalati come errore di campo (400 `NotUnique`); profili e unità produttive facoltativi nella stessa richiesta; ragione sociale max 20 caratteri (invariato); indirizzo solo italiano: paese fisso `IT`, città deve essere un comune della lista e il CAP uno dei suoi (se la lista non risponde, si accetta com'è scritto); un tenant cancellato (soft delete, da fare) continua a bloccare ragione sociale, P.IVA e PEC.

- [x] `POST /api/tenants` → 201 + `Location` + dettaglio, oppure 400 `ValidationProblem` con un codice per campo (`companyName`, `profiles[0].name`, `productiveUnits[1].profileKeys[0]`, ...).
- [x] Primo caso d'uso in Application: `TenantCreateService` + `AddPsaasApplication()` (registra anche `TenantCreateValidator`, prima in Infrastructure). Nuovo codice errore `InvalidValue`.
- [x] Le unità collegano i profili della stessa richiesta tramite `key` scelta dal client; nomi unici (senza distinzione maiuscole) dentro la richiesta.
- [x] Test: 9 nuovi test di creazione nel backend (Backend 26/26).
- [ ] Prossimi: modifica di tenant, profili e unità; soft delete del tenant; creazione utenti (righe 6-7, da decidere: Keycloak o collegamento a utente esistente).

## Revisione: DTO e logging

- [x] DTO del backend spostati da dentro i controller a `Contracts/` (un file per DTO).
- [x] Rimosso il ripiego su `NullLogger` nei 5 repository di Infrastructure; `TenantCreateService` logga esito, rifiuti (codici per campo, nessun dato personale), corse sui duplicati e lista comuni non raggiungibile.
- [x] ~~Livelli interni con `ILogger<T>`~~ → sostituito, vedi sotto.
- [x] Bugfix NLog: la configurazione veniva letta prima che l'host conoscesse l'ambiente (override di Development ignorati se l'ambiente non era una variabile del processo); ora si ricarica dalla configurazione dell'host.
- [x] Bugfix NLog: le regole `Microsoft.*`/`System.*` con `minLevel: Warn, final` lasciavano passare Debug/Info del framework; ora le scartano (`maxLevel: Info, final`). Sezione `Logging` rimossa: le regole NLog sono l'unico filtro. In Development i log PSAAS sono a Debug.
- [x] Test: 2 test di logging (log di Application arrivano a NLog; livelli corretti). Backend 28/28.
- [x] Log anche nel backend: ogni controller ha un `ILogger<T>` obbligatorio e registra l'esito di ogni operazione (Info; ricerca comuni a Debug perché chiamata a ogni tasto, 503 a Warning).
- [x] Tornano visibili i log utili del framework: avvio (`Microsoft.Hosting.Lifetime`) e una riga per inizio/fine di ogni richiesta HTTP con stato e durata (`Microsoft.AspNetCore.Hosting.Diagnostics`). Il resto di `Microsoft.*`/`System.*` solo da Warning. Override di Development ora su `rules:4` (gli override JSON di NLog vanno per indice: se si aggiunge una regola va aggiornato).
- [x] **Decisione: NLog ovunque, niente Microsoft.Extensions.Logging nel codice.** Ogni classe usa `private static readonly Logger Logger = LogManager.GetCurrentClassLogger();` (pacchetto `NLog` in Application, Infrastructure e Auth; nel backend tramite `NLog.Web.AspNetCore`). Il logger non passa più dal costruttore, quindi non esiste un caso "logger mancante". Convertiti backend (5 controller), Application (`TenantCreateService`), Infrastructure (5 repository + `ItalianCityProvider`) e Auth (`KeycloakClientCredentialsHandler`, guard TLS). I log del framework ASP.NET passano comunque da Microsoft.Extensions.Logging e arrivano a NLog tramite `UseNLog()`.

## Decisione: utenti solo in Keycloak

Tutto ciò che riguarda l'utente sta in Keycloak: niente tabella `Users` nel DB.
- [x] Rimossi migration `Users` (mai applicata), `UserDbModel`, `UserReadRepository`, `IUserReadRepository`, `UserReadModel`, `TenantUsersController` (`GET /api/tenants/{id}/users[/{userId}]`).
- [x] `GET /api/me` legge solo il token: `keycloakId` (`sub`), `name`, `email`, `roles`.
- [ ] Se servono gli utenti di un tenant: leggerli dall'API di amministrazione di Keycloak (client M2M, `PSAAS.Auth` modalità MachineToMachine) con tenant/profilo come attributi o gruppi Keycloak. Da decidere.
