# UC8 – Consultare, modificare ed eliminare i movimenti

**Attore:** Utente
**Obiettivo:** ritrovare i propri movimenti e correggere quelli sbagliati, così che saldi, debiti e patrimonio restino veri.

Con UC5 (creazione) completa il CRUD dei movimenti (D1).

## Flusso principale – Consultare
1. Dalla dashboard l'utente apre "Tutti i movimenti".
2. Il sistema mostra i movimenti dal più recente (per data, poi per inserimento), a pagine, con data, tipo, importo con segno, conto, categoria, eventuale debito e nota.
3. L'utente filtra per periodo (da / a), tipo, conto, categoria o debito.
4. Il sistema mostra i movimenti che rispettano tutti i filtri e il totale di spese e ricavi del filtro.

## Flusso principale – Modificare
1. L'utente apre un movimento.
2. Cambia uno o più campi: tipo, data, importo, conto, categoria, debito, nota.
3. Il sistema verifica il movimento con le stesse regole di UC5 e lo salva.
4. Il sistema ricalcola saldi dei conti, residui dei debiti e andamento del patrimonio coinvolti, sia quelli di prima sia quelli di dopo.

## Flusso principale – Eliminare
1. L'utente sceglie "Elimina" su un movimento.
2. Il sistema chiede conferma, mostrando data, importo e conto del movimento e che l'eliminazione non si può annullare.
3. L'utente conferma.
4. Il sistema elimina il movimento e ricalcola saldo, eventuale residuo e andamento.

## Flussi alternativi
- **C3a. Nessun movimento rispetta i filtri:** il sistema lo dice e offre di togliere i filtri; non è un errore.
- **C3b. Periodo con "da" successivo ad "a":** `InvalidValue` sul periodo.
- **M3a. Dati non validi:** come UC5 (5a–5g); il movimento resta com'era.
- **M3b. La rata modificata supera il residuo:** il controllo usa il residuo **senza** la rata che si sta modificando (una rata di 300 € su un residuo di 0 può diventare 250 €, non 301 €); `InvalidValue` sull'importo.
- **M3c. Il cambio di tipo da spesa a ricavo con un debito collegato:** `InvalidValue` sul debito (D5); l'utente deve togliere il debito.
- **E3a. L'utente annulla:** nulla cambia.
- **\*a. Movimento inesistente o di un altro utente:** `404`.

## Regole
- R1. Modificare un movimento equivale a eliminarlo e registrarlo di nuovo con gli stessi id e data di inserimento: tutte le regole di UC5 valgono sul nuovo stato.
- R2. Un debito estinto torna attivo se una sua rata viene eliminata o ridotta; un debito attivo si estingue se una rata modificata porta il residuo a 0.
- R3. Un debito estinto può essere scelto in modifica solo dal movimento che è già una sua rata.
- R4. L'eliminazione è definitiva (i movimenti sono dati personali dell'utente; nessun cestino nell'MVP).
- R5. Filtri e pagina sono nell'indirizzo, come nell'Admin Portal, per tornare alla stessa vista.
- R6. Pagine da 1 a 100 movimenti, predefinito 25.

## Scenari di accettazione

Test: [`UC8_GestireMovimenti`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC8_GestireMovimenti.cs)

### UC8-S1

Test: `UC8_GestireMovimenti.S1_FiltroPerContoEPeriodo`

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Scenario: Filtro per conto e periodo
  Dato che "Mario" ha registrato:
    | data       | tipo   | importo  | conto        | categoria        |
    | 2026-11-02 | ricavo | 1.800,00 | Conto Intesa | Stipendio        |
    | 2026-11-05 | spesa  | 85,40    | Conto Intesa | Spesa alimentare |
    | 2026-11-06 | spesa  | 20,00    | Contanti     | Bar              |
    | 2026-11-10 | spesa  | 150,00   | Conto Intesa | Bollette         |
  Quando "Mario" filtra i movimenti di "Conto Intesa" dal 2026-11-03 al 2026-11-15
  Allora vede i movimenti del 2026-11-10 e del 2026-11-05, in quest'ordine
  E il totale delle spese è 235,40 € e quello dei ricavi 0,00 €
```

### UC8-S2

Test: `UC8_GestireMovimenti.S2_ModificaImporto`

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Scenario: Correggere l'importo di una spesa
  Dato che "Conto Intesa" ha saldo 2.414,60 € dopo la spesa di 85,40 €
  Quando "Mario" corregge l'importo della spesa in 58,40 €
  Allora il saldo di "Conto Intesa" è 2.441,60 €
```

### UC8-S3

Test: `UC8_GestireMovimenti.S3_CambioConto`

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Scenario: Spostare una spesa su un altro conto
  Dato che "Mario" ha registrato una spesa di 20,00 € da "Conto Intesa"
  E i saldi sono "Conto Intesa" 980,00 € e "Contanti" 100,00 €
  Quando "Mario" sposta la spesa su "Contanti"
  Allora i saldi sono "Conto Intesa" 1.000,00 € e "Contanti" 80,00 €
```

### UC8-S4

Test: `UC8_GestireMovimenti.S4_EliminaRataRiattivaIlDebito`

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Scenario: Eliminare l'ultima rata riattiva il debito
  Dato che "Mario" ha estinto il finanziamento "Auto" con una rata di 300,00 € da "Conto Intesa"
  Quando "Mario" elimina quella rata
  Allora il residuo di "Auto" è 300,00 € e il debito è attivo
  E il saldo di "Conto Intesa" è aumentato di 300,00 €
  E il movimento non compare più tra i movimenti
```

### UC8-S5

Test: `UC8_GestireMovimenti.S5_RataModificataEResiduo`

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Schema dello scenario: La rata modificata si confronta con il residuo senza di sé
  Dato che "Auto" aveva residuo 300,00 € ed è stata estinta con una rata di 300,00 €
  Quando "Mario" porta la rata a <importo>
  Allora <esito>

  Esempi:
    | importo  | esito                                                     |
    | 250,00 € | il residuo di "Auto" è 50,00 € e il debito è attivo       |
    | 300,01 € | la richiesta è rifiutata con "InvalidValue" sull'importo  |
```

### UC8-S6

Test: `UC8_GestireMovimenti.S6_ModificaNonValida`

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Schema dello scenario: Modifica non valida
  Dato che "Mario" ha aperto "Conto Intesa" il 2026-11-01 e vi ha registrato una spesa
  Quando "Mario" modifica la spesa con <campo> "<valore>"
  Allora la richiesta è rifiutata con "<codice>" su <campo>
  E il movimento è com'era

  Esempi:
    | campo     | valore                  | codice        |
    | importo   | 0                       | InvalidValue  |
    | data      | domani                  | InvalidValue  |
    | data      | 2026-10-31              | InvalidValue  |
    | categoria | una categoria di ricavo | InvalidValue  |
```

### UC8-S7

Test: `UC8_GestireMovimenti.S7_MovimentoDiUnAltroUtente`

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Schema dello scenario: Il movimento di un altro utente non esiste
  Dato che "Mario" ha registrato un movimento
  Quando "Anna" <azione> quel movimento
  Allora la risposta è "non trovato"
  E il movimento di "Mario" è com'era

  Esempi:
    | azione   |
    | legge    |
    | modifica |
    | elimina  |
```

### UC8-S8

Test: [`UC8_GestireMovimenti.S8_ConfermaEliminazione`](../../../src/MySync/FinHorizon.MySync.Frontend/src/features/delete-transaction/ui/UC8_GestireMovimenti.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Consultare, modificare ed eliminare i movimenti

Scenario: L'eliminazione chiede conferma
  Quando "Mario" sceglie "Elimina" su una spesa di 85,40 € del 2026-11-05 da "Conto Intesa"
  Allora vede la richiesta di conferma con data, importo e conto, e l'avviso che non si può annullare
  Quando annulla
  Allora il movimento è ancora nell'elenco
```
