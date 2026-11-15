# UC2 – Creare un conto

**Attore:** Utente
**Obiettivo:** registrare un posto dove l'utente tiene soldi o valore, con quanto c'è oggi.

## Flusso principale
1. L'utente sceglie il tipo di conto: Conto corrente, Banca virtuale, Contanti, Preziosi, Altro.
2. Inserisce nome, descrizione, tag e saldo iniziale.
3. Il sistema verifica i dati e il limite di conti, poi crea il conto con data di apertura oggi.
4. Il sistema mostra il conto con il suo saldo.

## Flussi alternativi
- **3a. Dati non validi:** il sistema non crea nulla e indica, campo per campo, cosa correggere.
- **3b. Nome già usato da un altro conto dell'utente:** il sistema non crea nulla e segnala `NotUnique` sul nome.
- **3c. Limite raggiunto:** l'utente ha già `MySync:MaxAccounts` conti; il sistema non crea nulla e segnala `LimitReached`. Il frontend nasconde "Nuovo conto" quando il limite è raggiunto.

## Regole
- R1. Tipo, nome e saldo iniziale sono obbligatori; descrizione e tag sono facoltativi.
- R2. Nome al massimo 50 caratteri, unico tra i conti dell'utente ignorando maiuscole e spazi iniziali e finali.
- R3. Descrizione al massimo 250 caratteri; tag al massimo 30 caratteri, uno solo per conto, testo libero. Il frontend suggerisce i tag già usati dall'utente.
- R4. Saldo iniziale in euro con al massimo 2 decimali; può essere 0 o negativo (scoperto, D7).
- R5. La data di apertura è la data di creazione e non si sceglie (D3): i movimenti del conto non possono essere precedenti.
- R6. Il limite `MySync:MaxAccounts` è un intero ≥ 1 letto all'avvio; se manca vale 2 (D2). Un valore non valido impedisce l'avvio del backend.
- R7. Il conto appartiene all'utente autenticato; il proprietario non arriva mai dalla richiesta.

## Scenari di accettazione

Test: [`UC2_CreareConto`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC2_CreareConto.cs)

### UC2-S1

Test: `UC2_CreareConto.S1_ContoCreato`

```gherkin
# language: it
Funzionalità: Creare un conto

Scenario: Conto corrente creato
  Quando "Mario" crea il conto corrente "Conto Intesa", descrizione "Stipendio e bollette", tag "Banca", saldo iniziale 2.500,00 €
  Allora il conto "Conto Intesa" è creato con data di apertura oggi
  E il suo saldo è 2.500,00 €
```

### UC2-S2

Test: `UC2_CreareConto.S2_SoloCampiObbligatori`

```gherkin
# language: it
Funzionalità: Creare un conto

Scenario: Conto senza descrizione e senza tag
  Quando "Mario" crea il conto "Contanti" di tipo Contanti con saldo iniziale 0
  Allora il conto è creato senza descrizione e senza tag
```

### UC2-S3

Test: `UC2_CreareConto.S3_DatiObbligatoriMancanti`

```gherkin
# language: it
Funzionalità: Creare un conto

Scenario: Dati obbligatori mancanti
  Quando "Mario" crea un conto senza compilare nessun campo
  Allora la richiesta è rifiutata con "Required" su tipo, nome e saldo iniziale
  E nessun conto è stato creato
```

### UC2-S4

Test: `UC2_CreareConto.S4_NomeGiaUsato`

```gherkin
# language: it
Funzionalità: Creare un conto

Scenario: Nome già usato da un altro conto dello stesso utente
  Dato che "Mario" ha il conto "Conto Intesa"
  Quando "Mario" crea il conto " conto intesa "
  Allora la richiesta è rifiutata con "NotUnique" sul nome
```

### UC2-S5

Test: `UC2_CreareConto.S5_StessoNomePerUtentiDiversi`

```gherkin
# language: it
Funzionalità: Creare un conto

Scenario: Utenti diversi possono usare lo stesso nome
  Dato che "Mario" ha il conto "Contanti"
  Quando "Anna" crea il conto "Contanti"
  Allora il conto di "Anna" è creato
```

### UC2-S6

Test: `UC2_CreareConto.S6_LimiteRaggiunto`

```gherkin
# language: it
Funzionalità: Creare un conto

Scenario: Limite di conti raggiunto
  Dato che "MySync:MaxAccounts" vale 2
  E "Mario" ha già 2 conti
  Quando "Mario" crea un terzo conto valido
  Allora la richiesta è rifiutata con "LimitReached"
  E "Mario" ha ancora 2 conti
```

### UC2-S7

Test: `UC2_CreareConto.S7_ValoriNonValidi`

```gherkin
# language: it
Funzionalità: Creare un conto

Schema dello scenario: Valori non validi
  Quando "Mario" crea un conto con <campo> "<valore>"
  Allora la richiesta è rifiutata con "<codice>" su <campo>

  Esempi:
    | campo           | valore                | codice            |
    | tipo            | Criptovalute          | InvalidValue      |
    | nome            | 51 caratteri          | MaxLengthExceeded |
    | descrizione     | 251 caratteri         | MaxLengthExceeded |
    | tag             | 31 caratteri          | MaxLengthExceeded |
    | saldo iniziale  | 10,005                | InvalidFormat     |
```

### UC2-S8

Test: `UC2_CreareConto.S8_SaldoInizialeNegativo`

```gherkin
# language: it
Funzionalità: Creare un conto

Scenario: Conto in scoperto
  Quando "Mario" crea il conto corrente "Conto BPM" con saldo iniziale -300,00 €
  Allora il conto è creato con saldo -300,00 €
```
