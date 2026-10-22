# UC12 – Consultare l'audit log e lo storico di un cliente

**Attore:** Amministratore
**Epica:** EXP-10 (proposta di espansione, [ADMIN-PORTAL.md](../admin-portal/ADMIN-PORTAL.md))
**Obiettivo:** sapere chi ha fatto cosa, quando e su quale cliente.

## Flusso principale
1. Ogni modifica fatta dal portale (attivazione di un cliente, modifica di anagrafica e organizzazione, creazione di un utente, e le operazioni delle epiche successive) viene registrata con autore, ora, oggetto e dettagli.
2. L'amministratore apre l'audit log e vede gli eventi dal più recente, a pagine.
3. Filtra per tipo di azione, per testo (autore o oggetto) o per periodo.
4. Dalla scheda di un cliente vede lo storico di quel solo cliente.

## Flussi alternativi
- **1a. La registrazione non riesce:** la modifica resta valida; l'errore compare nel log del backend.
- **2a. Nessun evento:** il sistema mostra un elenco vuoto.

## Regole
- R1. Il registro è solo in aggiunta: nessun evento si modifica o si cancella dal portale.
- R2. L'autore è l'amministratore del token (nome e id Keycloak); per le operazioni automatiche è "Sistema".
- R3. Il nome dell'oggetto è quello al momento dell'evento: una rinomina successiva non cambia lo storico.
- R4. Una modifica anagrafica registra solo i campi cambiati, con valore precedente e nuovo; salvare senza cambiare nulla non registra niente.
- R5. Una modifica dell'organizzazione registra profili e unità aggiunti, tolti e rinominati, e quanti accessi sono cambiati.
- R6. La creazione di un utente compare nello storico di ogni cliente di cui ha ricevuto profili.
- R7. Pagine da 1 a 200 eventi.

## Scenari di accettazione

Test: [`UC12_AuditLog`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC12_AuditLog.cs)

### UC12-S1

Test: `UC12_AuditLog.S1_AttivazioneRegistrataConAutore`

```gherkin
# language: it
Funzionalità: Audit log

Scenario: L'attivazione di un cliente è registrata con il suo autore
  Quando l'amministratore "admin.psaas" attiva il cliente "Alfa Srl"
  Allora l'audit log contiene "tenant.created" su "Alfa Srl" con autore "admin.psaas"
```

### UC12-S2

Test: `UC12_AuditLog.S2_ModificaAnagraficaConPrimaEDopo`

```gherkin
# language: it
Funzionalità: Audit log

Scenario: La modifica dell'anagrafica registra solo i campi cambiati
  Dato che esiste il cliente "Alfa Srl" con PEC "alfa@pec.it"
  Quando l'amministratore cambia solo la PEC in "nuova@pec.it"
  Allora l'evento "tenant.registry.updated" riporta la PEC da "alfa@pec.it" a "nuova@pec.it" e nessun altro campo
```

### UC12-S3

Test: `UC12_AuditLog.S3_ModificaOrganizzazioneConAggiunteETolte`

```gherkin
# language: it
Funzionalità: Audit log

Scenario: La modifica dell'organizzazione registra cosa è stato aggiunto e tolto
  Dato che il cliente "Alfa Srl" ha il profilo "Operatore"
  Quando l'amministratore toglie "Operatore" e aggiunge il profilo "Revisore"
  Allora l'evento "tenant.organization.updated" riporta "Revisore" aggiunto e "Operatore" tolto
```

### UC12-S4

Test: `UC12_AuditLog.S4_UtenteNelloStoricoDiOgniCliente`

```gherkin
# language: it
Funzionalità: Audit log

Scenario: La creazione di un utente compare nello storico di ogni suo cliente
  Dato che "Alfa Srl" ha il profilo "Operatore" e "Beta Snc" il profilo "Revisore"
  Quando l'amministratore crea "Mario Rossi" con entrambi i profili
  Allora lo storico di "Alfa Srl" e quello di "Beta Snc" contengono "user.created" su "Mario Rossi"
```

### UC12-S5

Test: `UC12_AuditLog.S5_StoricoDiUnSoloCliente`

```gherkin
# language: it
Funzionalità: Audit log

Scenario: Lo storico di un cliente contiene solo i suoi eventi, dal più recente
  Dato che sono stati attivati "Alfa Srl" e "Beta Snc" e poi modificata l'organizzazione di "Alfa Srl"
  Quando l'amministratore apre lo storico di "Alfa Srl"
  Allora vede la modifica dell'organizzazione e poi l'attivazione, e nulla di "Beta Snc"
```

### UC12-S6

Test: `UC12_AuditLog.S6_FiltriPerAzioneETesto`

```gherkin
# language: it
Funzionalità: Audit log

Scenario: Filtri per tipo di azione e per testo
  Dato che sono stati attivati "Alfa Srl" e "Beta Snc" ed è stato creato l'utente "Mario Rossi"
  Quando l'amministratore filtra per azioni "tenant."
  Allora vede solo le due attivazioni
  E filtrando per testo "beta" vede solo l'attivazione di "Beta Snc"
```

### UC12-S7

Test: `UC12_AuditLog.S7_PagineLimitate`

```gherkin
# language: it
Funzionalità: Audit log

Scenario: Dimensione della pagina fuori intervallo
  Quando l'amministratore chiede pagine da 500 eventi
  Allora la richiesta è rifiutata come non valida
```
