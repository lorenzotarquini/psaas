# UC2 – Consultare la scheda di un cliente

**Attore:** Amministratore
**Obiettivo:** vedere tutto ciò che riguarda un cliente.

## Flusso principale
1. L'amministratore sceglie un cliente (di solito da UC1).
2. Il sistema mostra anagrafica (ragione sociale, P.IVA, PEC, indirizzo), unità produttive e profili, con le unità aperte da ogni profilo.

## Flussi alternativi
- **2a. Il cliente non esiste:** il sistema segnala che il cliente non è stato trovato.

## Regole
- R1. Un cliente può non avere unità produttive né profili; un profilo può non aprire nessuna unità.
- R2. Per ogni profilo si vede quanti utenti lo hanno (da Keycloak); se Keycloak non risponde la scheda si apre comunque, senza i conteggi.

## Scenari di accettazione

Test: [`UC2_ConsultareCliente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC2_ConsultareCliente.cs)

### UC2-S1

Test: `UC2_ConsultareCliente.S1_SchedaConAnagraficaEOrganizzazione`

```gherkin
# language: it
Funzionalità: Consultare la scheda di un cliente

Scenario: Scheda completa di anagrafica e organizzazione
  Dato che esiste il cliente "Alfa Srl" con sede in "Via Roma 1", "Roma", "00118"
  E ha il profilo "Operatore" che apre l'unità produttiva "Stabilimento"
  E ha l'unità produttiva "Magazzino" non aperta da nessun profilo
  Quando l'amministratore apre la scheda di "Alfa Srl"
  Allora vede l'indirizzo "Via Roma 1", "Roma", "00118", "IT"
  E vede il profilo "Operatore" e le unità "Magazzino" e "Stabilimento"
  E vede che "Stabilimento" è aperta da "Operatore" e "Magazzino" da nessuno
```

### UC2-S2

Test: `UC2_ConsultareCliente.S2_ClienteSenzaOrganizzazione`

```gherkin
# language: it
Funzionalità: Consultare la scheda di un cliente

Scenario: Cliente senza profili né unità produttive
  Dato che esiste il cliente "Alfa Srl" senza organizzazione
  Quando l'amministratore apre la scheda di "Alfa Srl"
  Allora vede l'anagrafica e nessun profilo né unità produttiva
```

### UC2-S3

Test: `UC2_ConsultareCliente.S3_ClienteInesistente`

```gherkin
# language: it
Funzionalità: Consultare la scheda di un cliente

Scenario: Cliente inesistente
  Quando l'amministratore apre la scheda di un cliente che non esiste
  Allora il sistema risponde che il cliente non è stato trovato
```

### UC2-S4

Test: `UC2_ConsultareCliente.S4_UtentiPerProfilo`

```gherkin
# language: it
Funzionalità: Consultare la scheda di un cliente

Scenario: Quanti utenti hanno ogni profilo
  Dato che il cliente "Alfa Srl" ha i profili "Operatore" e "Revisore"
  E che 2 utenti hanno il profilo "Operatore"
  Quando l'amministratore apre la scheda di "Alfa Srl"
  Allora vede 2 utenti su "Operatore" e 0 su "Revisore"
```

### UC2-S5

Test: `UC2_ConsultareCliente.S5_SchedaSenzaKeycloak`

```gherkin
# language: it
Funzionalità: Consultare la scheda di un cliente

Scenario: Keycloak non raggiungibile
  Dato che Keycloak non risponde
  Quando l'amministratore apre la scheda di un cliente con profili
  Allora vede la scheda completa, senza il numero di utenti per profilo
```
