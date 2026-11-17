# UC17 – Pagare l'abbonamento a MySync

**Attore:** Utente
**Sistemi coinvolti:** Stripe (pagamenti, fatture, email), Keycloak
**Obiettivo:** che l'utente paghi MySync da solo, veda se il pagamento è andato a buon fine e lo sistemi quando non va, senza interventi dell'amministratore.

Non va confuso con gli abbonamenti dell'utente (UC16), che sono spese che MySync gli aiuta a tenere sotto controllo: questo è l'abbonamento **a** MySync.

## Flusso principale
1. L'utente apre Impostazioni › Abbonamento: vede lo stato (attivo, pagamento non riuscito, scaduto), il prezzo, il prossimo rinnovo e il metodo di pagamento.
2. Con l'abbonamento scaduto preme "Rinnova": MySync lo porta sulla pagina di pagamento di Stripe (Checkout).
3. L'utente paga; Stripe lo riporta alle Impostazioni e gli manda l'email con la fattura.
4. MySync legge da Stripe il nuovo stato (subito al ritorno e a ogni evento di Stripe): l'abbonamento è attivo e l'utente torna a modificare i suoi dati.
5. Ogni mese Stripe rinnova e addebita da solo, con l'email della fattura.

## Flussi alternativi
- **5a. Il rinnovo non riesce:** Stripe manda l'email di pagamento non riuscito e riprova nei giorni seguenti. Nelle Impostazioni l'utente vede l'errore (es. carta rifiutata) con "Paga ora" e "Aggiorna metodo di pagamento"; intanto continua a usare MySync.
- **5b. Tutti i tentativi falliscono:** Stripe chiude l'abbonamento; MySync passa in sola lettura e nelle Impostazioni resta l'errore dell'ultimo pagamento con "Rinnova".
- **1a. Gestire l'abbonamento attivo:** "Gestisci" apre il portale clienti di Stripe: metodo di pagamento, fatture, disdetta a fine periodo.
- **2a. L'utente non è un utente finale (gruppo `end_users`):** il pagamento è rifiutato.
- **2b. L'abbonamento è già attivo o in attesa di pagamento:** non si apre un secondo pagamento; si usa "Gestisci" o "Paga ora".

## Regole
- R1. Prezzo: 4,99 € al mese, IVA inclusa, da configurazione (`Membership:Price`, `Membership:Currency`). Cambiare il prezzo crea un nuovo prezzo in Stripe: chi è già abbonato resta su quello vecchio.
- R2. Ogni utente del gruppo `end_users` è un cliente Stripe, creato da MySync al primo pagamento con l'email e il nome del token; il cliente è legato all'utente (`sub`), mai a un'organizzazione.
- R3. Stati: **Attivo** (Stripe `active`, `trialing`), **Pagamento non riuscito** (`past_due`), **Scaduto** (nessun abbonamento, oppure `canceled`, `unpaid`, `incomplete`, `incomplete_expired`, `paused`). Chi non ha mai pagato, compresi gli utenti che usavano MySync prima di Stripe, è Scaduto.
- R4. **Sola lettura:** con l'abbonamento Scaduto ogni modifica (`POST`, `PUT`, `DELETE` sotto `/api`, tranne `/api/membership`) risponde `402`; la lettura resta libera. Con Pagamento non riuscito l'utente modifica ancora, finché Stripe riprova.
- R5. Lo stato lo decide Stripe: MySync ne tiene una copia, riletta da Stripe a ogni evento e al ritorno dal pagamento, quindi un evento arrivato due volte o fuori ordine non lo sbaglia.
- R6. Da Stripe si torna sempre a Impostazioni di MySync: l'indirizzo lo costruisce il backend da `Membership:FrontendUrl`, il browser non lo sceglie.
- R7. Email, solleciti, tentativi e fatture sono di Stripe (Dashboard › Settings › Billing); MySync non manda email.

## Scenari di accettazione

Test: [`UC17_AbbonamentoMySync`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC17_AbbonamentoMySync.cs)

### UC17-S1

Test: `UC17_AbbonamentoMySync.S1_UtenteEsistenteScaduto`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Chi usava MySync prima di Stripe ha l'abbonamento scaduto
  Dato che Luca usa MySync e non ha mai pagato
  Quando apre l'abbonamento
  Allora l'abbonamento è "Expired" a 4,99 EUR al mese
  E Luca vede i suoi conti ma non può crearne uno nuovo (402)
```

### UC17-S2

Test: `UC17_AbbonamentoMySync.S2_RinnovoConCheckout`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Rinnovare con il pagamento di Stripe
  Dato che l'abbonamento di Luca è scaduto
  Quando Luca preme "Rinnova"
  Allora riceve l'indirizzo della pagina di pagamento di Stripe ed è diventato cliente Stripe con la sua email
  Quando paga e torna in MySync
  Allora l'abbonamento è "Active" con la carta usata e il prossimo rinnovo
  E Luca può di nuovo creare un conto
```

### UC17-S3

Test: `UC17_AbbonamentoMySync.S3_PagamentoNonRiuscito`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Il rinnovo non riesce
  Dato che Mario è abbonato
  Quando Stripe segnala che il rinnovo è stato rifiutato per fondi insufficienti
  Allora l'abbonamento è "PastDue" con l'errore "insufficient_funds" e l'indirizzo per pagare
  E Mario può ancora creare un conto
```

### UC17-S4

Test: `UC17_AbbonamentoMySync.S4_TentativiEsauriti`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Tutti i tentativi falliscono
  Dato che il rinnovo di Mario non è riuscito
  Quando Stripe chiude l'abbonamento
  Allora l'abbonamento è "Expired" e mostra ancora l'errore dell'ultimo pagamento
  E Mario non può più creare un conto (402)
```

### UC17-S5

Test: `UC17_AbbonamentoMySync.S5_GiaAttivo`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Niente secondo pagamento
  Dato che Mario è abbonato
  Quando preme "Rinnova"
  Allora la richiesta è rifiutata con "AlreadyActive" sull'abbonamento
  E "Gestisci" gli apre il portale clienti di Stripe
```

### UC17-S6

Test: `UC17_AbbonamentoMySync.S6_RitornoSuMySync`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Da Stripe si torna sempre a MySync
  Quando Luca apre il pagamento o il portale clienti
  Allora Stripe lo riporterà alle Impostazioni di MySync
```

### UC17-S7

Test: `UC17_AbbonamentoMySync.S7_SoloUtentiFinali`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Solo gli utenti del gruppo end_users pagano
  Dato un utente con MySync ma senza il ruolo "enduser"
  Quando preme "Rinnova"
  Allora l'accesso è negato (403)
```

### UC17-S8

Test: `UC17_AbbonamentoMySync.S8_EventoNonFirmato`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Evento di Stripe non firmato
  Quando arriva un evento senza firma valida
  Allora la risposta è 400 e nessun abbonamento cambia
```

### UC17-S9

Test: `UC17_AbbonamentoMySync.S9_ClientiDiAltri`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Evento di un cliente che non è di MySync
  Quando arriva un evento firmato per un cliente Stripe che MySync non conosce (es. un'organizzazione dell'Admin Portal)
  Allora la risposta è 200 e MySync non chiede nulla a Stripe
```
