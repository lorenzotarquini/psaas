# Admin Portal – Use case

Requisiti raccolti con l'intervista del 2026-10-20. Ogni use case ha un file dedicato; gli scenari di accettazione (Gherkin) verranno aggiunti a ogni file con un identificativo (`UCn-Sm`) e il link al test xUnit corrispondente.

## Il prodotto

PSAAS è un gestionale per aziende: una base comune estendibile con plugin. Ogni azienda cliente è un **tenant**.

L'**Admin Portal** è il pannello di controllo del gestore della piattaforma: serve a gestire i clienti, i loro utenti e (in futuro) comunicazioni, disattivazioni, licenze, report.

## Attori

| Attore | Descrizione |
|---|---|
| Amministratore | Il gestore della piattaforma o un suo collaboratore. Oggi è l'unico ruolo (`Administrator`, il più alto); in futuro potranno esserci ruoli con permessi ridotti. |
| Keycloak | Sistema esterno di identità: autentica l'amministratore e custodisce gli utenti dei clienti. |
| Lista comuni | Sistema esterno con i comuni italiani e i loro CAP. |

## Glossario

| Termine | Significato |
|---|---|
| Cliente (tenant) | Azienda che usa PSAAS. Identificata da ragione sociale, P.IVA e PEC, tutte uniche. |
| Unità produttiva | Parte indipendente di un cliente: un reparto (IT, commerciale) o, per aziende grandi, una filiale. |
| Profilo | Regola di un cliente che dà accesso a zero o più delle sue unità produttive. |
| Utente | Una persona fisica. Vive solo in Keycloak; ha zero o più profili, anche di clienti diversi. I clienti di un utente si ricavano dai suoi profili. |

## Use case

| Id | Use case | Stato |
|---|---|---|
| [UC1](UC1-cercare-clienti.md) | Cercare i clienti | in ambito |
| [UC2](UC2-consultare-cliente.md) | Consultare la scheda di un cliente | in ambito |
| [UC3](UC3-attivare-cliente.md) | Attivare un nuovo cliente | in ambito |
| [UC4](UC4-modificare-anagrafica.md) | Modificare l'anagrafica di un cliente | in ambito |
| [UC5](UC5-modificare-organizzazione.md) | Modificare l'organizzazione di un cliente | in ambito |
| [UC6](UC6-creare-utente.md) | Creare un utente | in ambito |
| [UC7](UC7-cercare-utenti.md) | Cercare gli utenti | in ambito |
| [UC8](UC8-chi-sono.md) | Sapere chi sono | in ambito |
| [UC9](UC9-panoramica-profili.md) | Consultare la panoramica dei profili | in ambito |
| [UC10](UC10-cercare-comune.md) | Cercare un comune (di supporto a UC3 e UC4) | in ambito |
| [UC11](UC11-verificare-campo-cliente.md) | Verificare un campo univoco del cliente (di supporto a UC3 e UC4) | in ambito |
| [UC12](UC12-audit-log.md) | Consultare l'audit log e lo storico di un cliente (EXP-10) | in ambito |
| [UC13](UC13-stato-cliente.md) | Gestire lo stato di un cliente (EXP-04) | in ambito |
| [UC14](UC14-onboarding-cliente.md) | Attivare un cliente con l'onboarding guidato (EXP-04) | in ambito |

Contratto HTTP: [API.md](API.md) (collection Postman: [PSAAS.AdminPortal.postman_collection.json](../postman/PSAAS.AdminPortal.postman_collection.json)). Test unitari derivati dagli scenari: [TEST-UNITARI.md](TEST-UNITARI.md).

Regola comune a tutti gli use case: l'amministratore deve essere autenticato su Keycloak e avere il ruolo `Administrator`; altrimenti nessuna operazione è consentita.

## Fuori ambito per ora

- Disattivazione / cancellazione (soft delete) di un cliente: funzionalità complessa, da definire.
- Comunicazioni ai clienti, licenze, abbonamenti, fatturazione, report (in corso di implementazione con le epiche EXP, vedi [ADMIN-PORTAL.md](../admin-portal/ADMIN-PORTAL.md)).
- Ruoli dell'Admin Portal con permessi ridotti.
- Modifica e cancellazione degli utenti.

## Scenari comuni

Test: [`GEN_Sicurezza`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/GEN_Sicurezza.cs)

### GEN-S1

Test: `GEN_Sicurezza.S1_OgniOperazioneRichiedeAutenticazione`

```gherkin
# language: it
Funzionalità: Accesso all'Admin Portal

Schema dello scenario: Ogni operazione richiede un amministratore autenticato
  Quando qualcuno chiama "<operazione>" senza essere autenticato
  Allora l'accesso è negato come non autenticato

  Esempi:
    | operazione                            |
    | GET /api/tenants                      |
    | GET /api/tenants/{id}                 |
    | POST /api/tenants                     |
    | PUT /api/tenants/{id}                 |
    | PUT /api/tenants/{id}/organization    |
    | POST /api/users                       |
    | GET /api/users                        |
    | GET /api/me                           |
    | GET /api/profiles                     |
    | GET /api/geography/cities?term=ro     |
    | GET /api/tenants/field-validation?field=companyName&value=x |
```

### GEN-S2

Test: `GEN_Sicurezza.S2_OgniOperazioneRichiedeIlRuoloAdministrator`

```gherkin
# language: it
Funzionalità: Accesso all'Admin Portal

Schema dello scenario: Ogni operazione richiede il ruolo Administrator
  Quando un utente autenticato senza ruolo "Administrator" chiama "<operazione>"
  Allora l'accesso è negato come non autorizzato

  Esempi: le stesse operazioni di GEN-S1
```

### GEN-S3

Test: `GEN_Sicurezza.S3_StatoDelServizioSenzaAutenticazione`

```gherkin
# language: it
Funzionalità: Accesso all'Admin Portal

Scenario: Lo stato del servizio è consultabile senza autenticazione
  Quando qualcuno chiama "GET /health" senza essere autenticato
  Allora il servizio risponde che è attivo
```
