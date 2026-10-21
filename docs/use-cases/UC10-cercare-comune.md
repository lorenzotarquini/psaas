# UC10 – Cercare un comune

**Attore:** Amministratore
**Sistemi coinvolti:** Lista comuni
**Obiettivo:** compilare città e CAP di un indirizzo (supporto a UC3 e UC4).

## Flusso principale
1. L'amministratore scrive almeno 2 caratteri del nome di un comune.
2. Il sistema mostra fino a 20 comuni che contengono quel testo, con provincia e CAP; prima quelli con il nome identico, poi quelli che iniziano con il testo.

## Flussi alternativi
- **1a. Meno di 2 caratteri:** la ricerca è rifiutata.
- **2a. La lista comuni non è raggiungibile:** il sistema segnala che il servizio non è disponibile.

## Regole
- R1. La ricerca ignora le maiuscole.

## Scenari di accettazione

Test: [`UC10_CercareComune`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC10_CercareComune.cs)

### UC10-S1

Test: `UC10_CercareComune.S1_ComuniConProvinciaECap`

```gherkin
# language: it
Funzionalità: Cercare un comune

Scenario: Comuni trovati con provincia e CAP
  Dato che la lista comuni contiene "Roma" (RM) con CAP "00118"
  Quando l'amministratore cerca "ro"
  Allora vede "Roma" con provincia "RM" e CAP "00118"
```

### UC10-S2

Test: `UC10_CercareComune.S2_TestoTroppoCorto`

```gherkin
# language: it
Funzionalità: Cercare un comune

Scenario: Meno di 2 caratteri
  Quando l'amministratore cerca "r"
  Allora la richiesta è rifiutata come non valida
```

### UC10-S3

Test: `UC10_CercareComune.S3_ListaComuniNonRaggiungibile`

```gherkin
# language: it
Funzionalità: Cercare un comune

Scenario: La lista comuni non è raggiungibile
  Dato che la lista comuni non risponde
  Quando l'amministratore cerca "ro"
  Allora il sistema segnala che il servizio non è disponibile
```
