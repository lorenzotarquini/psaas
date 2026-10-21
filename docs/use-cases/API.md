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
| UC6 | `POST /api/users` | **nuovo** |
| UC7 | `GET /api/users?search=` | **nuovo** |
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
Risposte: `201` + utente (vedi UC7) · `400` errori per campo (`email` = `NotUnique` se già usata, `profileIds[i]` = `InvalidValue` se il profilo non esiste) · `503` Keycloak non raggiungibile.
Effetto: utente creato in Keycloak (username = email, attributi `birthDate` e `profileIds`) e email di impostazione password inviata da Keycloak.

### UC7 – `GET /api/users?search=`
Risposta: `[{ id, firstName, lastName, email, birthDate, profiles: [{ id, name, tenantId, tenantCompanyName }] }]` · `503` Keycloak non raggiungibile.
I profili salvati sull'utente che non esistono più vengono omessi.

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
