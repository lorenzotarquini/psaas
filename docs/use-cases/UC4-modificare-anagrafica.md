# UC4 – Modificare l'anagrafica di un cliente

**Attore:** Amministratore
**Obiettivo:** aggiornare i dati anagrafici di un cliente esistente.

## Flusso principale
1. L'amministratore apre la scheda del cliente (UC2) e modifica ragione sociale, P.IVA completa, PEC o indirizzo.
2. Il sistema verifica i dati e salva le modifiche.
3. Il sistema mostra la scheda aggiornata.

## Flussi alternativi
- **1a. Il cliente non esiste:** il sistema segnala che il cliente non è stato trovato.
- **2a. Dati non validi o duplicati:** il sistema non salva nulla e indica, campo per campo, cosa correggere.
- **2b. La lista comuni non è raggiungibile:** città e CAP vengono accettati come scritti.

## Regole
- R1. Tutti i dati anagrafici sono modificabili, P.IVA compresa.
- R2. Valgono le stesse regole di UC3 (R1–R5, e R9 per un privato). L'unicità esclude il cliente stesso: si possono salvare i propri valori invariati.
- R3. Il tipo (azienda o privato) non si modifica: la richiesta non lo contiene e valgono le regole del tipo scelto alla creazione.

## Scenari di accettazione

Test: [`UC4_ModificareAnagrafica`](../../src/AdminPortal/Tests/FinHorizon.AdminPortal.Backend.Tests/UC4_ModificareAnagrafica.cs)

### UC4-S1

Test: `UC4_ModificareAnagrafica.S1_ModificaDiTuttiIDatiPartitaIvaCompresa`

```gherkin
# language: it
Funzionalità: Modificare l'anagrafica di un cliente

Scenario: Modifica di tutti i dati anagrafici, P.IVA compresa
  Dato che esiste il cliente "Alfa Srl" con P.IVA completa "IT11111111111"
  Quando l'amministratore lo modifica in "Alfa Spa", P.IVA completa "IT99999999999", PEC "nuova@pec.it", sede "Corso Buenos Aires 10", "Milano", "20124"
  Allora la scheda mostra i nuovi dati, con P.IVA "99999999999" e città "Milano"
  E l'organizzazione del cliente non è cambiata
```

### UC4-S2

Test: `UC4_ModificareAnagrafica.S2_SalvareValoriInvariatiNonEUnDuplicato`

```gherkin
# language: it
Funzionalità: Modificare l'anagrafica di un cliente

Scenario: Salvare i propri valori invariati non è un duplicato
  Dato che esiste il cliente "Alfa Srl"
  Quando l'amministratore lo salva senza cambiare nulla
  Allora la modifica è accettata
```

### UC4-S3

Test: `UC4_ModificareAnagrafica.S3_DatiUniciDiUnAltroCliente`

```gherkin
# language: it
Funzionalità: Modificare l'anagrafica di un cliente

Scenario: Valori unici già usati da un altro cliente
  Dato che esistono i clienti "Alfa Srl" e "Beta Snc"
  Quando l'amministratore modifica "Beta Snc" usando ragione sociale, P.IVA completa e PEC di "Alfa Srl"
  Allora la richiesta è rifiutata con "NotUnique" su ragione sociale, P.IVA completa e PEC
  E "Beta Snc" non è cambiato
```

### UC4-S4

Test: `UC4_ModificareAnagrafica.S4_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Modificare l'anagrafica di un cliente

Scenario: Dati non validi
  Dato che esiste il cliente "Alfa Srl"
  Quando l'amministratore svuota la ragione sociale e scrive la P.IVA completa "12345"
  Allora la richiesta è rifiutata con "Required" sulla ragione sociale e "InvalidFormat" sulla P.IVA completa
```

### UC4-S5

Test: `UC4_ModificareAnagrafica.S5_IndirizzoNonPresenteNellaListaComuni`

```gherkin
# language: it
Funzionalità: Modificare l'anagrafica di un cliente

Scenario: CAP che non appartiene alla città
  Dato che esiste il cliente "Alfa Srl"
  Quando l'amministratore cambia la sede in città "Roma" con CAP "20100"
  Allora la richiesta è rifiutata con "InvalidValue" sul CAP
```

### UC4-S6

Test: `UC4_ModificareAnagrafica.S6_ClienteInesistente`

```gherkin
# language: it
Funzionalità: Modificare l'anagrafica di un cliente

Scenario: Cliente inesistente
  Quando l'amministratore modifica un cliente che non esiste
  Allora il sistema risponde che il cliente non è stato trovato
```

### UC4-S7

Test: `UC4_ModificareAnagrafica.S7_UnPrivatoRestaPrivato`

```gherkin
# language: it
Funzionalità: Modificare l'anagrafica di un cliente

Scenario: Un privato resta privato
  Dato che esiste il privato "Mario Rossi" con codice fiscale "RSSMRA85T10A562S"
  Quando l'amministratore lo rinomina in "Mario Giuseppe Rossi" con codice fiscale "VRDGPP80A01F205X"
  Allora la scheda mostra il privato "Mario Giuseppe Rossi" con il nuovo codice fiscale
  Ma salvando la P.IVA "IT11111111111" al posto del codice fiscale la richiesta è rifiutata con "InvalidFormat"
```
