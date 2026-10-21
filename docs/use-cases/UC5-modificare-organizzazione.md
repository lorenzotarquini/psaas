# UC5 – Modificare l'organizzazione di un cliente

**Attore:** Amministratore
**Obiettivo:** cambiare unità produttive e profili di un cliente dopo l'attivazione.

## Flusso principale
1. L'amministratore apre la scheda del cliente (UC2).
2. Aggiunge, rinomina o toglie unità produttive e profili, e cambia quali unità apre ogni profilo.
3. Il sistema verifica e salva tutte le modifiche insieme.
4. Il sistema mostra la scheda aggiornata.

## Flussi alternativi
- **1a. Il cliente non esiste:** il sistema segnala che il cliente non è stato trovato.
- **3a. Dati non validi:** il sistema non salva nulla e indica cosa correggere.

## Regole
- R1. Nomi obbligatori e unici all'interno del cliente, per i profili e per le unità (senza distinzione di maiuscole).
- R2. Un profilo può aprire solo unità dello stesso cliente.
- R3. Togliere un'unità produttiva è sempre consentito: i profili che la aprivano la perdono e possono restare senza unità.
- R4. Togliere un profilo è sempre consentito: gli utenti che lo avevano lo perdono e possono restare senza profili (nessun profilo sostitutivo o di fallback).
- R5. Un cliente può restare senza profili e senza unità.

## Scenari di accettazione

Test: [`UC5_ModificareOrganizzazione`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC5_ModificareOrganizzazione.cs)

### UC5-S1

Test: `UC5_ModificareOrganizzazione.S1_AggiungereProfiloEUnitaCollegati`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Aggiungere un profilo e un'unità collegati
  Dato che esiste il cliente "Alfa Srl" senza organizzazione
  Quando l'amministratore aggiunge il profilo "Operatore" e l'unità "Stabilimento" aperta da "Operatore"
  Allora la scheda mostra "Stabilimento" aperta da "Operatore"
```

### UC5-S2

Test: `UC5_ModificareOrganizzazione.S2_RinominareMantenendoLIdentita`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Rinominare un profilo e un'unità mantiene la loro identità e i collegamenti
  Dato che esiste il cliente "Alfa Srl" con il profilo "Operatore" che apre l'unità "Stabilimento"
  Quando l'amministratore rinomina il profilo in "Addetto" e l'unità in "Fabbrica"
  Allora la scheda mostra "Fabbrica" aperta da "Addetto"
  E profilo e unità hanno gli stessi identificativi di prima
```

### UC5-S3

Test: `UC5_ModificareOrganizzazione.S3_TogliereUnUnitaLasciaIlProfiloSenzaUnita`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Togliere un'unità lascia i profili che la aprivano senza unità
  Dato che esiste il cliente "Alfa Srl" con il profilo "Magazziniere" che apre solo l'unità "Magazzino"
  Quando l'amministratore toglie l'unità "Magazzino"
  Allora la scheda mostra il profilo "Magazziniere" senza unità
```

### UC5-S4

Test: `UC5_ModificareOrganizzazione.S4_TogliereUnProfiloLoToglieAgliUtenti`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Togliere un profilo lo toglie agli utenti che lo avevano
  Dato che esiste il cliente "Alfa Srl" con il profilo "Magazziniere"
  E l'utente "Mario Rossi" ha solo il profilo "Magazziniere"
  Quando l'amministratore toglie il profilo "Magazziniere"
  Allora cercando "Mario" l'utente risulta senza profili
```

### UC5-S5

Test: `UC5_ModificareOrganizzazione.S5_NomiDuplicati`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Nomi duplicati nell'organizzazione
  Dato che esiste il cliente "Alfa Srl"
  Quando l'amministratore salva due profili "Operatore" e "OPERATORE" e due unità "Magazzino"
  Allora la richiesta è rifiutata con "NotUnique" sul secondo profilo e sulla seconda unità
  E l'organizzazione non è cambiata
```

### UC5-S6

Test: `UC5_ModificareOrganizzazione.S6_ProfiloOUnitaDiUnAltroCliente`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Profili e unità di un altro cliente non sono utilizzabili
  Dato che esistono i clienti "Alfa Srl" e "Beta Snc", e "Beta Snc" ha il profilo "Operatore" e l'unità "Stabilimento"
  Quando l'amministratore salva l'organizzazione di "Alfa Srl" usando gli identificativi del profilo e dell'unità di "Beta Snc"
  Allora la richiesta è rifiutata con "InvalidValue" sull'identificativo del profilo e dell'unità
  E l'organizzazione di "Beta Snc" non è cambiata
```

### UC5-S7

Test: `UC5_ModificareOrganizzazione.S7_TogliereTuttaLOrganizzazione`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Un cliente può restare senza organizzazione
  Dato che esiste il cliente "Alfa Srl" con profili e unità produttive
  Quando l'amministratore salva un'organizzazione vuota
  Allora la scheda mostra nessun profilo né unità produttiva
```

### UC5-S8

Test: `UC5_ModificareOrganizzazione.S8_ClienteInesistente`

```gherkin
# language: it
Funzionalità: Modificare l'organizzazione di un cliente

Scenario: Cliente inesistente
  Quando l'amministratore modifica l'organizzazione di un cliente che non esiste
  Allora il sistema risponde che il cliente non è stato trovato
```
