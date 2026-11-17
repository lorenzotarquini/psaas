# UC14 – Proiettare il patrimonio

**Attore:** Utente
**Obiettivo:** vedere dove sarà il proprio patrimonio tra un numero di mesi scelto, partendo dai dati veri di oggi, e capire subito cosa cambia se spende, guadagna o mette da parte in modo diverso.

## Precondizioni
- L'utente ha almeno un conto (altrimenti si apre UC1).

## Flusso principale
1. Dal menu l'utente apre **Proiezione**.
2. Il sistema mostra il **punto di partenza** di oggi, nella valuta di visualizzazione:
   - **liquidità**: saldi dei conti di tutti i giorni (UC12-R1);
   - **investimenti e depositi**: saldi di conti deposito, investimenti, PAC e moneta virtuale;
   - **patrimonio stabile**: saldi dei preziosi;
   - i **debiti** con il loro residuo.
3. Il sistema propone i **parametri**, già compilati con le medie degli ultimi mesi (R3):
   - entrate al mese;
   - spese fisse al mese e spese variabili al mese (UC15);
   - quota messa da parte al mese, cioè quanto passa dalla liquidità agli investimenti;
   - rendimento annuo degli investimenti (2%);
   - la rata mensile di ogni debito (UC3-R8);
   - gli **abbonamenti** (UC16), uno per uno, ognuno con il suo importo mensile e un interruttore per toglierlo dalla proiezione ("e se lo annullassi?").
4. L'utente sceglie l'**orizzonte**: da 1 a 360 mesi, con le scorciatoie 6 mesi, 1, 5, 10 e 30 anni (12 mesi all'inizio).
5. Il sistema mostra:
   - un **grafico** mese per mese con patrimonio netto, quota investita e debiti residui, tratteggiato per distinguerlo dall'andamento reale (UC7);
   - un **riepilogo**: patrimonio alla fine dell'orizzonte, variazione rispetto a oggi, mese in cui si estingue ogni debito.
6. L'utente cambia un parametro: grafico e riepilogo si aggiornano **subito**, senza attendere il server (R10).
7. L'utente può aggiungere fino a 10 **eventi una tantum** (mese, importo in entrata o in uscita, descrizione), per esempio la tredicesima o l'acquisto di un'auto.
8. L'utente può indicare un **obiettivo** (una cifra di patrimonio): il grafico mostra una linea orizzontale e il riepilogo dice in quale mese la raggiunge.
9. L'utente può accendere **"In potere d'acquisto di oggi"** con un'inflazione annua (2%): i valori si mostrano scontati dell'inflazione.
10. Con **"Salva"** il sistema conserva orizzonte, parametri, eventi, obiettivo e inflazione: riaprendo la pagina l'utente li ritrova.

## Flussi alternativi
- **3a. Meno di 3 mesi completi di dati:** i parametri sono le medie dei mesi disponibili (0 se non ce n'è nessuno), con l'avviso che la stima si basa su pochi dati e conviene controllarli.
- **3b. Un debito senza rata mensile e senza rate negli ultimi 3 mesi:** la rata proposta è 0 e il debito è segnalato come "senza rata": nella proiezione il suo residuo non scende.
- **5a. La liquidità scende sotto zero:** il riepilogo avverte "La liquidità va sotto zero a <mese>". La proiezione continua.
- **8a. Obiettivo già raggiunto oggi:** il riepilogo dice "Obiettivo già raggiunto".
- **8b. Obiettivo non raggiunto entro l'orizzonte:** il riepilogo lo dice e suggerisce di allungare l'orizzonte.
- **10a. Dati non validi:** il sistema non salva e indica, campo per campo, cosa correggere.
- **10b. "Riparti dai miei dati":** il sistema cancella i parametri salvati e ripropone quelli calcolati dai movimenti.
- **\*a. Servono tassi di cambio e il servizio non risponde:** come UC13-5a, il sistema segnala che il servizio non è disponibile.

## Regole
- R1. **Punto di partenza:** saldi e residui di oggi (UC4-R1, R2), convertiti nella valuta di visualizzazione con il tasso di oggi (UC13). La proiezione considera sempre **tutti** i conti e i debiti, senza gli switch di UC12 (D15).
- R2. **Mesi:** il mese 0 è oggi. Il mese *m* è il mese di calendario *m* mesi dopo quello corrente e ha come etichetta il suo nome (oggi è 2026-11-17, quindi il mese 1 è dicembre 2026).
- R3. **Parametri proposti:** si calcolano sugli ultimi 6 mesi di calendario completi prima di quello corrente, limitati ai mesi completi successivi all'apertura del primo conto (`monthsOfData`, da 0 a 6). Ogni importo è convertito con il tasso del suo giorno (UC13-R3). I valori sono medie mensili:
  - **entrate**: tutti i ricavi;
  - **spese fisse**: le spese non collegate a un debito né a un abbonamento, con categoria fissa (UC15);
  - **spese variabili**: le spese non collegate a un debito né a un abbonamento, con categoria variabile;
  - **abbonamento**: il suo importo mensile di oggi (UC16), convertito con il tasso di oggi, incluso. Non è una media: un abbonamento nuovo conta per intero e uno annullato esce dai mesi successivi alla data di fine (R13). Si propongono gli abbonamenti senza data di fine o con data di fine da oggi in poi;
  - **quota messa da parte**: i trasferimenti da conti di tutti i giorni a conti di investimenti e depositi, meno quelli in senso opposto; mai sotto 0;
  - **rata di un debito**: la sua rata mensile (UC3-R8) se c'è, altrimenti la media delle rate pagate negli ultimi 3 mesi completi;
  - **rendimento** 2% e **inflazione** 2%, con l'inflazione spenta. Sono solo i valori iniziali: rendimento e inflazione sono campi che l'utente cambia a piacimento, entro gli intervalli di R9, come ogni altro parametro.

  Il mese corrente, incompleto, non conta.
- R4. **Calcolo, per ogni mese *m* da 1 all'orizzonte:**
  1. investimenti: I(m) = I(m−1) × (1 + rendimento / 12) + quota messa da parte;
  2. per ogni debito: pagato = min(rata, residuo), residuo −= pagato;
  3. liquidità: L(m) = L(m−1) + entrate − spese fisse − spese variabili − Σ pagato − Σ abbonamenti del mese (R13) − quota messa da parte + eventi del mese;
  4. patrimonio stabile: costante (i preziosi non si rivalutano, come nel resto dell'MVP);
  5. patrimonio netto: P(m) = L(m) + I(m) + patrimonio stabile; debiti residui: Σ residui. Come in D9, i debiti non si sottraggono.
- R5. Un debito estinto non si paga più: la sua rata resta nella liquidità, e la crescita mensile aumenta di conseguenza.
- R6. I calcoli non si arrotondano durante la proiezione: i valori si arrotondano al centesimo solo per mostrarli.
- R7. **Obiettivo:** è il primo mese *m* ≥ 1 in cui P(m) ≥ obiettivo; se P(0) ≥ obiettivo è già raggiunto. Con l'inflazione accesa si confronta il valore scontato.
- R8. **Inflazione:** con l'interruttore acceso ogni valore del mese *m* si mostra diviso per (1 + inflazione)^(m/12). Il calcolo di R4 non cambia.
- R9. **Validazione** (al salvataggio, e nel frontend mentre si scrive):
  - orizzonte: intero da 1 a 360;
  - entrate, spese, quota messa da parte, rate e importi degli abbonamenti: ≥ 0, con 2 decimali;
  - ogni debito e ogni abbonamento compare al massimo una volta, e deve essere dell'utente;
  - rendimento: da −10% a 20%; inflazione: da 0% a 20%;
  - eventi: al massimo 10, ognuno con mese da 1 all'orizzonte, importo diverso da 0 e descrizione obbligatoria di al massimo 40 caratteri;
  - obiettivo: facoltativo, > 0.
- R10. Il calcolo di R4 gira nel browser: il backend dà solo punto di partenza, parametri proposti e parametri salvati. Cambiare un parametro non chiama il server.
- R11. Ogni utente ha **una** proiezione salvata. Le rate salvate sono legate all'id del debito: un debito creato dopo il salvataggio usa la rata proposta, quella di un debito che non c'è più si ignora. Lo stesso vale per gli abbonamenti: importo e interruttore salvati sono legati all'id, un abbonamento nuovo usa i valori proposti, uno eliminato si ignora. Primo addebito e data di fine vengono sempre dall'abbonamento di oggi, mai dal salvataggio: annullare un abbonamento cambia subito anche la proiezione salvata.
- R12. Grafico accessibile come in UC7-R5 (tabella dei dati alternativa, tema chiaro e scuro, telefono); con gli importi nascosti di UC12 la proiezione non nasconde nulla, perché l'occhio vale solo in dashboard.
- R13. **Abbonamenti nel mese *m*:** un abbonamento incluso costa il suo importo nel mese *m* se il suo addebito in quel mese di calendario (UC16-R6) non precede il primo addebito e non supera la data di fine. Così un abbonamento che parte tra due mesi conta da lì, e uno annullato con data di fine il 10 gennaio paga ancora l'addebito del 5 gennaio e poi smette. Un abbonamento spento con l'interruttore non costa nulla in nessun mese.

## Fase 2 (fuori ambito)
- Più scenari con nome, confrontati sullo stesso grafico.
- Confronto tra una proiezione passata e quello che è successo davvero.
- Rendimento diverso per ogni conto; rivalutazione dei preziosi; tasso e piano di ammortamento dei debiti.

## Scenari di accettazione

Test backend: [`UC14_ProiettareIlPatrimonio`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC14_ProiettareIlPatrimonio.cs). Test del calcolo: [`UC14_CalcoloProiezione`](../../../src/MySync/FinHorizon.MySync.Frontend/src/entities/projection/model/UC14_CalcoloProiezione.test.ts) (frontend, Vitest). Test della pagina: [`UC14_ProiettareIlPatrimonio`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/projection/ui/UC14_ProiettareIlPatrimonio.test.tsx) (frontend, Vitest).

Negli scenari oggi è il 2026-11-17, la valuta di visualizzazione è l'euro e i parametri non citati valgono 0 (rendimento compreso).

### UC14-S1

Test: `UC14_ProiettareIlPatrimonio.S1_PuntoDiPartenzaEParametriProposti`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Punto di partenza e parametri dai movimenti
  Dato che "Mario" ha il conto corrente "Conto Intesa" aperto il 2026-01-10 con saldo 3.000,00 €
  E ha il conto deposito "Deposito" con saldo 5.000,00 € e i preziosi "Oro" con saldo 2.000,00 €
  E ha il finanziamento "Auto" con residuo 4.000,00 € senza rata mensile
  E le categorie di spesa "Affitto" (fissa), "Spesa alimentare" e "Svago" (variabili)
  E in ogni mese da maggio a ottobre 2026 ha registrato:
    | movimento                          | importo  |
    | ricavo "Stipendio"                 | 2.000,00 |
    | spesa "Affitto"                    | 700,00   |
    | spesa "Spesa alimentare"           | 400,00   |
    | spesa "Svago"                      | 200,00   |
    | rata di "Auto"                     | 250,00   |
    | trasferimento a "Deposito"         | 300,00   |
  E il 2026-11-05 ha registrato la spesa "Svago" di 1.000,00 €
  Quando "Mario" apre la proiezione
  Allora il punto di partenza è liquidità 3.000,00 €, investimenti 5.000,00 €, patrimonio stabile 2.000,00 €
  E il debito "Auto" ha residuo 4.000,00 €
  E i parametri proposti sono entrate 2.000,00 €, spese fisse 700,00 €, spese variabili 600,00 €, quota messa da parte 300,00 €
  E la rata proposta di "Auto" è 250,00 €
  E i mesi di dati sono 6
```

### UC14-S2

Test: `UC14_CalcoloProiezione.S2_CrescitaMensile`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Entrate e spese
  Dato una liquidità di 1.000,00 €
  E entrate 2.000,00 €, spese fisse 700,00 €, spese variabili 600,00 € al mese
  Quando la proiezione è di 3 mesi
  Allora i punti sono:
    | mese          | patrimonio netto |
    | oggi          | 1.000,00         |
    | dicembre 2026 | 1.700,00         |
    | gennaio 2027  | 2.400,00         |
    | febbraio 2027 | 3.100,00         |
  E il riepilogo dice +2.100,00 € rispetto a oggi
```

### UC14-S3

Test: `UC14_CalcoloProiezione.S3_RendimentoDegliInvestimenti`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Quota messa da parte e rendimento
  Dato una liquidità di 1.000,00 € e investimenti per 10.000,00 €
  E una quota messa da parte di 100,00 € al mese con rendimento annuo del 6%
  Quando la proiezione è di 2 mesi
  Allora i punti sono:
    | mese          | liquidità | investimenti | patrimonio netto |
    | dicembre 2026 | 900,00    | 10.150,00    | 11.050,00        |
    | gennaio 2027  | 800,00    | 10.300,75    | 11.100,75        |
```

### UC14-S4

Test: `UC14_CalcoloProiezione.S4_IlDebitoEstintoLiberaLaRata`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Un debito estinto libera la rata
  Dato entrate di 1.000,00 € al mese
  E il finanziamento "Auto" con residuo 500,00 € e rata 200,00 €
  Quando la proiezione è di 4 mesi
  Allora i punti sono:
    | mese          | patrimonio netto | debiti residui |
    | dicembre 2026 | 800,00           | 300,00         |
    | gennaio 2027  | 1.600,00         | 100,00         |
    | febbraio 2027 | 2.500,00         | 0,00           |
    | marzo 2027    | 3.500,00         | 0,00           |
  E il riepilogo dice che "Auto" si estingue a febbraio 2027
```

### UC14-S5

Test: `UC14_CalcoloProiezione.S5_EventiELiquiditaNegativa`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Eventi una tantum
  Dato la proiezione di UC14-S2
  E l'evento "Tredicesima" di +1.500,00 € a gennaio 2027
  E l'evento "Auto nuova" di −5.000,00 € a febbraio 2027
  Quando la proiezione è di 3 mesi
  Allora il patrimonio netto è 1.700,00 € a dicembre, 3.900,00 € a gennaio e −400,00 € a febbraio
  E il riepilogo avverte che la liquidità va sotto zero a febbraio 2027
```

### UC14-S6

Test: `UC14_CalcoloProiezione.S6_Obiettivo`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Schema dello scenario: Quando raggiungo l'obiettivo
  Dato la proiezione di UC14-S2 su 12 mesi
  Quando l'obiettivo è <obiettivo>
  Allora il riepilogo dice "<risposta>"

  Esempi:
    | obiettivo  | risposta                                    |
    | 5.000,00   | Obiettivo raggiunto a maggio 2027           |
    | 20.000,00  | Obiettivo non raggiunto entro 12 mesi       |
    | 500,00     | Obiettivo già raggiunto                     |
```

### UC14-S7

Test: `UC14_CalcoloProiezione.S7_PotereDAcquisto`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Valori in potere d'acquisto di oggi
  Dato una liquidità di 10.000,00 € e nessuna entrata o spesa
  E l'inflazione annua al 2%
  Quando la proiezione è di 12 mesi con "In potere d'acquisto di oggi" acceso
  Allora il patrimonio netto di novembre 2027 è 9.803,92 €
  Quando l'utente spegne "In potere d'acquisto di oggi"
  Allora il patrimonio netto di novembre 2027 è 10.000,00 €
```

### UC14-S8

Test: `UC14_ProiettareIlPatrimonio.S8_IlGraficoCambiaSubito` (pagina)

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Cambiare un parametro aggiorna la proiezione
  Dato che "Mario" ha aperto la proiezione di UC14-S2 su 3 mesi
  Quando cambia le spese variabili da 600,00 € a 400,00 €
  Allora il patrimonio di febbraio 2027 diventa 3.700,00 €
  E il frontend non ha fatto nessuna nuova chiamata al server
```

### UC14-S9

Test: `UC14_ProiettareIlPatrimonio.S9_SalvareERipartire`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Parametri salvati e "Riparti dai miei dati"
  Dato i parametri proposti di UC14-S1
  Quando "Mario" salva orizzonte 60 mesi, spese variabili 450,00 €, l'evento "Tredicesima" di +1.500,00 € al mese 2 e l'obiettivo 30.000,00 €
  Allora riaprendo la proiezione ritrova esattamente quei valori
  Quando "Mario" sceglie "Riparti dai miei dati"
  Allora la proiezione salvata è cancellata
  E i parametri sono di nuovo quelli proposti, con orizzonte 12 mesi e senza eventi né obiettivo
```

### UC14-S10

Test: `UC14_ProiettareIlPatrimonio.S10_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Schema dello scenario: Dati non validi
  Quando "Mario" salva una proiezione con <dato>
  Allora la richiesta è rifiutata con "<codice>" su "<campo>"
  E la proiezione salvata non è cambiata

  Esempi:
    | dato                               | codice        | campo              |
    | orizzonte 0                        | InvalidValue  | horizonMonths      |
    | orizzonte 361                      | InvalidValue  | horizonMonths      |
    | spese fisse −1,00                  | InvalidValue  | fixedExpenses      |
    | entrate 10,001                     | InvalidFormat | income             |
    | rendimento 25%                     | InvalidValue  | annualReturn       |
    | inflazione −1%                     | InvalidValue  | inflation          |
    | 11 eventi                          | InvalidValue  | events             |
    | un evento al mese 13 su 12 mesi    | InvalidValue  | events[0].month    |
    | un evento di importo 0             | InvalidValue  | events[0].amount   |
    | un evento senza descrizione        | Required      | events[0].note     |
    | obiettivo 0                        | InvalidValue  | goal               |
    | un abbonamento di importo −1,00    | InvalidValue  | subscriptions[0].amount |
    | un abbonamento di un altro utente  | InvalidValue  | subscriptions[0].subscriptionId |
```

### UC14-S11

Test: `UC14_ProiettareIlPatrimonio.S11_PochiDati`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Schema dello scenario: Pochi mesi di dati
  Dato che "Mario" ha aperto il primo conto il <apertura>
  E ogni mese completo ha registrato un ricavo "Stipendio" di 2.000,00 €
  Quando "Mario" apre la proiezione
  Allora i mesi di dati sono <mesi> e le entrate proposte <entrate>

  Esempi:
    | apertura   | mesi | entrate  |
    | 2026-09-15 | 1    | 2.000,00 |
    | 2026-11-02 | 0    | 0,00     |
```

Il frontend mostra l'avviso di 3a con meno di 3 mesi di dati.

### UC14-S12

Test: `UC14_ProiettareIlPatrimonio.S12_RateSalvateEDebitiNuovi`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Un debito creato dopo il salvataggio
  Dato che "Mario" ha salvato la proiezione con rata 300,00 € per "Auto"
  Quando crea il finanziamento "Moto" con rata mensile 150,00 €
  E riapre la proiezione
  Allora la rata di "Auto" è 300,00 € e quella di "Moto" è 150,00 €
```

### UC14-S13

Test: `UC14_ProiettareIlPatrimonio.S13_AbbonamentiProposti`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Gli abbonamenti sono proposti uno per uno
  Dato i movimenti di UC14-S1
  E la categoria di spesa fissa "Abbonamenti"
  E l'abbonamento "Netflix" da "Conto Intesa", 20,00 € al mese, primo addebito il 2026-05-05
  E in ogni mese da maggio a ottobre 2026 la spesa di 20,00 € in "Abbonamenti" collegata a "Netflix"
  E l'abbonamento "Palestra" di 45,00 € al mese, annullato con data di fine il 2026-10-31
  E l'abbonamento "Cloud" di 3,00 € al mese, creato oggi con primo addebito il 2026-12-01 e ancora senza spese
  Quando "Mario" apre la proiezione
  Allora i parametri proposti sono spese fisse 700,00 € e spese variabili 600,00 €
  E gli abbonamenti proposti sono "Netflix" 20,00 € e "Cloud" 3,00 €, entrambi inclusi
  E "Palestra" non è proposto
```

### UC14-S14

Test: `UC14_CalcoloProiezione.S14_AbbonamentiNelTempo`

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: Abbonamenti che iniziano e finiscono
  Dato una liquidità di 1.000,00 €
  E l'abbonamento "Netflix" di 20,00 € con addebito il giorno 5, data di fine il 2027-01-10
  E l'abbonamento "Cloud" di 3,00 € con primo addebito il 2027-01-20
  Quando la proiezione è di 3 mesi
  Allora i punti sono:
    | mese          | patrimonio netto |
    | dicembre 2026 | 980,00           |
    | gennaio 2027  | 957,00           |
    | febbraio 2027 | 954,00           |
```

### UC14-S15

Test: `UC14_ProiettareIlPatrimonio.S15_EStaccassiUnAbbonamento` (pagina)

```gherkin
# language: it
Funzionalità: Proiettare il patrimonio

Scenario: "E se lo annullassi?"
  Dato che "Mario" ha aperto la proiezione di UC14-S2 su 12 mesi con l'abbonamento "Palestra" di 45,00 € incluso
  E il patrimonio di novembre 2027 è 8.860,00 €
  Quando spegne l'interruttore di "Palestra"
  Allora il patrimonio di novembre 2027 diventa 9.400,00 €
  E il frontend non ha fatto nessuna nuova chiamata al server
  E "Palestra" non è stato annullato
```

Oltre agli scenari, la suite della pagina copre il salvataggio e "Riparti dai miei dati" dall'interfaccia (S9) e un campo non valido che blocca il salvataggio lasciando il grafico sull'ultimo valore valido; la suite del calcolo copre il debito senza rata (3b).
