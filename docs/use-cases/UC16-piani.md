# UC16 – Gestire i piani commerciali

**Attore:** Amministratore
**Epica:** EXP-02
**Obiettivo:** definire cosa si vende: pacchetti di moduli con limiti e prezzo.

## Flusso principale
1. L'amministratore crea un piano con codice, nome, prezzo e periodicità, giorni di prova, limiti (utenti, unità produttive) e moduli inclusi.
2. Il sistema verifica e salva; il core è incluso sempre.
3. Un piano si modifica o si archivia (non più vendibile).

## Flussi alternativi
- **2a. Dati non validi o codice già usato:** il sistema non salva nulla e indica il campo.

## Regole
- R1. Codice unico, minuscolo; prezzo ≥ 0; periodicità mensile o annuale; prova 0–90 giorni; limiti ≥ 1 o assenti (illimitato).
- R2. I moduli inclusi devono esistere nel catalogo.
- R3. Modificare un piano non cambia le sottoscrizioni già in corso, che conservano prezzo e limiti di quando sono state fatte (UC17).

## Scenari di accettazione

Test: [`UC16_Piani`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC16_Piani.cs)

### UC16-S1

Test: `UC16_Piani.S1_CreareUnPiano`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Creare un piano
  Quando l'amministratore crea il piano "pro" a 99 € al mese con al massimo 50 utenti e il modulo "warehouse"
  Allora il piano "pro" include "core" e "warehouse"
```

### UC16-S2

Test: `UC16_Piani.S2_CodiceDuplicato`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Codice già usato
  Dato che esiste il piano "pro"
  Quando l'amministratore crea un altro piano "pro"
  Allora la richiesta è rifiutata con "NotUnique" sul codice
```

### UC16-S3

Test: `UC16_Piani.S3_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Dati non validi
  Quando l'amministratore crea un piano con prezzo negativo, periodicità "settimanale" e un modulo inesistente
  Allora la richiesta è rifiutata con "InvalidValue" su prezzo, periodicità e modulo
```
