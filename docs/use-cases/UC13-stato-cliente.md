# UC13 – Gestire lo stato di un cliente

**Attore:** Amministratore
**Epica:** EXP-04 (ciclo di vita del cliente)
**Obiettivo:** sospendere, riattivare, mettere in disdetta o cessare un cliente, sapendo sempre perché.

## Flusso principale
1. L'amministratore apre la scheda del cliente e sceglie una delle azioni consentite dallo stato attuale.
2. Per sospensione, disdetta e cessazione indica il motivo.
3. Il sistema cambia lo stato, registra motivo e data, e lo scrive nello storico (UC12).

## Flussi alternativi
- **1a. Il cliente non esiste:** il sistema segnala che non è stato trovato.
- **2a. Transizione non consentita:** il sistema rifiuta la richiesta (`status` = `InvalidValue`).
- **2b. Motivo mancante dove serve:** il sistema rifiuta la richiesta (`reason` = `Required`).

## Regole
- R1. Stati: **Onboarding** (bozza in preparazione, UC14), **Trial** (prova), **Attivo**, **Sospeso** (accesso bloccato, dati conservati), **In disdetta** (periodo di preavviso), **Cessato** (cancellazione logica).
- R2. Transizioni consentite: Onboarding → Trial o Attivo · Trial → Attivo, Sospeso o Cessato · Attivo → Sospeso o In disdetta · Sospeso → Attivo o Cessato · In disdetta → Attivo (revoca) o Cessato · Cessato → nessuna.
- R3. Sospendere, mettere in disdetta e cessare richiedono un motivo.
- R4. Un cliente cessato continua a occupare ragione sociale, P.IVA e PEC (UC3-R8).
- R5. La scheda mostra stato, motivo e data dell'ultimo cambio, e la data di creazione ("Cliente dal").

## Scenari di accettazione

Test: [`UC13_StatoCliente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC13_StatoCliente.cs)

### UC13-S1

Test: `UC13_StatoCliente.S1_SospensioneConMotivo`

```gherkin
# language: it
Funzionalità: Stato del cliente

Scenario: Sospendere un cliente attivo
  Dato che il cliente "Alfa Srl" è attivo
  Quando l'amministratore lo sospende con motivo "Morosità"
  Allora il cliente risulta "Sospeso" con motivo "Morosità"
  E lo storico contiene il passaggio da "Attivo" a "Sospeso"
```

### UC13-S2

Test: `UC13_StatoCliente.S2_Riattivazione`

```gherkin
# language: it
Funzionalità: Stato del cliente

Scenario: Riattivare un cliente sospeso
  Dato che il cliente "Alfa Srl" è sospeso
  Quando l'amministratore lo riattiva
  Allora il cliente risulta "Attivo"
```

### UC13-S3

Test: `UC13_StatoCliente.S3_TransizioneNonConsentita`

```gherkin
# language: it
Funzionalità: Stato del cliente

Scenario: Un cliente cessato non torna attivo
  Dato che il cliente "Alfa Srl" è cessato
  Quando l'amministratore prova a riattivarlo
  Allora la richiesta è rifiutata con "InvalidValue" sullo stato
```

### UC13-S4

Test: `UC13_StatoCliente.S4_MotivoObbligatorio`

```gherkin
# language: it
Funzionalità: Stato del cliente

Scenario: La sospensione richiede un motivo
  Dato che il cliente "Alfa Srl" è attivo
  Quando l'amministratore lo sospende senza motivo
  Allora la richiesta è rifiutata con "Required" sul motivo
  E il cliente resta "Attivo"
```

### UC13-S5

Test: `UC13_StatoCliente.S5_CessatoOccupaIDatiUnivoci`

```gherkin
# language: it
Funzionalità: Stato del cliente

Scenario: Un cliente cessato occupa ancora i suoi dati univoci
  Dato che il cliente "Alfa Srl" è cessato
  Quando l'amministratore attiva un nuovo cliente "Alfa Srl"
  Allora la richiesta è rifiutata con "NotUnique" sulla ragione sociale
```

### UC13-S6

Test: `UC13_StatoCliente.S6_ClienteInesistente`

```gherkin
# language: it
Funzionalità: Stato del cliente

Scenario: Cliente inesistente
  Quando l'amministratore cambia lo stato di un cliente che non esiste
  Allora il sistema risponde che il cliente non è stato trovato
```
