# UC16 – Gestire gli abbonamenti

**Attore:** Utente
**Obiettivo:** tenere in un solo posto le spese fisse che si ripetono ogni mese finché non le annulla (streaming, palestra, telefono, cloud), ricordarsi di registrarle e vedere subito nella proiezione cosa cambia annullandone una.

## Precondizioni
- L'utente ha almeno un conto in una valuta tradizionale.

## Flusso principale
1. Dal menu l'utente apre **Abbonamenti**.
2. Il sistema mostra gli abbonamenti **attivi**, ognuno con nome, importo mensile, conto, categoria e prossimo addebito, il **totale mensile** nella valuta di visualizzazione e, in fondo, gli **annullati** con la loro data di fine.
3. Con **"Nuovo abbonamento"** l'utente inserisce nome, descrizione facoltativa, conto, categoria di spesa, importo mensile e data del primo addebito (oggi come predefinita).
4. Il sistema verifica i dati e il limite di abbonamenti, poi crea l'abbonamento. Non registra nessun movimento: saldi e patrimonio non cambiano.
5. Quando un addebito scade, l'abbonamento è **da registrare** (R7): la dashboard (UC4) e questa pagina lo mostrano con il pulsante **"Registra"**.
6. "Registra" apre la spesa di UC5 già compilata (data dell'addebito, importo, conto, categoria e collegamento all'abbonamento). L'utente può cambiare qualsiasi campo e salva.
7. Il sistema registra la spesa collegata all'abbonamento, che non è più da registrare fino all'addebito successivo.

## Flussi alternativi
- **3a. L'utente non ha conti in valuta tradizionale:** il frontend non offre "Nuovo abbonamento" e spiega perché.
- **4a. Dati non validi:** il sistema non crea nulla e indica, campo per campo, cosa correggere.
- **4b. Nome già usato da un altro abbonamento dell'utente:** `NotUnique` sul nome.
- **4c. Conto o categoria inesistente o di un altro utente, categoria di ricavo, conto in criptovaluta:** `InvalidValue` sul campo.
- **4d. Limite raggiunto:** l'utente ha già `MySync:MaxSubscriptions` abbonamenti non annullati (R4). Il pulsante "Nuovo abbonamento" è disabilitato con il messaggio del limite, e il backend risponde `LimitReached`.
- **5a. Questo mese l'addebito non c'è stato** (mese gratuito, addebito saltato): con **"Salta"** l'utente toglie l'addebito da quelli da registrare (R9).
- **6a. La data dell'addebito precede l'apertura del conto scelto:** come UC5-5g, `InvalidValue` sulla data. L'utente sceglie un altro conto o salta l'addebito.
- **M. Modificare:** con la matita l'utente cambia tutti i campi del passo 3 (R12). Le spese già registrate non cambiano.
- **A. Annullare:** con **"Annulla"** l'utente indica la data di fine (oggi come predefinita, R13). L'abbonamento passa tra gli annullati e non ha più addebiti dopo quella data.
- **A1. Riattivare:** un abbonamento annullato si riattiva togliendo la data di fine; vale il limite di 4d.
- **E. Eliminare:** un abbonamento senza spese collegate si elimina; con spese collegate il sistema propone di annullarlo (R15).
- **\*a. Abbonamento inesistente o di un altro utente:** `404`.

## Regole

### L'abbonamento
- R1. Nome, conto, categoria, importo mensile e data del primo addebito sono obbligatori; la descrizione è facoltativa. Nome e descrizione seguono le regole del conto (UC2-R2, R3); il nome è unico tra gli abbonamenti dell'utente, annullati compresi.
- R2. Il conto è un conto dell'utente in una **valuta tradizionale**: la valuta dell'abbonamento è quella del conto (D12). L'importo mensile è maggiore di 0, con al massimo 2 decimali.
- R3. La categoria è una categoria di spesa dell'utente. Il frontend la cerca e la crea al volo come in UC6. Il fatto che la categoria sia fissa o variabile (UC15) non conta per l'abbonamento (R17).
- R4. Il limite `MySync:MaxSubscriptions` è un intero da 0 a 1000 letto all'avvio; se manca vale 10. Contano solo gli abbonamenti senza data di fine, così annullarne uno libera subito il posto, anche se resta attivo fino alla data di fine. Con 0 gli abbonamenti sono disattivati e il frontend non li mostra.
- R5. L'abbonamento ricorda il giorno in cui è stato creato (la **data di creazione**), come il debito (UC3).

### Gli addebiti
- R6. Gli **addebiti** di un abbonamento cadono ogni mese nel giorno del primo addebito, o nell'ultimo giorno del mese se quel giorno non esiste (primo addebito il 31 gennaio → 28 febbraio, 31 marzo, 30 aprile). Il primo è il primo addebito, l'ultimo è il più recente che non supera la data di fine.
- R7. Un abbonamento è **da registrare** quando il suo addebito più recente:
  - ha una data fino a oggi compreso;
  - non precede la data di creazione (gli addebiti precedenti l'utente li ha già annotati, o li collega con UC8);
  - non ha, nello stesso mese di calendario, una spesa collegata all'abbonamento;
  - non è stato saltato (R9).
  Gli addebiti più vecchi non si mostrano: se l'utente ne ha persi alcuni, li registra da UC5.
- R8. Registrare un addebito è una normale spesa di UC5 collegata all'abbonamento: MySync non registra mai movimenti da solo (D19). Il saldo cambia solo quando l'utente salva.
- R9. "Salta" vale solo per l'addebito da registrare in quel momento e lo toglie per sempre; l'addebito del mese dopo sarà di nuovo da registrare.

### Le spese collegate
- R10. Solo una **spesa** può essere collegata a un abbonamento. Una spesa è la rata di un debito oppure il pagamento di un abbonamento, non tutte e due: `InvalidValue` su `subscriptionId`.
- R11. Una spesa collegata:
  - esce da un conto nella valuta dell'abbonamento, altrimenti `InvalidValue` su `accountId` (come UC13-R8 per i debiti);
  - ha una data compresa tra il primo addebito e la data di fine, se c'è, altrimenti `InvalidValue` sulla data;
  - ha un importo e una categoria qualsiasi: un aumento di prezzo non obbliga a modificare l'abbonamento.
  Il collegamento si mette, si cambia e si toglie anche modificando un movimento (UC8). Eliminare o scollegare la spesa di un addebito lo rende di nuovo da registrare.

### Modificare, annullare, eliminare
- R12. Si modificano nome, descrizione, conto, categoria, importo e primo addebito; la data di creazione no. Il conto si cambia solo con uno nella stessa valuta finché ci sono spese collegate (`InvalidValue` su `accountId`, come D14). Il primo addebito non può superare la data della prima spesa collegata (`InvalidValue` su `firstChargeOn`).
- R13. La data di fine si sceglie liberamente, anche nel futuro (per esempio "pagato fino al 30 novembre"), ma non può precedere il primo addebito né l'ultima spesa collegata: `InvalidValue` su `endsOn`. Un abbonamento con data di fine resta **attivo** fino a quel giorno compreso, per il totale mensile e per la proiezione (UC14-R13), ma non conta più nel limite (R4). Annullarlo di nuovo cambia la data di fine.
- R14. Riattivare toglie la data di fine. Gli addebiti ripartono dal giorno del primo addebito: quelli caduti mentre l'abbonamento era annullato non sono da registrare, perché riattivando il più recente di loro viene saltato (R9) e R7 guarda solo il più recente.
- R15. Si elimina solo un abbonamento senza spese collegate (`InvalidValue` su `payments`). Le spese collegate sono storia: per smettere di pagare si annulla.

### Cosa mostra e cosa cambia
- R16. Il **totale mensile** è la somma degli importi degli abbonamenti attivi, convertiti nella valuta di visualizzazione con il tasso di oggi (UC13). Con gli importi nascosti di UC12 la dashboard mostra `••••`.
- R17. Nella proiezione (UC14) gli abbonamenti sono una voce a sé, uno per uno. Le spese collegate a un abbonamento non contano né tra le fisse né tra le variabili (UC15-R5), così niente è contato due volte e un abbonamento annullato esce subito dalle previsioni.
- R18. Saldi, patrimonio e andamento cambiano solo con le spese registrate: creare, modificare, annullare o eliminare un abbonamento non li tocca.

## Fase 2 (fuori ambito)
- Registrazione automatica degli addebiti, scelta per ogni abbonamento.
- Abbonamenti annuali, trimestrali o settimanali.
- Notifiche prima del rinnovo e promemoria dei periodi di prova.
- Storico dei prezzi di un abbonamento.

## Scenari di accettazione

Test: [`UC16_GestireAbbonamenti`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC16_GestireAbbonamenti.cs). Regole del dominio (R6, R7, R9, R10, R11, R13, R14): [`SubscriptionTests`](../../../src/MySync/Tests/FinHorizon.MySync.Domain.Tests/Subscriptions/SubscriptionTests.cs). Test della pagina: [`UC16_GestireAbbonamenti`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/subscriptions/ui/UC16_GestireAbbonamenti.test.tsx) (frontend, Vitest).

Negli scenari oggi è il 2026-11-17, la valuta di visualizzazione è l'euro e "Mario" ha il conto corrente "Conto Intesa" in euro, aperto il 2026-01-10 con saldo 2.000,00 €, e la categoria di spesa "Abbonamenti".

### UC16-S1

Test: `UC16_GestireAbbonamenti.S1_AbbonamentoCreato`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Abbonamento creato
  Quando "Mario" crea l'abbonamento "Netflix" da "Conto Intesa" in "Abbonamenti", 17,99 € al mese, primo addebito il 2026-12-05
  Allora "Netflix" è attivo con prossimo addebito il 2026-12-05
  E il totale mensile è 17,99 €
  E il saldo di "Conto Intesa" è ancora 2.000,00 €
  E "Netflix" non è da registrare
```

### UC16-S2

Test: `UC16_GestireAbbonamenti.S2_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Schema dello scenario: Dati non validi
  Dato che "Mario" ha l'abbonamento "Netflix"
  E "Anna" ha il conto "Conto Anna"
  E "Mario" ha la categoria di ricavo "Stipendio" e il conto in moneta virtuale "Wallet BTC"
  Quando "Mario" crea un abbonamento con <dato>
  Allora la richiesta è rifiutata con "<codice>" su "<campo>"

  Esempi:
    | dato                       | codice        | campo          |
    | nome vuoto                 | Required      | name           |
    | nome "NETFLIX"             | NotUnique     | name           |
    | importo 0                  | InvalidValue  | amount         |
    | importo 10,001             | InvalidFormat | amount         |
    | conto "Conto Anna"         | InvalidValue  | accountId      |
    | conto "Wallet BTC"         | InvalidValue  | accountId      |
    | categoria "Stipendio"      | InvalidValue  | categoryId     |
    | senza primo addebito       | Required      | firstChargeOn  |
```

### UC16-S3

Test: `UC16_GestireAbbonamenti.S3_Limite`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Il limite conta solo gli abbonamenti non annullati
  Dato che "MySync:MaxSubscriptions" vale 1
  E "Mario" ha l'abbonamento "Netflix" senza data di fine
  Quando "Mario" crea l'abbonamento "Spotify"
  Allora la richiesta è rifiutata con "LimitReached"
  Quando "Mario" annulla "Netflix" con data di fine il 2026-11-30
  E crea l'abbonamento "Spotify"
  Allora "Spotify" è creato
  Quando "Mario" riattiva "Netflix"
  Allora la richiesta è rifiutata con "LimitReached"
```

### UC16-S4

Test: `UC16_GestireAbbonamenti.S4_RegistrareLAddebito`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Un addebito scaduto si registra
  Dato che il 2026-10-01 "Mario" ha creato l'abbonamento "Netflix" da "Conto Intesa", 17,99 € al mese, primo addebito il 2026-10-05
  E ha registrato la spesa di 17,99 € del 2026-10-05 collegata a "Netflix"
  Quando "Mario" apre gli abbonamenti
  Allora "Netflix" è da registrare con l'addebito del 2026-11-05 di 17,99 €
  Quando "Mario" registra la spesa proposta
  Allora il saldo di "Conto Intesa" è 1.964,02 €
  E "Netflix" non è da registrare, con prossimo addebito il 2026-12-05
```

### UC16-S5

Test: `UC16_GestireAbbonamenti.S5_UnaSpesaNelloStessoMeseVale`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Il pagamento registrato in un altro giorno del mese vale
  Dato l'abbonamento "Netflix" di UC16-S4, con l'addebito del 2026-11-05 da registrare
  Quando "Mario" registra il 2026-11-09 una spesa di 19,99 € in "Svago" collegata a "Netflix"
  Allora "Netflix" non è da registrare
  E l'importo mensile di "Netflix" è ancora 17,99 €
  Quando "Mario" elimina quella spesa
  Allora "Netflix" è di nuovo da registrare con l'addebito del 2026-11-05
```

### UC16-S6

Test: `UC16_GestireAbbonamenti.S6_GiornoDiAddebito` (e `SubscriptionTests.Charge_FallsOnTheDayOfTheFirstOrOnTheLastOfTheMonth` nel dominio)

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Schema dello scenario: Il giorno di addebito nei mesi più corti
  Dato un abbonamento con primo addebito il 2026-01-31
  Allora l'addebito di <mese> cade il <giorno>

  Esempi:
    | mese          | giorno     |
    | febbraio 2026 | 2026-02-28 |
    | marzo 2026    | 2026-03-31 |
    | aprile 2026   | 2026-04-30 |
    | febbraio 2028 | 2028-02-29 |
```

### UC16-S7

Test: `UC16_GestireAbbonamenti.S7_GliAddebitiPrimaDellaCreazione`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Un abbonamento che l'utente pagava già
  Quando oggi "Mario" crea l'abbonamento "Palestra" da "Conto Intesa", 45,00 € al mese, primo addebito il 2026-09-03
  Allora "Palestra" non è da registrare, con prossimo addebito il 2026-12-03
  Quando "Mario" collega a "Palestra" la sua spesa di 45,00 € del 2026-10-03
  Allora la spesa è collegata a "Palestra"
```

### UC16-S8

Test: `UC16_GestireAbbonamenti.S8_SaltareUnAddebito`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Saltare l'addebito di un mese gratuito
  Dato l'abbonamento "Netflix" di UC16-S4, con l'addebito del 2026-11-05 da registrare
  Quando "Mario" salta l'addebito del 2026-11-05
  Allora "Netflix" non è da registrare
  E il saldo di "Conto Intesa" non è cambiato
  Quando arriva il 2026-12-05
  Allora "Netflix" è da registrare con l'addebito del 2026-12-05
```

### UC16-S9

Test: `UC16_GestireAbbonamenti.S9_Annullare`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Annullare con una data di fine futura
  Dato l'abbonamento "Netflix" di UC16-S4, con la spesa del 2026-11-05 registrata
  Quando "Mario" annulla "Netflix" con data di fine il 2026-12-10
  Allora "Netflix" è annullato con data di fine il 2026-12-10 ma conta ancora nel totale mensile
  E il suo ultimo addebito è il 2026-12-05
  Quando il 2026-12-05 "Mario" registra la spesa proposta
  E arriva il 2026-12-11
  Allora "Netflix" non conta più nel totale mensile
  E dal 2027-01-05 in poi "Netflix" non è mai da registrare
```

### UC16-S10

Test: `UC16_GestireAbbonamenti.S10_DataDiFineNonValida`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Schema dello scenario: Data di fine non valida
  Dato l'abbonamento "Netflix" di UC16-S4, con la spesa del 2026-11-05 registrata
  Quando "Mario" annulla "Netflix" con data di fine il <data>
  Allora la richiesta è rifiutata con "InvalidValue" su "endsOn"

  Esempi:
    | data       | perché                                  |
    | 2026-09-30 | prima del primo addebito                |
    | 2026-11-04 | prima dell'ultima spesa collegata       |
```

### UC16-S11

Test: `UC16_GestireAbbonamenti.S11_SpeseCollegateNonValide`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Schema dello scenario: Una spesa che non può essere collegata
  Dato l'abbonamento "Netflix" di UC16-S4, annullato con data di fine il 2026-11-10
  E "Mario" ha il conto "Revolut USD" in dollari e il finanziamento "Auto"
  Quando "Mario" registra <movimento> collegato a "Netflix"
  Allora la richiesta è rifiutata con "InvalidValue" su "<campo>"

  Esempi:
    | movimento                                | campo          |
    | un ricavo                                | subscriptionId |
    | una spesa che è anche una rata di "Auto" | subscriptionId |
    | una spesa da "Revolut USD"               | accountId      |
    | una spesa del 2026-11-12                 | date           |
    | una spesa del 2026-09-30                 | date           |
```

### UC16-S12

Test: `UC16_GestireAbbonamenti.S12_Eliminare`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Si elimina solo un abbonamento senza spese collegate
  Dato che "Mario" ha l'abbonamento "Spotify" senza spese collegate
  E l'abbonamento "Netflix" di UC16-S4, con una spesa collegata
  Quando "Mario" elimina "Spotify"
  Allora "Spotify" non c'è più
  Quando "Mario" elimina "Netflix"
  Allora la richiesta è rifiutata con "InvalidValue" su "payments"
  E "Netflix" c'è ancora
```

### UC16-S13

Test: `UC16_GestireAbbonamenti.S13_ModificareNonCambiaLePassate`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Un aumento di prezzo
  Dato l'abbonamento "Netflix" di UC16-S4, con la spesa del 2026-11-05 registrata
  Quando "Mario" porta l'importo di "Netflix" a 19,99 €
  Allora il totale mensile è 19,99 €
  E la spesa del 2026-11-05 è ancora di 17,99 €
  E il saldo di "Conto Intesa" non è cambiato
```

### UC16-S14

Test: `UC16_GestireAbbonamenti.S14_AbbonamentoDiUnAltroUtente`

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: Abbonamento di un altro utente
  Dato che "Anna" ha l'abbonamento "Netflix"
  Quando "Mario" modifica, annulla, salta o elimina il "Netflix" di "Anna"
  Allora la risposta è "non trovato"
  Quando "Mario" registra una spesa collegata al "Netflix" di "Anna"
  Allora la richiesta è rifiutata con "InvalidValue" su "subscriptionId"
  E il "Netflix" di "Anna" non è cambiato
```

### UC16-S15

Test: `UC16_GestireAbbonamenti.S15_RegistraDallaDashboard` (pagina)

```gherkin
# language: it
Funzionalità: Gestire gli abbonamenti

Scenario: "Registra" apre la spesa già compilata
  Dato l'abbonamento "Netflix" di UC16-S4, con l'addebito del 2026-11-05 da registrare
  Quando "Mario" preme "Registra" su "Netflix" nella dashboard
  Allora si apre la spesa con data 2026-11-05, importo 17,99 €, conto "Conto Intesa", categoria "Abbonamenti" e abbonamento "Netflix"
  E nessun movimento è stato registrato finché "Mario" non salva
```

Oltre agli scenari, la suite del backend copre il cambio di conto e il primo addebito spostato con spese collegate (R12), la riattivazione che non rende da registrare gli addebiti del periodo annullato (R14), il salto di un addebito che non è quello da registrare (R9) e `MySync:MaxSubscriptions` a 0 (R4); la suite della pagina copre "Salta", la creazione, l'annullamento con data di fine, l'eliminazione rifiutata con spese collegate e il pulsante disabilitato oltre il limite.
