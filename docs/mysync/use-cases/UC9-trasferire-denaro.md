# UC9 – Trasferire denaro tra due conti

**Attore:** Utente
**Obiettivo:** spostare soldi da un proprio conto a un altro (per esempio dal conto corrente a Revolut) senza che risulti una spesa o un ricavo.

## Flusso principale
1. Nella card "Conti" della dashboard l'utente preme il pulsante ⇄ (tooltip "Trasferisci denaro").
2. Si apre una modale con due righe, una per conto. La riga di sopra è il conto di partenza (segnaposto `-0`), quella di sotto il conto di destinazione (segnaposto `+0`).
3. L'utente sceglie i due conti e scrive l'importo nella riga di sopra, che diventa `-x`; quella di sotto mostra `+x`.
4. Se vuole invertire la direzione preme il pulsante tra le due righe: i conti restano dove sono, si scambiano i segni.
5. Facoltativamente cambia la data (oggi come predefinita) e aggiunge una nota.
6. Il sistema registra il trasferimento: toglie l'importo dal conto di partenza e lo aggiunge a quello di destinazione.
7. Il trasferimento compare nella lista dei movimenti come "Trasferimento · Conto A → Conto B".

## Flussi alternativi
- **1a. L'utente ha un solo conto:** il pulsante non c'è.
- **6a. Stesso conto nelle due righe:** `InvalidValue` sul conto di destinazione (`counterAccountId`); il frontend lo segnala prima di inviare.
- **6b. Conto di partenza o di destinazione inesistente o di un altro utente:** `InvalidValue` su quel conto.
- **6c. Data futura o precedente all'apertura di uno dei due conti:** `InvalidValue` sulla data.
- **6d. Importo mancante, zero o con più di 2 decimali:** come UC5.

## Regole
- R1. Un trasferimento non è né una spesa né un ricavo: non entra nei totali di spese e ricavi e non cambia il patrimonio netto, cambia solo i saldi dei due conti.
- R2. I due conti sono dell'utente e diversi tra loro.
- R3. Un trasferimento non ha categoria e non può essere la rata di un debito.
- R4. La data va dalla più recente delle due aperture dei conti a oggi, inclusi.
- R5. Si consulta, si modifica (anche invertendo la direzione) e si elimina come gli altri movimenti (UC8); nel filtro dei movimenti ha il tipo "Trasferimento" e compare filtrando per uno qualunque dei due conti.

## Note tecniche
Ogni movimento produce delle **scritture** (*postings*: conto e importo con segno): una spesa scrive `-x` sul suo conto, un ricavo `+x`, un trasferimento `-x` sul conto di partenza e `+x` su quello di destinazione. Saldi, patrimonio e andamento sono somme di scritture, quindi non dipendono dal tipo di movimento. Cosa scrive e quali campi usa un tipo lo decide la sua implementazione di `IMovementKind` (`ExpenseKind`, `IncomeKind`, `TransferKind`): un tipo nuovo è una classe in più. La migrazione `Transfers` ha creato le scritture dei movimenti già registrati, senza cambiare i saldi.

## Scenari di accettazione

Test: [`UC9_TrasferireDenaro`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC9_TrasferireDenaro.cs)

### UC9-S1

Test: `UC9_TrasferireDenaro.S1_TrasferimentoTraDueConti`

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: Trasferimento tra due conti
  Dato che "Mario" ha "Credem" con saldo 7.600,00 € e "Revolut" con saldo 23,00 €
  Quando trasferisce 500,00 € da "Credem" a "Revolut"
  Allora i saldi sono "Credem" 7.100,00 € e "Revolut" 523,00 €
  E il patrimonio netto resta 7.623,00 €
  E il movimento è un trasferimento da "Credem" a "Revolut" senza categoria
```

### UC9-S2

Test: `UC9_TrasferireDenaro.S2_StessoConto`

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: Stesso conto di partenza e di destinazione
  Quando "Mario" trasferisce 10,00 € da "Credem" a "Credem"
  Allora la richiesta è rifiutata con "InvalidValue" sul conto di destinazione
```

### UC9-S3

Test: `UC9_TrasferireDenaro.S3_ContoDiDestinazioneDiUnAltroUtente`

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: Conto di destinazione di un altro utente
  Dato che "Anna" ha il conto "Conto Anna"
  Quando "Mario" trasferisce 10,00 € da "Credem" a "Conto Anna"
  Allora la richiesta è rifiutata con "InvalidValue" sul conto di destinazione
  E il saldo di "Credem" non è cambiato
```

### UC9-S4

Test: `UC9_TrasferireDenaro.S4_DatiObbligatori`

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: Dati obbligatori
  Quando "Mario" registra un trasferimento senza compilare nessun altro campo
  Allora la richiesta è rifiutata con "Required" su data, importo, conto di partenza e conto di destinazione
```

### UC9-S5

Test: `UC9_TrasferireDenaro.S5_NeSpesaNeRicavo`

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: Né spesa né ricavo
  Dato che "Mario" ha trasferito 300,00 € da "Credem" a "Revolut"
  Quando filtra i movimenti di "Revolut"
  Allora vede il trasferimento
  E i totali di spese e ricavi sono 0,00 €
  E il patrimonio netto è lo stesso in ogni punto dell'andamento
```

### UC9-S6

Test: `UC9_TrasferireDenaro.S6_ModificaEdEliminazione`

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: Modifica ed eliminazione
  Dato che "Mario" ha trasferito 500,00 € da "Credem" (7.600,00 €) a "Revolut" (23,00 €)
  Quando lo modifica in 20,00 € da "Revolut" a "Credem"
  Allora i saldi sono "Credem" 7.620,00 € e "Revolut" 3,00 €
  Quando lo elimina
  Allora i saldi tornano "Credem" 7.600,00 € e "Revolut" 23,00 €
```

### UC9-S7

Test: `UC9_TrasferireDenaro.S7_CategoriaODebitoRifiutati`

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: Un trasferimento non ha categoria
  Quando "Mario" registra un trasferimento con la categoria "Spesa"
  Allora la richiesta è rifiutata con "InvalidValue" sulla categoria
```

### UC9-S8

Test: [`UC9_TrasferireDenaro.S8_ModaleDelTrasferimento`](../../../src/MySync/FinHorizon.MySync.Frontend/src/widgets/portfolio-lists/ui/UC9_TrasferireDenaro.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Trasferire denaro tra due conti

Scenario: La modale con le due righe
  Dato che "Mario" ha i conti "Credem" e "Revolut"
  Quando preme "Trasferisci denaro" nella card "Conti"
  Allora vede la riga di sopra con "-0" e quella di sotto con "+0"
  Quando scrive 500 nella riga di sopra
  Allora le righe mostrano "-500" e "+500"
  Quando preme il pulsante tra le due righe
  Allora le righe mostrano "+500" e "-500"
  Quando conferma
  Allora il trasferimento va da "Revolut" a "Credem"
```
