# UC5 – Registrare una spesa o un ricavo

**Attore:** Utente
**Obiettivo:** annotare soldi usciti o entrati da un conto, così che saldi, debiti e patrimonio restino veri.

## Precondizioni
- L'utente ha almeno un conto.

## Flusso principale
1. Dalla dashboard l'utente sceglie "Spesa" o "Ricavo".
2. Inserisce data (oggi come predefinita), importo, conto e categoria; facoltativamente una nota.
3. Se è una spesa, può indicare che è la rata di un suo debito: il sistema propone come conto quello collegato al debito (l'utente può cambiarlo).
4. Se la categoria non esiste, la crea dallo stesso campo (UC6).
5. Il sistema verifica i dati e registra il movimento.
6. Il sistema aggiorna la dashboard: saldo del conto, residuo dell'eventuale debito, patrimonio netto e ultimi movimenti.

## Flussi alternativi
- **5a. Dati non validi:** il sistema non registra nulla e indica, campo per campo, cosa correggere.
- **5b. Conto, categoria o debito inesistente o di un altro utente:** `InvalidValue` sul campo.
- **5c. Categoria del tipo sbagliato** (una categoria di ricavo su una spesa): `InvalidValue` sulla categoria.
- **5d. Debito su un ricavo:** `InvalidValue` sul debito (D5).
- **5e. Rata maggiore del residuo:** `InvalidValue` sull'importo, con il residuo nel messaggio.
- **5f. Debito estinto:** non è proposto nel frontend; se arriva lo stesso, `InvalidValue` sul debito.
- **5g. Data futura o precedente all'apertura del conto:** `InvalidValue` sulla data (D3, D4).

## Regole
- R1. Tipo (spesa o ricavo), data, importo, conto e categoria sono obbligatori; nota (max 250 caratteri) e debito sono facoltativi.
- R2. L'importo è sempre positivo, in euro con al massimo 2 decimali: è il tipo a dire se esce o entra.
- R3. Una spesa diminuisce il saldo del conto; un ricavo lo aumenta. Il saldo può diventare negativo (D7).
- R4. Una spesa collegata a un debito ne riduce il residuo dell'intero importo (niente quota interessi nell'MVP); quando il residuo arriva a 0 il debito è estinto.
- R5. La data va dalla data di apertura del conto a oggi, inclusi.
- R6. Il movimento registrato si consulta, si modifica e si elimina con UC8 (D1).
- R7. Una rata non può avere una data precedente alla creazione del debito: il residuo iniziale è la fotografia di quel giorno, come il saldo iniziale per il conto (D3). Errore `InvalidValue` sulla data.

## Scenari di accettazione

Test: [`UC5_RegistrareMovimento`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC5_RegistrareMovimento.cs)

### UC5-S1

Test: `UC5_RegistrareMovimento.S1_Spesa`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: Spesa registrata
  Dato che "Mario" ha il conto "Conto Intesa" con saldo 2.500,00 € e la categoria di spesa "Spesa alimentare"
  Quando "Mario" registra oggi una spesa di 85,40 € da "Conto Intesa" in "Spesa alimentare"
  Allora il saldo di "Conto Intesa" è 2.414,60 €
  E il movimento è il primo degli ultimi movimenti
```

### UC5-S2

Test: `UC5_RegistrareMovimento.S2_Ricavo`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: Ricavo registrato
  Dato che "Mario" ha il conto "Conto Intesa" con saldo 2.500,00 € e la categoria di ricavo "Stipendio"
  Quando "Mario" registra un ricavo di 1.800,00 € su "Conto Intesa" in "Stipendio"
  Allora il saldo di "Conto Intesa" è 4.300,00 €
```

### UC5-S3

Test: `UC5_RegistrareMovimento.S3_RataDiUnDebito`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: Rata del mutuo
  Dato che "Mario" ha il conto "Conto Intesa" con saldo 2.500,00 € e il mutuo "Mutuo casa" collegato, con residuo 120.000,00 €
  Quando "Mario" registra una spesa di 650,00 € da "Conto Intesa" in "Casa" come rata di "Mutuo casa"
  Allora il saldo di "Conto Intesa" è 1.850,00 €
  E il residuo di "Mutuo casa" è 119.350,00 €
  E il patrimonio netto è 1.850,00 €
```

### UC5-S4

Test: `UC5_RegistrareMovimento.S4_UltimaRataEstingueIlDebito`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: L'ultima rata estingue il debito
  Dato che "Mario" ha il finanziamento "Auto" con residuo 300,00 €
  Quando "Mario" registra una spesa di 300,00 € come rata di "Auto"
  Allora il residuo di "Auto" è 0 e il debito è estinto
```

### UC5-S5

Test: `UC5_RegistrareMovimento.S5_RataOltreIlResiduo`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: Rata maggiore del residuo
  Dato che "Mario" ha il finanziamento "Auto" con residuo 300,00 €
  Quando "Mario" registra una spesa di 300,01 € come rata di "Auto"
  Allora la richiesta è rifiutata con "InvalidValue" sull'importo
  E il residuo di "Auto" è ancora 300,00 €
```

### UC5-S6

Test: `UC5_RegistrareMovimento.S6_DatiObbligatoriMancanti`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: Dati obbligatori mancanti
  Quando "Mario" registra un movimento senza compilare nessun campo
  Allora la richiesta è rifiutata con "Required" su tipo, data, importo, conto e categoria
```

### UC5-S7

Test: `UC5_RegistrareMovimento.S7_ValoriNonValidi`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Schema dello scenario: Valori non validi
  Dato che "Mario" ha aperto "Conto Intesa" il 2026-11-01
  Quando "Mario" registra una spesa su "Conto Intesa" con <campo> "<valore>"
  Allora la richiesta è rifiutata con "<codice>" su <campo>

  Esempi:
    | campo     | valore                       | codice        |
    | importo   | 0                            | InvalidValue  |
    | importo   | -10,00                       | InvalidValue  |
    | importo   | 10,005                       | InvalidFormat |
    | data      | domani                       | InvalidValue  |
    | data      | 2026-10-31                   | InvalidValue  |
    | categoria | una categoria di ricavo      | InvalidValue  |
    | conto     | un conto di "Anna"           | InvalidValue  |
```

### UC5-S8

Test: `UC5_RegistrareMovimento.S8_DebitoSuUnRicavo`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: Un ricavo non può essere collegato a un debito
  Quando "Mario" registra un ricavo collegato a "Mutuo casa"
  Allora la richiesta è rifiutata con "InvalidValue" sul debito
```

### UC5-S9

Test: `UC5_RegistrareMovimento.S9_SaldoNegativo`

```gherkin
# language: it
Funzionalità: Registrare una spesa o un ricavo

Scenario: La spesa può mandare il conto in negativo
  Dato che "Mario" ha il conto "Contanti" con saldo 20,00 €
  Quando "Mario" registra una spesa di 35,00 € da "Contanti"
  Allora il saldo di "Contanti" è -15,00 €
```
