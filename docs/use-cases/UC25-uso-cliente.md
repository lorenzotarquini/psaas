# UC25 – Misurare l'uso di un cliente

**Attore:** Moduli (invio dei dati) · Amministratore (consultazione)
**Epica:** EXP-09
**Obiettivo:** sapere quanto un cliente usa la piattaforma rispetto ai limiti del suo piano.

## Flusso principale
1. Ogni modulo invia periodicamente le sue metriche per cliente (es. documenti creati, spazio occupato) con il proprio service account.
2. L'amministratore apre la scheda Uso del cliente: vede utenti e unità produttive rispetto ai limiti del piano, gli utenti attivi negli ultimi 30 giorni e l'andamento di ogni metrica giorno per giorno.

## Flussi alternativi
- **1a. Dati non validi** (cliente o modulo inesistente, modulo non attivo per il cliente, metrica con caratteri non ammessi, valore negativo, data futura o più vecchia di un anno): il sistema rifiuta la richiesta indicando la riga e il campo; nessuna riga viene salvata.
- **2a. Keycloak non raggiungibile:** utenti e utenti attivi restano vuoti, il resto si vede.

## Regole
- R1. Una metrica è un **contatore** (i valori dello stesso giorno si sommano, es. documenti creati) o una **misura** (vale l'ultimo valore del giorno, es. spazio occupato).
- R2. L'invio richiede il ruolo `UsageReporter` (o `Administrator`) e accetta fino a 500 righe; chi ha solo `UsageReporter` non può usare il resto dell'API.
- R3. Il totale del periodo è la somma per i contatori e l'ultimo valore per le misure.
- R4. Utente attivo = ultimo accesso Keycloak negli ultimi 30 giorni.

## Scenari di accettazione

Test: [`UC25_UsoCliente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC25_UsoCliente.cs)

### UC25-S1

Test: `UC25_UsoCliente.S1_ContatoriEMisure`

```gherkin
# language: it
Funzionalità: Uso del cliente

Scenario: Contatori e misure
  Dato che "Alfa Srl" ha il modulo "warehouse"
  Quando il modulo invia 3 e poi 2 "documents.created" e poi 120 e 150 di "storage.mb" per oggi
  Allora la scheda Uso mostra 5 documenti creati e 150 MB oggi
```

### UC25-S2

Test: `UC25_UsoCliente.S2_LimitiDelPiano`

```gherkin
# language: it
Funzionalità: Uso del cliente

Scenario: Utenti e unità rispetto al piano
  Dato che "Alfa Srl" ha 2 utenti e 1 unità produttiva, il piano ammette 10 utenti e 3 unità, e un utente ha fatto accesso ieri
  Quando l'amministratore apre la scheda Uso
  Allora vede 2 utenti su 10, 1 attivo, e 1 unità su 3
```

### UC25-S3

Test: `UC25_UsoCliente.S3_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Uso del cliente

Scenario: Invio non valido
  Quando un modulo invia una riga per un modulo che "Alfa Srl" non ha e una con valore negativo
  Allora la richiesta è rifiutata con "InvalidValue" su "records[0].moduleId" e "records[1].value" e nessuna riga è salvata
```

### UC25-S4

Test: `UC25_UsoCliente.S4_RuoloDelModulo`

```gherkin
# language: it
Funzionalità: Uso del cliente

Scenario: Il ruolo del modulo basta solo per inviare
  Dato un service account con il solo ruolo "UsageReporter"
  Quando invia l'uso e poi chiede l'elenco dei clienti
  Allora l'invio è accettato e l'elenco è vietato
```
