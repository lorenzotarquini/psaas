# UC3 – Creare un debito

**Attore:** Utente
**Obiettivo:** registrare quanto l'utente deve ancora pagare e da quale conto lo paga, e vederlo accanto al patrimonio.

## Precondizioni
- L'utente ha almeno un conto (il debito ne richiede uno collegato).

## Flusso principale
1. L'utente sceglie il tipo di debito: Finanziamento, Mutuo, Fido, Altro.
2. Inserisce nome, descrizione, tag, il conto collegato (tra i propri) e il residuo da pagare oggi.
3. Il sistema verifica i dati e il limite di debiti, poi crea il debito.
4. Il sistema mostra il debito con il suo residuo; i debiti residui della dashboard aumentano del residuo, il patrimonio netto non cambia (D9).

## Flussi alternativi
- **3a. Dati non validi:** il sistema non crea nulla e indica, campo per campo, cosa correggere.
- **3b. Nome già usato da un altro debito dell'utente:** `NotUnique` sul nome.
- **3c. Il conto collegato non esiste o è di un altro utente:** `InvalidValue` sul conto.
- **3d. Limite raggiunto:** l'utente ha già `MySync:MaxDebts` debiti; il sistema non crea nulla e segnala `LimitReached`.
- **1a. L'utente non ha conti:** il frontend non offre "Nuovo debito" e invita a creare prima un conto.

## Regole
- R1. Tipo, nome, conto collegato e residuo sono obbligatori; descrizione e tag sono facoltativi.
- R2. Nome, descrizione e tag seguono le stesse regole del conto (UC2-R2, R3); il nome è unico tra i debiti dell'utente. Il tag è suggerito tra quelli già usati dall'utente su conti e debiti.
- R3. Residuo nella valuta del debito con al massimo 2 decimali, maggiore di 0.
- R7. Ogni debito ha una valuta tradizionale (UC13, D12): se non indicata vale quella di visualizzazione, e il frontend propone quella del conto collegato. Le rate escono da conti nella stessa valuta (UC13-R8).
- R4. Il conto collegato è quello da cui l'utente paga le rate: in UC5 è proposto come conto della rata, ma l'utente può cambiarlo (D6).
- R5. Il limite `MySync:MaxDebts` è un intero ≥ 0 letto all'avvio; se manca vale 1 (D2). Con 0 i debiti sono disattivati e il frontend non li mostra.
- R6. Contano nel limite anche i debiti estinti (residuo 0).

## Scenari di accettazione

Test: [`UC3_CreareDebito`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC3_CreareDebito.cs)

### UC3-S1

Test: `UC3_CreareDebito.S1_DebitoCreato`

```gherkin
# language: it
Funzionalità: Creare un debito

Scenario: Mutuo creato
  Dato che "Mario" ha il conto "Conto Intesa" con saldo 2.500,00 €
  Quando "Mario" crea il mutuo "Mutuo casa", descrizione "Tasso fisso 25 anni", tag "Casa", collegato a "Conto Intesa", residuo 120.000,00 €
  Allora il debito "Mutuo casa" è creato con residuo 120.000,00 €
  E il patrimonio netto di "Mario" resta 2.500,00 €, con debiti residui 120.000,00 €
```

### UC3-S2

Test: `UC3_CreareDebito.S2_DatiObbligatoriMancanti`

```gherkin
# language: it
Funzionalità: Creare un debito

Scenario: Dati obbligatori mancanti
  Quando "Mario" crea un debito senza compilare nessun campo
  Allora la richiesta è rifiutata con "Required" su tipo, nome, conto collegato e residuo
```

### UC3-S3

Test: `UC3_CreareDebito.S3_ResiduoNonPositivo`

```gherkin
# language: it
Funzionalità: Creare un debito

Schema dello scenario: Residuo non positivo
  Quando "Mario" crea un debito con residuo <residuo>
  Allora la richiesta è rifiutata con "InvalidValue" sul residuo

  Esempi:
    | residuo  |
    | 0        |
    | -100,00  |
```

### UC3-S4

Test: `UC3_CreareDebito.S4_ContoDiUnAltroUtente`

```gherkin
# language: it
Funzionalità: Creare un debito

Scenario: Conto collegato di un altro utente
  Dato che "Anna" ha il conto "Conto Anna"
  Quando "Mario" crea un debito collegato a "Conto Anna"
  Allora la richiesta è rifiutata con "InvalidValue" sul conto collegato
```

### UC3-S5

Test: `UC3_CreareDebito.S5_NomeGiaUsato`

```gherkin
# language: it
Funzionalità: Creare un debito

Scenario: Nome già usato da un altro debito
  Dato che "Mario" ha il debito "Mutuo casa"
  Quando "Mario" crea il debito "MUTUO CASA"
  Allora la richiesta è rifiutata con "NotUnique" sul nome
```

### UC3-S6

Test: `UC3_CreareDebito.S6_LimiteRaggiunto`

```gherkin
# language: it
Funzionalità: Creare un debito

Scenario: Limite di debiti raggiunto
  Dato che "MySync:MaxDebts" vale 1
  E "Mario" ha già il debito "Mutuo casa"
  Quando "Mario" crea il finanziamento "Auto"
  Allora la richiesta è rifiutata con "LimitReached"
```
