# Analisi delle API (Admin Portal Backend)

Contratto HTTP che i test di accettazione esercitano. Tutti gli endpoint `/api/*` richiedono un token Keycloak valido (audience `adminportal-api`) con ruolo `Administrator`: senza token o con token non valido → `401`, senza ruolo → `403`.

Errori di validazione: `400` `ValidationProblem` con un codice per campo in `errors`, ad esempio `{"errors": {"companyName": ["Required"], "profiles[1].name": ["NotUnique"]}}`. Codici: `Required`, `InvalidFormat`, `MaxLengthExceeded`, `NotUnique`, `InvalidValue`, `LimitExceeded` (limite del piano, UC17).

| Use case | Endpoint | Stato |
|---|---|---|
| UC1, UC21 | `GET /api/tenants?search=&status=&planId=&moduleId=&province=&renewalWithinDays=&sort=&page=1&pageSize=20` · `GET /api/tenants/counts` · `GET /api/tenants/export` | esiste |
| UC2 | `GET /api/tenants/{id}` | esiste |
| UC3 | `POST /api/tenants` | esiste |
| UC4 | `PUT /api/tenants/{id}` | esiste |
| UC5 | `PUT /api/tenants/{id}/organization` | esiste |
| UC6 | `POST /api/users` | esiste |
| UC7 | `GET /api/users?search=&tenantId=&profileId=&status=&page=&pageSize=` | esiste |
| UC8 | `GET /api/me` | esiste |
| UC9 | `GET /api/profiles` | esiste |
| UC10 | `GET /api/geography/cities?term=` | esiste |
| UC11 | `GET /api/tenants/field-validation?field=&value=&excludeTenantId=` | esiste |
| UC12 | `GET /api/audit?tenantId=&action=&search=&from=&to=&page=&pageSize=` | esiste |
| UC13 | `POST /api/tenants/{id}/status` | esiste |
| UC14 | `POST /api/tenants` con `draft: true` · `DELETE /api/tenants/{id}` | esiste |
| UC15 | `GET /api/modules` · `POST /api/modules` · `PUT /api/modules/{id}/status` | esiste |
| UC16 | `GET /api/plans` · `POST /api/plans` · `PUT /api/plans/{id}` | esiste |
| UC17 | `GET /api/subscriptions` · `GET/PUT /api/tenants/{id}/subscription` | esiste |
| UC18 | `POST /api/tenants/{id}/modules/{moduleId}/retry` | esiste |
| UC19 | `GET/PUT /api/users/{id}` · `POST /api/users/{id}/invite|reset-password|disable|enable` | esiste |
| UC20 | `POST /api/users/import` | esiste |
| UC22 | `GET /api/saved-views?area=` · `POST /api/saved-views` · `DELETE /api/saved-views/{id}` | esiste |
| UC23 | `POST /api/billing/run` (e fatture emesse da UC17) | esiste |
| UC25 | `POST /api/usage` (ruolo `UsageReporter`) · `GET /api/tenants/{id}/usage?days=` | esiste |
| UC26 | `GET /api/system/services` | esiste |
| UC24 | `GET /api/invoices?tenantId=&status=&search=&page=&pageSize=` · `GET /api/invoices/summary` · `GET /api/invoices/{id}` · `POST /api/invoices/{id}/payments` · `POST /api/invoices/{id}/cancel` | esiste |

## Contratti esistenti (sintesi)

- **UC1/UC21** → `{ items: [{ id, companyName, vatNumber, completeVatNumber, certifiedEmail, city, province, code, status, createdAt, planId, planName, subscriptionStatus, maxUsers, renewsAt, modules: [{ id, name, icon, status }], profileIds, userCount }], totalCount, page, pageSize }`. Vedi la sezione UC21.
- **UC2** → `{ id, companyName, vatNumber, completeVatNumber, certifiedEmail, address: { street, city, postalCode, country }, profiles: [{ id, name, description, userCount }], productiveUnits: [{ id, name, description, profileIds }] }`; `404` se non esiste. `userCount` = utenti con quel profilo (da Keycloak), `null` se Keycloak non risponde: la scheda non fallisce.
- **UC3** → body `{ companyName, completeVatNumber, certifiedEmail, street, city, postalCode, profiles?: [{ key, name, description }], productiveUnits?: [{ name, description, profileKeys }] }`; `201` + `Location` + scheda (UC2).
- **UC8** → `{ keycloakId, name, email, roles }`.
- **UC9** → `[{ id, tenantId, tenantCompanyName, name, description }]` ordinati per nome.
- **UC10** → `[{ name, provinceCode, postalCodes }]`; `400` se `term` ha meno di 2 caratteri, `503` se la lista comuni non risponde.

## Contratti nuovi

### UC4 – `PUT /api/tenants/{id}`
Body: `{ companyName, completeVatNumber, certifiedEmail, street, city, postalCode }` (stesse regole di UC3, unicità che esclude il cliente stesso).
Risposte: `200` + scheda aggiornata (UC2) · `400` errori per campo · `404` cliente inesistente.
Un duplicato salvato nello stesso momento da un altro amministratore è riportato sul campo (`NotUnique`), non come errore generico.

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
- I profili tolti vengono rimossi anche dagli utenti in Keycloak (attributo `profileIds`). Se Keycloak non risponde l'organizzazione resta salvata: gli utenti conservano un id che le letture già omettono (UC7-R3), e l'evento è nel log.

### UC6 – `POST /api/users`
Body: `{ firstName, lastName, email, birthDate: "YYYY-MM-DD", profileIds: [] }`.
Risposte: `201` + `Location` (`/api/users/{id}`) + utente (vedi UC7) · `400` errori per campo (`firstName`, `lastName`, `email`, `birthDate` = `Required`; `email` = `InvalidFormat` o `NotUnique` se già usata; `birthDate` = `InvalidFormat` se non è una data `YYYY-MM-DD`; `profileIds[i]` = `InvalidValue` se il profilo non esiste) · `503` Keycloak non raggiungibile.
Effetto: utente creato in Keycloak (username = email in minuscolo, attributi `birthDate`, `profileIds`, `createdBy`, `invitesSent`, `lastInviteAt`, azione richiesta `UPDATE_PASSWORD` finché l'utente non imposta la password) e email di impostazione password (`UPDATE_PASSWORD`) inviata da Keycloak. Se Keycloak non riesce a inviare l'email, l'utente appena creato viene rimosso e la risposta è `503`.

### UC7 – `GET /api/users?search=&tenantId=&profileId=&status=&page=1&pageSize=50`
Risposta: `{ items: [utente], totalCount, page, pageSize }` con utente = `{ id, firstName, lastName, email, birthDate, profiles: [{ id, name, tenantId, tenantCompanyName, tenantStatus }], status, mfaRequired, lastLoginAt, createdAt, createdBy, invitesSent, lastInviteAt }` · `400` (`page` < 1, `pageSize` fuori 1–200, `status` sconosciuto = `InvalidValue`) · `503` Keycloak non raggiungibile.
- `search` vuoto restituisce tutti gli utenti; altrimenti cerca il testo all'interno di nome, cognome, username ed email, senza maiuscole. `tenantId`/`profileId` tengono gli utenti con un profilo di quel cliente / quel profilo; `status`: `Invited`, `Active`, `Suspended`, `Disabled` (UC19-R1).
- Ordine per cognome e nome. Filtri e pagine sono applicati sui primi 1000 utenti che Keycloak restituisce per la ricerca.
- `lastLoginAt` dall'ultimo evento `LOGIN` di Keycloak, `null` se non c'è o se gli eventi non sono disponibili.
- I profili salvati sull'utente che non esistono più vengono omessi.
- `birthDate` è `null` per gli utenti creati fuori dal portale (ad esempio direttamente in Keycloak).

### UC21 – Elenco clienti con filtri, ordinamento, conteggi ed export
- `GET /api/tenants` accetta, oltre a `search`, `page` e `pageSize`: `status` e `planId` (ripetibili, in OR tra loro), `moduleId`, `province` (sigla, senza maiuscole), `renewalWithinDays` (1–3650: rinnovo o fine prova entro N giorni, scaduti compresi), `sort` (`companyName`, `city`, `status`, `plan`, `renewsAt`, con `-` davanti per l'ordine decrescente). Parametri non validi → `400` con il parametro = `InvalidValue`.
- `renewsAt` è la fine della prova per i clienti in prova, altrimenti il prossimo rinnovo; `null` senza piano. `userCount` conta gli utenti con un profilo del cliente (Keycloak), `null` se Keycloak non risponde: l'elenco non fallisce.
- `GET /api/tenants/counts` → `{ all, byStatus: { Active, Trial, … }, renewingSoon }` su tutti i clienti; `renewingSoon` = non cessati con rinnovo entro 30 giorni.
- `GET /api/tenants/export` con gli stessi filtri (oppure `ids=` ripetuto per la selezione) → `text/csv` UTF-8 con BOM, colonne `ragioneSociale,partitaIva,pec,comune,provincia,stato,piano,rinnovo`, al massimo 5000 righe. I valori che iniziano con `= + - @` sono preceduti da un apice, perché un foglio di calcolo non li esegua come formule.
- La scheda (UC2) restituisce anche `address.province`: viene dall'elenco dei comuni al salvataggio dell'anagrafica (UC3/UC4).

### UC22 – Viste salvate
- `GET /api/saved-views?area=tenants` → `[{ id, area, name, query, createdAt }]` dell'amministratore del token.
- `POST /api/saved-views` `{ area, name, query }` → `201` + vista · `400` (`area` `InvalidValue` se non è `tenants` o `users`; `name` `Required`, `MaxLengthExceeded` oltre 40, `NotUnique` tra le proprie viste senza maiuscole, `LimitExceeded` oltre 20 viste).
- `DELETE /api/saved-views/{id}` → `204`, `404` se la vista non esiste o è di un altro amministratore.

### UC23 – Fatturazione dei periodi
- Fattura = `{ id, number: "2026/0001", kind: Invoice|CreditNote, tenantId, tenantName, tenantVatNumber, tenantCertifiedEmail, tenantSdiCode, issuedAt, dueAt, periodFrom, periodTo, lines: [{ description, quantity, unitPrice, amount }], net, vatRate, vat, total, status: Issued|Overdue|Paid|Cancelled, paidAt, paymentReference, remindersSent, lastReminderAt, relatedInvoiceId, relatedNumber, cancelReason }`. I dati del cliente sono quelli al momento dell'emissione.
- UC17 emette la fattura del primo periodo pagato (non in prova, prezzo > 0) e, a periodo in corso, la differenza pro-rata del cambio di piano (nota di credito se negativa).
- `POST /api/billing/run` → `200` `{ issued, expired, reminders, suspended }`: rinnovi (anche arretrati), fine prova, scadenze, solleciti, sospensioni. Lo stesso processo gira ogni `Billing:RunHours` ore (default 24, 0 = spento); ripeterlo non crea doppioni.
- Audit: `invoice.issued` (`{ number, kind, total }`), `subscription.expired` (`{ plan }`).

### UC24 – Incassi, solleciti, annullamenti
- `GET /api/invoices` dal più recente; `status` sconosciuto, `page` < 1 o `pageSize` fuori 1–200 → `400`. `search` cerca nella ragione sociale e nel numero.
- `GET /api/invoices/summary` → `{ toCollect, overdue, overdueCount, paidThisMonth, issuedThisMonth }` (totali IVA inclusa; il mese è quello UTC corrente).
- `POST /api/invoices/{id}/payments` `{ paidAt: "YYYY-MM-DD", reference }` → `200` + fattura · `400` (`paidAt` `Required`, `InvalidValue` se non è una data o è futura; `reference` `MaxLengthExceeded` oltre 100) · `409` se non è aperta · `404`.
- `POST /api/invoices/{id}/cancel` `{ reason }` → `200` + fattura annullata con `relatedInvoiceId` della nota di credito · `400` (`reason` `Required`) · `409` se pagata, annullata o è una nota di credito · `404`.
- Processo giornaliero: fatture oltre la scadenza → `Overdue`, sollecito ogni 7 giorni, sospensione del cliente attivo o in prova scaduto da più di `Billing:SuspendAfterDays` giorni (default 30, 0 = mai) con motivo "Morosità: fattura N".
- Audit: `invoice.paid` (`{ number, total, reference }`), `invoice.reminder.sent` (`{ number, reminder, total }`), `invoice.cancelled` (`{ number, reason, creditNote }`), `tenant.status.changed` per la sospensione.

### UC25 – Uso del cliente
- `POST /api/usage` `{ records: [{ tenantId, moduleId, metric, value, kind: counter|gauge, at? }] }` (1–500 righe) → `204` · `400` con `records[i].<campo>` (`tenantId` `Required`/`InvalidValue`; `moduleId` `InvalidValue` se il modulo non è attivo per il cliente; `metric` `InvalidFormat`, minuscole/cifre/`._-` fino a 60; `value` `InvalidValue` se negativo; `kind` `InvalidValue`; `at` `InvalidValue` se futuro o più vecchio di 366 giorni). Nessuna riga viene salvata se una è sbagliata.
- Autorizzazione: policy `UsageReporter` (ruoli `UsageReporter` o `Administrator`). È l'unico endpoint fuori dalla policy `Administrator`: un service account con il solo `UsageReporter` riceve `403` sul resto dell'API.
- Aggregazione giornaliera per cliente, modulo e metrica: i contatori si sommano, le misure tengono l'ultimo valore del giorno.
- `GET /api/tenants/{id}/usage?days=30` (1–365) → `{ days, users, maxUsers, activeUsers, productiveUnits, maxProductiveUnits, metrics: [{ moduleId, moduleName, metric, kind, total, points: [{ day, value }] }] }` · `404`. `users`/`activeUsers` `null` se Keycloak non risponde; attivo = accesso negli ultimi 30 giorni.

### UC26 – Stato dei servizi
- `GET /api/system/services` → `{ status, checkedAt, services: [{ key, status: Up|Degraded|Down|NotConfigured, latencyMs, detail }] }`. Chiavi: `database`, `keycloak` (documento di discovery usato per validare i token), `keycloak-admin` (conteggio su un profilo inesistente), `city-list`, `module-hooks` (`detail` = moduli in errore), `invoice-transmission` (oggi `NotConfigured`).
- Ogni verifica entro 5 s; un errore o un timeout rendono quel servizio `Down` con il motivo, mai la risposta intera. `status` è il peggiore tra i servizi configurati.

### UC19 – Gestire un utente
- `GET /api/users/{id}` → utente (come in UC7) · `404`.
- `PUT /api/users/{id}` body `{ firstName, lastName, birthDate: "YYYY-MM-DD", profileIds, mfaRequired }` → `200` + utente · `400` con i codici di UC6 (`firstName`, `lastName`, `birthDate`, `profileIds[i]` = `InvalidValue` o `LimitExceeded` per un cliente nuovo al limite) · `404`. L'email non si modifica. `mfaRequired: true` aggiunge l'azione `CONFIGURE_TOTP`.
- `POST /api/users/{id}/invite` (solo `Invited`), `/reset-password` (solo `Active` o `Suspended`), `/disable`, `/enable` → `200` + utente · `409` se lo stato non lo consente · `404`. Invito e reset fanno inviare a Keycloak l'email `execute-actions-email` con `UPDATE_PASSWORD`.
- Audit: `user.updated` (`{ email, changes: { <campo>: { before, after }, profilesAdded, profilesRemoved } }`, nello storico dei clienti di prima e di dopo), `user.invited`, `user.password.reset`, `user.disabled`, `user.enabled` (`{ email }`).

### UC20 – `POST /api/users/import`
Body `{ rows: [{ firstName, lastName, email, birthDate, profiles: "Cliente:Profilo|…" }], confirm }` (1–500 righe, altrimenti `400` con `rows` = `InvalidValue`). Risposta `200` `[{ row, email, errors: { <campo>: codice }, created }]`: con `confirm: false` solo anteprima; con `confirm: true` le righe senza errori vengono create con UC6 (`created` = utente) e gli eventuali errori del momento (es. `email` `NotUnique`) sono riportati sulla riga. `profiles` = `InvalidValue` se una coppia non esiste; un'email ripetuta nel file è `NotUnique` dalla seconda volta.

### Keycloak come archivio utenti (UC6, UC7, UC19)
Il backend chiama l'API di amministrazione di Keycloak (`{Authority}` con `/realms/` → `/admin/realms/`) con il service account del client `adminportal-api`. Configurazione richiesta:
- client `adminportal-api` con *Client authentication* e *Service accounts* attivi; il segreto va in `Keycloak:ClientSecret` (user secrets o variabile d'ambiente `Keycloak__ClientSecret`, mai in `appsettings.json`);
- al service account vanno assegnati i ruoli `view-users`, `manage-users` e `view-events` (ultimo accesso) del client `realm-management`;
- per l'ultimo accesso vanno salvati gli eventi utente `LOGIN` del realm (*Realm settings → Events*);
- nel *User profile* del realm vanno dichiarati gli attributi `birthDate`, `profileIds` (multivalore), `mfaRequired`, `createdBy`, `invitesSent` e `lastInviteAt`, oppure vanno abilitati gli *Unmanaged attributes*: da Keycloak 24 gli attributi non dichiarati vengono scartati senza errore;
- il realm deve avere l'SMTP configurato, altrimenti la creazione risponde `503`.

Senza segreto o senza ruoli le chiamate a `/api/users` rispondono `503` e la causa compare nel log come errore; all'avvio il backend avvisa se `Keycloak:ClientSecret` manca.

### UC11 – `GET /api/tenants/field-validation?field=&value=&excludeTenantId=`
Verifica un solo campo dell'anagrafica con le regole del salvataggio (UC3/UC4), per controllarlo all'uscita dal campo.
- `field`: `companyName`, `completeVatNumber` oppure `certifiedEmail`. `value`: il valore scritto. `excludeTenantId` (facoltativo): il cliente in modifica, i cui valori non contano come duplicati.
- Risposte: `200` `{ "errorCode": null, "conflictingTenant": null }` se valido, altrimenti il codice (`NotUnique`, `Required`, `InvalidFormat`, `MaxLengthExceeded`); con `NotUnique` anche `conflictingTenant: { id, companyName }`, il cliente che usa già il valore · `400` con `errors.field = InvalidValue` se `field` non è verificabile.
- Non registra nulla: il salvataggio ripete tutti i controlli.

### UC12 – `GET /api/audit`
Audit log, dal più recente: `{ items: [{ id, occurredAt, actorId, actorName, action, targetType, targetId, targetName, tenantId, details }], totalCount, page, pageSize }`.
- Filtri facoltativi: `tenantId` (storico di un cliente), `action` (codice o prefisso, es. `tenant.`), `search` (testo nel nome dell'autore o dell'oggetto, senza maiuscole), `from`/`to` (istanti ISO, `to` escluso). `pageSize` 1–200 (default 50), altrimenti `400`.
- Azioni registrate e `details`:

| `action` | Quando | `details` |
|---|---|---|
| `tenant.created` | UC3 | `{ profiles, productiveUnits }` (quanti) |
| `tenant.registry.updated` | UC4, solo se qualcosa cambia | `{ changes: { <campo>: { before, after } } }` |
| `tenant.organization.updated` | UC5, solo se qualcosa cambia | `{ profilesAdded, profilesRemoved, profilesRenamed: [{ before, after }], unitsAdded, unitsRemoved, unitsRenamed, accessChanges }` |
| `user.created` | UC6, un evento per ogni cliente dei profili (o uno senza cliente) | `{ email, tenant, profiles }` |

- L'autore viene dal token (`sub`, nome); le operazioni automatiche hanno autore `system`/"Sistema". La registrazione avviene dopo il salvataggio: se non riesce, la modifica resta valida e l'errore va nel log.

### Ciclo di vita e dati commerciali (UC13, UC14)
- Scheda (UC2) e righe dell'elenco (UC1) hanno anche `code` (es. `tn_8f2c41a9`, ricavato dall'id), stato e date: la scheda `lifecycle: { status, reason, changedAt, createdAt }`, `sdiCode`, `accountManager`; l'elenco `status` e `createdAt`.
- `status`: `Onboarding`, `Trial`, `Active`, `Suspended`, `Cancelling`, `Ceased`. Transizioni consentite e motivi obbligatori in [UC13](UC13-stato-cliente.md).
- UC3/UC4 accettano anche `sdiCode` (facoltativo, 6–7 lettere o cifre, salvato in maiuscolo; altrimenti `InvalidFormat`) e `accountManager` (testo libero).
- **UC13** `POST /api/tenants/{id}/status` body `{ status, reason }` → `200` + scheda · `400` (`status` = `InvalidValue` se la transizione non è consentita, `reason` = `Required`) · `404`.
- **UC14** `POST /api/tenants` con `draft: true` crea il cliente in `Onboarding` (`201` + scheda); si attiva con UC13 (`status: Active`). `DELETE /api/tenants/{id}` elimina una bozza con profili e unità (`204`); un cliente non in bozza → `409`; inesistente → `404`.
- Audit (UC12): `tenant.onboarding.started`, `tenant.onboarding.discarded`, `tenant.status.changed` (`{ from, to, reason }`); l'anagrafica registra anche `sdiCode` e `accountManager` cambiati.

### Catalogo, piani e sottoscrizioni (UC15–UC18)
- **UC15** `GET /api/modules` → `[{ id, name, description, icon, status, version, isCore, requires, permissions, provisionUrl, updatedAt, tenantCount }]` (il modulo `core` c'è sempre). `POST /api/modules` con il manifest `{ id, name, description?, version, icon?, status?, requires?, permissions?, provisionUrl?, deprovisionUrl? }` → `200` + modulo; un id già presente aggiorna il modulo. Errori: `id` `InvalidFormat` (minuscole, cifre, trattini, 2–40), `version` `InvalidFormat` (semver), `status` `InvalidValue` (`Beta`, `Available`, `Deprecated`), `requires[i]` `InvalidValue` (modulo inesistente o se stesso), `provisionUrl`/`deprovisionUrl` `InvalidFormat` (http/https). `PUT /api/modules/{id}/status` `{ status }` → `200` · `400` (`core` non lascia `Available`) · `404`.
- **UC16** `GET /api/plans` → `[{ id, code, name, description, price, billingPeriod, trialDays, maxUsers, maxProductiveUnits, moduleIds, active, subscriberCount }]`. `POST` (`201`) e `PUT /api/plans/{id}` (`200`, `404`) con `{ code, name, description?, price, billingPeriod, trialDays?, maxUsers?, maxProductiveUnits?, moduleIds?, active }`; `null` nei limiti = illimitato; `core` sempre incluso. Errori: `code` `InvalidFormat`/`NotUnique`, `price` `InvalidValue` (< 0), `billingPeriod` `InvalidValue` (`Monthly`, `Yearly`), `trialDays` `InvalidValue` (0–90), `maxUsers`/`maxProductiveUnits` `InvalidValue` (< 1), `moduleIds[i]` `InvalidValue`.
- **UC17** `PUT /api/tenants/{id}/subscription` `{ planId, trial, addOnModuleIds, autoRenew }` → `200` + sottoscrizione `{ tenantId, tenantName, planId, planName, status, price, billingPeriod, maxUsers, maxProductiveUnits, startsAt, trialEndsAt, renewsAt, autoRenew, addOnModuleIds, modules: [{ moduleId, name, icon, version, status, attempts, lastError, isAddOn, updatedAt }] }` · `400` (`planId` `Required`/`InvalidValue`/`LimitExceeded`, `addOnModuleIds[i]` `InvalidValue`) · `404`. `GET` della stessa risorsa → `200` oppure `204` se il cliente non ha piano. `GET /api/subscriptions` → tutte. `status`: `Trial`, `Active`, `Expired`, `Cancelled`; stato dei moduli: `Provisioning`, `Active`, `Failed`.
- **UC6** risponde `400` con `profileIds[i]` = `LimitExceeded` se il cliente di quel profilo ha già gli utenti massimi del piano.
- **UC18** `POST /api/tenants/{id}/modules/{moduleId}/retry` → `200` + sottoscrizione · `400` (`moduleId` `InvalidValue` se già attivo) · `404`. Il provisioning chiama `provisionUrl` con `POST { tenantId, moduleId, version }` (timeout 10 s; senza hook il modulo è subito attivo). Un job riprova i falliti ogni `Provisioning:RetryMinutes` minuti (default 5, 0 = spento) fino a 3 tentativi.
- Audit (UC12): `module.registered`, `module.updated` (`{ version }` o `{ status }`), `plan.created`, `plan.updated` (`{ price, billingPeriod }`), `subscription.changed` (`{ plan, previousPlan, trial, modulesAdded, modulesRemoved }`), `module.provisioned`, `module.provisioning.failed` (`{ module, version, error }`).

## Confini dei test di accettazione

- **Reali:** backend completo (pipeline HTTP, autenticazione, controller, Application, Infrastructure) e **Postgres** (Testcontainers).
- **Simulati**, perché sistemi esterni:
  - Keycloak come emittente di token: i test firmano token con la stessa forma di quelli Keycloak.
  - Keycloak come archivio utenti: porta `IUserDirectory` sostituita da un archivio in memoria.
  - Lista comuni: porta `IItalianCityProvider` sostituita da una lista fissa.
  - Hook di provisioning dei moduli: porta `IModuleProvisioner` sostituita da hook simulati (`FakeModuleHooks.Failing`).
  - Tempo: `TimeProvider` sostituito da un orologio che gli scenari spostano in avanti (`DaysPassAsync`) per scadenze e rinnovi; i token restano validati con l'ora reale.
- Quello che i simulatori nascondono (chiamate HTTP verso Keycloak e verso la lista comuni, ordinamento e limite dei comuni) è verificato dai test unitari di Infrastructure.
