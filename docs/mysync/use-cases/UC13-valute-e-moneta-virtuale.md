# UC13 – Valute e moneta virtuale

**Attore:** Utente
**Sistemi coinvolti:** Frankfurter (catalogo delle valute e tassi di cambio)
**Obiettivo:** tenere conti in valute diverse, comprese le criptovalute (bitcoin, ethereum, …), e vedere patrimonio, debiti e andamento in un'unica valuta scelta dall'utente.

## Concetti
- **Valuta del conto:** ogni conto e ogni debito ha la sua valuta. Saldo iniziale, movimenti, saldo e residuo sono in quella valuta.
- **Moneta virtuale:** tipo di conto per le criptovalute. La sua valuta si sceglie dal catalogo delle criptovalute; per tutti gli altri tipi di conto, e per i debiti, si sceglie dal catalogo delle valute tradizionali.
- **Valuta di visualizzazione:** la valuta in cui l'utente vede patrimonio netto, debiti residui, variazioni e andamento. La sceglie alla configurazione (UC1) e la cambia nelle Impostazioni (UC11).
- **Conversione:** il valore in valuta di visualizzazione di un importo è l'importo moltiplicato per il tasso di cambio del giorno.

## Flusso principale
1. Alla configurazione (UC1, passo "Valuta") l'utente sceglie la valuta di visualizzazione; è proposto l'euro.
2. Creando o modificando un conto (UC2, UC10) l'utente sceglie il tipo e la valuta:
   - per "Moneta virtuale" sceglie da un elenco di criptovalute (Bitcoin, Ethereum, Solana, …);
   - per gli altri tipi da un elenco di valute tradizionali; è proposta la valuta di visualizzazione.
3. Creando un debito (UC3) sceglie la valuta, tra quelle tradizionali.
4. Ogni conto mostra il saldo nella sua valuta e, se è diversa, anche il controvalore nella valuta di visualizzazione.
5. Patrimonio netto, debiti residui, variazione e grafico dell'andamento (UC4, UC7) sono nella valuta di visualizzazione: ogni saldo e residuo viene convertito con il tasso di quel giorno.
6. Nelle Impostazioni (UC11) l'utente cambia la valuta di visualizzazione; dashboard e andamento si ricalcolano nella nuova valuta.

## Flussi alternativi
- **2a. Valuta che non è nel catalogo, o di categoria sbagliata** (una criptovaluta su un conto corrente, l'euro su un conto di moneta virtuale): `InvalidValue` su `currency`.
- **2b. Cambio di valuta di un conto con movimenti:** `InvalidValue` su `currency` (R6).
- **5a. Il servizio dei tassi non risponde e serve una conversione:** dashboard e andamento rispondono `503` con il problema "tassi di cambio non disponibili"; l'interfaccia mostra l'errore con il pulsante Riprova. Chi ha tutti i conti nella valuta di visualizzazione non ne risente.
- **Trasferimento tra conti di valute diverse (UC9):** l'utente scrive l'importo che esce e quello che entra (`counterAmount`). Se mancano o se le valute sono uguali vedi R7.
- **Rata di un debito (UC5):** il conto della spesa deve avere la valuta del debito, altrimenti `InvalidValue` su `accountId` (R8).

## Regole
- R1. Il catalogo e i tassi vengono da [Frankfurter](https://frankfurter.dev) (API v2): gratuito, senza chiave, con i tassi giornalieri delle banche centrali e di alcune fonti per le criptovalute, storico compreso (D10). L'indirizzo è il parametro `mysync-rates-url` dell'AppHost.
- R2. Precisione: 2 decimali per le valute tradizionali, 8 per le criptovalute. Gli importi convertiti si arrotondano a 2 decimali.
- R3. Il tasso di un giorno è l'ultimo pubblicato fino a quel giorno compreso: nei fine settimana e nei festivi vale quello dell'ultimo giorno lavorativo. Il saldo di oggi usa il tasso più recente.
- R4. La valuta di visualizzazione è una valuta tradizionale. Finché l'utente non la sceglie vale l'euro. Si salva in MySync (non in Keycloak) ed è uguale su ogni dispositivo.
- R5. "Moneta virtuale" appartiene al gruppo "Investimenti e depositi" (UC12).
- R6. La valuta di un conto si cambia solo finché il conto non ha movimenti; quella di un debito solo finché non ha rate.
- R7. Un trasferimento tra conti di valute diverse richiede `counterAmount`, l'importo che arriva sul conto di destinazione; tra conti della stessa valuta `counterAmount` non si indica (`InvalidValue`). Un trasferimento tra valute diverse cambia il patrimonio netto della differenza tra i due controvalori (cambio, commissioni).
- R8. Una rata esce da un conto nella valuta del debito.
- R9. Il catalogo resta in memoria 24 ore e i tassi 1 ora, per non chiamare Frankfurter a ogni richiesta.

## Scenari di accettazione

Test: [`UC13_Valute`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC13_Valute.cs). Nei test Frankfurter è sostituito da un catalogo e da tassi fissi: 1 EUR = 1,10 USD, 1 BTC = 50.000 EUR.

### UC13-S1

Test: `UC13_Valute.S1_ContoInMonetaVirtuale`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: Un conto in bitcoin
  Quando "Mario" crea il conto "Ledger" di tipo "Moneta virtuale" in "BTC" con saldo iniziale 0,12345678
  Allora il conto è creato in "BTC" con saldo 0,12345678
  E appartiene al gruppo "Investimenti e depositi"
```

### UC13-S2

Test: `UC13_Valute.S2_ValutaCoerenteConIlTipo`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Schema dello scenario: La valuta deve essere nel catalogo giusto
  Quando "Mario" crea un conto di tipo "<tipo>" in "<valuta>"
  Allora riceve l'errore "InvalidValue" sul campo "currency"

  Esempi:
    | tipo            | valuta |
    | Conto corrente  | BTC    |
    | Moneta virtuale | EUR    |
    | Conto corrente  | XYZ    |
```

### UC13-S3

Test: `UC13_Valute.S3_PatrimonioNellaValutaDiVisualizzazione`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: Il patrimonio somma i controvalori
  Dato che "Mario" vede gli importi in euro
  E ha "Credem" con 1.000,00 EUR, "Chase" con 1.100,00 USD e "Ledger" con 0,5 BTC
  Quando apre la dashboard
  Allora il patrimonio netto è 27.000,00 EUR
  E "Chase" mostra 1.100,00 USD e il controvalore 1.000,00 EUR
```

### UC13-S4

Test: `UC13_Valute.S4_CambioValutaDiVisualizzazione`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: Cambiare la valuta di visualizzazione
  Dato i conti di UC13-S3
  Quando "Mario" sceglie il dollaro nelle Impostazioni
  Allora il patrimonio netto è 29.700,00 USD
```

### UC13-S5

Test: `UC13_Valute.S5_AndamentoConIlTassoDelGiorno`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: Ogni giorno si converte con il suo tasso
  Dato che "Mario" ha "Ledger" con 1 BTC dal 2026-11-13
  E 1 BTC vale 40.000 EUR il 2026-11-13 e 50.000 EUR dal 2026-11-14
  Quando guarda l'andamento
  Allora il patrimonio è 40.000,00 il 2026-11-13 e 50.000,00 il 2026-11-15
```

### UC13-S6

Test: `UC13_Valute.S6_TrasferimentoTraValuteDiverse`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: Trasferimento da euro a dollari
  Dato i conti "Credem" in EUR e "Chase" in USD
  Quando "Mario" trasferisce 100,00 EUR da "Credem" e riceve 108,00 USD su "Chase"
  Allora "Credem" scende di 100,00 EUR e "Chase" sale di 108,00 USD
  Ma senza l'importo ricevuto riceve l'errore "Required" sul campo "counterAmount"
```

### UC13-S7

Test: `UC13_Valute.S7_RataNellaValutaDelDebito`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: La rata esce da un conto nella valuta del debito
  Dato il debito "Prestito" in EUR e il conto "Chase" in USD
  Quando "Mario" registra una rata del "Prestito" da "Chase"
  Allora riceve l'errore "InvalidValue" sul campo "accountId"
```

### UC13-S8

Test: `UC13_Valute.S8_ValutaBloccataDaiMovimenti`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: La valuta non cambia se il conto ha movimenti
  Dato il conto "Credem" in EUR con una spesa
  Quando "Mario" lo modifica in USD
  Allora riceve l'errore "InvalidValue" sul campo "currency"
```

### UC13-S9

Test: `UC13_Valute.S9_CatalogoDelleValute`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: Il catalogo distingue valute tradizionali e criptovalute
  Quando "Mario" chiede il catalogo delle valute
  Allora "EUR" e "USD" sono valute tradizionali e "BTC" è una criptovaluta
```

### UC13-S10

Test: `UC13_Valute.S10_TassiNonDisponibili`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: Senza tassi la dashboard lo dice
  Dato che il servizio dei tassi non risponde
  E "Mario" ha solo conti in euro
  Allora la dashboard si apre
  Quando crea un conto in dollari
  Allora la dashboard risponde "tassi di cambio non disponibili"
```

### UC13-S11

Test: `UC13_Valute.S11_ValutaDiVisualizzazione`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: La valuta di visualizzazione è una valuta tradizionale
  Quando "Mario" non l'ha ancora scelta
  Allora vale "EUR" e risulta non scelta
  Quando sceglie "BTC"
  Allora riceve l'errore "InvalidValue" sul campo "displayCurrency"
```

Altri test: `UC13_Valute.S10_TassiNonDisponibili_SoloEuroFunziona` e `UC13_Valute.DebitoInDollari` (backend); `CurrencyTests` di dominio e application; `FrankfurterCurrenciesTests` sul client (risposte reali di Frankfurter, cache, errori).

### UC13-S12

Test: `UC13_Valute.S12_DecimaliDellaValuta`

```gherkin
# language: it
Funzionalità: Valute e moneta virtuale

Scenario: I decimali dipendono dalla valuta del conto
  Quando "Mario" registra un ricavo di 0,00000001 sul conto "Ledger" in BTC
  Allora il movimento è registrato
  Quando registra una spesa di 1,005 sul conto "Credem" in EUR
  Allora riceve l'errore "InvalidFormat" sul campo "amount"
```

### Interfaccia

Test: [`UC13_Valute`](../../../src/MySync/FinHorizon.MySync.Frontend/src/app/routes/UC13_Valute.test.tsx) (frontend, Vitest; nel layer `app` perché attraversa form, pagine e widget)

- `S13_ValutaDelContoSecondoIlTipo`: con "Moneta virtuale" l'elenco mostra solo criptovalute, con gli altri tipi solo valute tradizionali.
- `S14_ValutaNellaConfigurazione` (UC1): il primo passo della configurazione chiede la valuta di visualizzazione.
- `S15_ValutaNelleImpostazioni` (UC11): la valuta scelta nelle Impostazioni si salva.
- `S16_TrasferimentoTraValute` (UC9): con conti di valute diverse compare l'importo ricevuto.
