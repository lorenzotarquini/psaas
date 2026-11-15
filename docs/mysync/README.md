# MySync

Applicazione FinHorizon con la stessa struttura dell'Admin Portal: clean architecture propria, database proprio (`mysync-db`), backend ASP.NET Core (.NET 10) e frontend React + Vite con le stesse tecnologie.

## Stato

Scheletro funzionante, senza funzionalità di business:

- **Backend** (`src/MySync/FinHorizon.MySync.Backend`): NLog, autenticazione Keycloak (`FinHorizon.Auth`), `GET /health` pubblico, `GET /api/me` per qualsiasi utente autenticato del realm (policy `User`, nessun ruolo richiesto).
- **Database**: `mysync-db` è vuoto. `MySyncDbContext` non ha ancora entità né migrazioni: arriveranno con il modello dati.
- **Frontend** (`src/MySync/FinHorizon.MySync.Frontend`): login Keycloak (PKCE), pagina iniziale che mostra l'utente letto da `/api/me`.
- **Test**: `GEN_Sicurezza` (accesso negato senza token o con un token per un altro client, accesso con un token valido, `/health` pubblico) e la verifica della stringa di connessione; Vitest sul client API.

## Avvio

Con Aspire (`src/FinHorizon.AppHost`): Postgres crea `mysync-db`, poi partono `mysync-backend` e `mysync-frontend` su `http://localhost:5174` (porta fissa per Keycloak).

## Keycloak (realm `PSAAS`)

| Client | Tipo | Configurazione |
|---|---|---|
| `mysync-frontend` | Pubblico (Client authentication off), Standard flow, PKCE `S256` | Valid redirect URIs `http://localhost:5174/*`; Web origins `http://localhost:5174`; Valid post logout redirect URIs `http://localhost:5174/*`; mapper *Audience* che aggiunge `mysync-api` all'access token |
| `mysync-api` | Confidenziale, nessun flusso abilitato | Serve solo come audience del backend (`Keycloak:ValidAudiences`); ruoli client da aggiungere quando serviranno |
