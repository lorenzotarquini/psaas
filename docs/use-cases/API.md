# Analisi delle API (Admin Portal Backend)

Contratto HTTP che i test di accettazione esercitano. Tutti gli endpoint `/api/*` richiedono un token Keycloak valido (audience `adminportal-api`) con ruolo `Administrator`: senza token o con token non valido → `401`, senza ruolo → `403`.

Errori di validazione: `400` `ValidationProblem` con un codice per campo in `errors`, ad esempio `{"errors": {"companyName": ["Required"], "profiles[1].name": ["NotUnique"]}}`. Codici: `Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`, `InvalidValue`.

| Use case | Endpoint | Stato |
|---|---|---|
| UC1 | `GET /api/tenants?search=&page=1&pageSize=20` | esiste (manca la ricerca per PEC) |
| UC2 | `GET /api/tenants/{id}` | esiste |
| UC3 | `POST /api/tenants` | esiste |
| UC4 | `PUT /api/tenants/{id}` | **nuovo** |
| UC5 | `PUT /api/tenants/{id}/organization` | **nuovo** |
| UC6 | `POST /api/users` | esiste |
| UC7 | `GET /api/users?search=` | esiste |
| UC8 | `GET /api/me` | esiste |
| UC9 | `GET /api/profiles` | esiste |
| UC10 | `GET /api/geography/cities?term=` | esiste |
| UC11 | `GET /api/tenants/field-validation?field=&value=&excludeTenantId=` | esiste |

## Contratti esistenti (sintesi)

- **UC1** → `{ items: [{ id, companyName, vatNumber, completeVatNumber, certifiedEmail, city }], totalCount, page, pageSize }`.
- **UC2** → `{ id, companyName, vatNumber, completeVatNumber, certifiedEmail, address: { street, city, postalCode, country }, profiles: [{ id, name, description }], productiveUnits: [{ id, name, description, profileIds }] }`; `404` se non esiste.
- **UC3** → body `{ companyName, completeVatNumber, certifiedEmail, street, city, postalCode, profiles?: [{ key, name, description }], productiveUnits?: [{ name, description, profileKeys }] }`; `201` + `Location` + scheda (UC2).
- **UC8** → `{ keycloakId, name, email, roles }`.
- **UC9** → `[{ id, tenantId, name, description }]` ordinati per nome.
- **UC10** → `[{ name, provinceCode, postalCodes }]`; `400` se `term` ha meno di 2 caratteri, `503` se la lista comuni non risponde.

## Contratti nuovi

### UC4 – `PUT /api/tenants/{id}`
Body: `{ companyName, completeVatNumber, certifiedEmail, street, city, postalCode }` (stesse regole di UC3, unicità che esclude il cliente stesso).
Risposte: `200` + scheda aggiornata (UC2) · `400` errori per campo · `404` cliente inesistente.

### UC5 – `PUT /api/tenants/{id}/organization`
Sostituisce l'intera organizzazione del cliente: ciò che non compare nel body viene tolto.
```json
{
  "profiles": [
    { "id": "<id esistente oppure null>", "key": "op", "name": "Operatore", "description": null }
  ],
  "productiveUnits": [
    { "id": "<id esistente oppure null>", "name": "Stabilimento", "description": null, "profileKeys": ["op"] }
  ]
}
```
- Un profilo con `id` è uno esistente (rinominabile), senza `id` è nuovo. `key` è scelta dal client e serve alle unità per riferirsi ai profili della richiesta.
- Risposte: `200` + scheda aggiornata (UC2) · `400` errori per campo (`profiles[i].name`, `profiles[i].id` = `InvalidValue` se non è un profilo del cliente, `productiveUnits[i].id`, `productiveUnits[i].profileKeys[j]`) · `404` cliente inesistente.

### UC6 – `POST /api/users`
Body: `{ firstName, lastName, email, birthDate: "YYYY-MM-DD", profileIds: [] }`.
Risposte: `201` + utente (vedi UC7, senza `Location`: non esiste un endpoint di dettaglio) · `400` errori per campo (`firstName`, `lastName`, `email`, `birthDate` = `Required`; `email` = `InvalidFormat` o `NotUnique` se già usata; `birthDate` = `InvalidFormat` se non è una data `YYYY-MM-DD`; `profileIds[i]` = `InvalidValue` se il profilo non esiste) · `503` Keycloak non raggiungibile.
Effetto: utente creato in Keycloak (username = email in minuscolo, attributi `birthDate` e `profileIds`) e email di impostazione password (`UPDATE_PASSWORD`) inviata da Keycloak. Se Keycloak non riesce a inviare l'email, l'utente appena creato viene rimosso e la risposta è `503`.

### UC7 – `GET /api/users?search=`
Risposta: `[{ id, firstName, lastName, email, birthDate, profiles: [{ id, name, tenantId, tenantCompanyName }] }]` · `503` Keycloak non raggiungibile.
- `search` vuoto restituisce tutti gli utenti; altrimenti cerca il testo all'interno di nome, cognome, username ed email, senza maiuscole.
- Al massimo 100 utenti (non c'è paginazione).
- I profili salvati sull'utente che non esistono più vengono omessi.
- `birthDate` è `null` per gli utenti creati fuori dal portale (ad esempio direttamente in Keycloak).

### Keycloak come archivio utenti (UC6, UC7)
Il backend chiama l'API di amministrazione di Keycloak (`{Authority}` con `/realms/` → `/admin/realms/`) con il service account del client `adminportal-api`. Configurazione richiesta:
- client `adminportal-api` con *Client authentication* e *Service accounts* attivi; il segreto va in `Keycloak:ClientSecret` (user secrets o variabile d'ambiente `Keycloak__ClientSecret`, mai in `appsettings.json`);
- al service account vanno assegnati i ruoli `view-users` e `manage-users` del client `realm-management`;
- nel *User profile* del realm vanno dichiarati gli attributi `birthDate` e `profileIds` (multivalore), oppure vanno abilitati gli *Unmanaged attributes*: da Keycloak 24 gli attributi non dichiarati vengono scartati senza errore;
- il realm deve avere l'SMTP configurato, altrimenti la creazione risponde `503`.

Senza segreto o senza ruoli la chiamata fallisce con `500` e l'errore compare nel log.

### UC11 – `GET /api/tenants/field-validation?field=&value=&excludeTenantId=`
Verifica un solo campo dell'anagrafica con le regole del salvataggio (UC3/UC4), per controllarlo all'uscita dal campo.
- `field`: `companyName`, `completeVatNumber` oppure `certifiedEmail`. `value`: il valore scritto. `excludeTenantId` (facoltativo): il cliente in modifica, i cui valori non contano come duplicati.
- Risposte: `200` `{ "errorCode": null }` se valido, altrimenti `{ "errorCode": "NotUnique" }` (o `Required`, `InvalidFormat`, `MaxLengthExceeded`) · `400` con `errors.field = InvalidValue` se `field` non è verificabile.
- Non registra nulla: il salvataggio ripete tutti i controlli.

## Confini dei test di accettazione

- **Reali:** backend completo (pipeline HTTP, autenticazione, controller, Application, Infrastructure) e **Postgres** (Testcontainers).
- **Simulati**, perché sistemi esterni:
  - Keycloak come emittente di token: i test firmano token con la stessa forma di quelli Keycloak.
  - Keycloak come archivio utenti: porta `IUserDirectory` sostituita da un archivio in memoria.
  - Lista comuni: porta `IItalianCityProvider` sostituita da una lista fissa.
- Quello che i simulatori nascondono (chiamate HTTP verso Keycloak e verso la lista comuni, ordinamento e limite dei comuni) è verificato dai test unitari di Infrastructure.
