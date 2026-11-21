# UC17 – Pagare l'abbonamento a MySync

**Attore:** Utente
**Sistemi coinvolti:** Stripe (pagamenti, fatture, email), Keycloak
**Obiettivo:** che l'utente paghi MySync da solo, veda se il pagamento è andato a buon fine e lo sistemi quando non va, senza interventi dell'amministratore.

Non va confuso con gli abbonamenti dell'utente (UC16), che sono spese che MySync gli aiuta a tenere sotto controllo: questo è l'abbonamento **a** MySync.

## Flusso principale
1. L'utente apre Impostazioni › Abbonamento e fatturazione (la seconda scheda, separata da Account): vede lo stato (attivo, pagamento non riuscito, scaduto), il piano con il suo prezzo, il prossimo rinnovo e il metodo di pagamento.
2. Con l'abbonamento scaduto sceglie uno dei piani in vendita (prezzo, periodicità, IVA, eventuale prova) e preme "Abbonati" ("Rinnova" se era già cliente): MySync lo porta sulla pagina di pagamento di Stripe (Checkout).
3. L'utente paga; Stripe lo riporta alle Impostazioni e gli manda l'email con la fattura.
4. MySync legge da Stripe il nuovo stato (subito al ritorno e a ogni evento di Stripe): l'abbonamento è attivo e l'utente torna a modificare i suoi dati.
5. Ogni mese Stripe rinnova e addebita da solo, con l'email della fattura.

## Flussi alternativi
- **5a. Il rinnovo non riesce:** Stripe manda l'email di pagamento non riuscito e riprova nei giorni seguenti. Nelle Impostazioni l'utente vede l'errore (es. carta rifiutata) con "Paga ora" e "Aggiorna metodo di pagamento"; intanto continua a usare MySync.
- **5b. Tutti i tentativi falliscono:** Stripe chiude l'abbonamento; MySync passa in sola lettura e nelle Impostazioni resta l'errore dell'ultimo pagamento con "Rinnova".
- **1a. Gestire l'abbonamento attivo:** "Gestisci" apre il portale clienti di Stripe: metodo di pagamento, fatture, disdetta a fine periodo.
- **1b. Passare a un piano superiore:** con l'abbonamento attivo le Impostazioni mostrano i piani di un tier più alto; l'utente ne sceglie uno e preme "Passa a questo piano". Stripe addebita subito, sulla carta dell'abbonamento, la differenza per il resto del periodo; pagata la differenza, il nuovo tier vale subito e i rinnovi seguenti sono al nuovo prezzo.
- **1b-a. La differenza non viene pagata:** il piano resta quello attuale e le Impostazioni mostrano il motivo (es. fondi insufficienti) con l'invito ad aggiornare il metodo di pagamento.
- **2a. L'utente non è un utente finale (gruppo `end_users`):** il pagamento è rifiutato.
- **2b. L'abbonamento è già attivo o in attesa di pagamento:** non si apre un secondo pagamento; si usa "Gestisci", "Paga ora" o, per salire di livello, il flusso 1b.

## Regole
- R1. I piani in vendita sono quelli del modulo `mysync` definiti nell'Admin Portal (UC16 dell'Admin Portal), letti da Stripe: nome, prezzo, periodicità, aliquota IVA, prova. Cambiare il prezzo di un piano non tocca chi è già abbonato. Si sceglie solo un piano in vendita (`Required`/`InvalidValue` su `planCode`).
- R2. Ogni utente del gruppo `end_users` è un cliente Stripe, creato da MySync al primo pagamento con l'email e il nome del token; il cliente è legato all'utente (`sub`), mai a un'organizzazione.
- R3. Stati: **Attivo** (Stripe `active`, `trialing`), **Pagamento non riuscito** (`past_due`), **Scaduto** (nessun abbonamento, oppure `canceled`, `unpaid`, `incomplete`, `incomplete_expired`, `paused`). Chi non ha mai pagato, compresi gli utenti che usavano MySync prima di Stripe, è Scaduto.
- R4. **Sola lettura:** con l'abbonamento Scaduto ogni modifica (`POST`, `PUT`, `DELETE` sotto `/api`, tranne `/api/membership`) risponde `402`; la lettura resta libera. Con Pagamento non riuscito l'utente modifica ancora, finché Stripe riprova.
- R5. Lo stato lo decide Stripe: MySync ne tiene una copia, riletta da Stripe a ogni evento e al ritorno dal pagamento, quindi un evento arrivato due volte o fuori ordine non lo sbaglia.
- R6. Da Stripe si torna sempre a Impostazioni di MySync, scheda "Abbonamento e fatturazione" (`/settings?tab=billing&membership=…`): l'indirizzo lo costruisce il backend da `Membership:FrontendUrl`, il browser non lo sceglie.
- R7. Email, solleciti, tentativi e fatture sono di Stripe (Dashboard › Settings › Billing); MySync non manda email.
- R8. La prova gratuita del piano vale solo per chi non è mai stato cliente Stripe di MySync.
- R9. MySync implementa il contratto dei moduli ([architettura-moduli.md](../../architettura-moduli.md)): ogni piano vende un **tier** (Base, Plus, Pro); il tier pagato decide i **limiti** (conti, debiti, abbonamenti; un limite non dichiarato resta quello di configurazione) e le **feature** (`projection` dal Plus in su: altrimenti `403` `feature-not-in-tier` e la pagina invita a cambiare piano). Senza tier pagato vale la sola lettura (R4); scendendo di tier i dati restano, oltre i limiti non se ne creano di nuovi. Si vendono solo piani di un tier che MySync dichiara.
- R10. **Passaggio a un piano superiore** (flusso 1b): solo con l'abbonamento Attivo (`NotActive` su `membership` altrimenti: scaduto si abbona, con pagamento non riuscito prima paga) e solo verso un piano in vendita di un tier **più alto** di quello pagato (`NotAnUpgrade` su `planCode`; un abbonamento senza tier può passare a qualunque tier). La differenza per il resto del periodo si paga subito; il nuovo piano vale solo se il pagamento riesce (`PaymentFailed` su `membership`, il piano non cambia). Un'aliquota IVA diversa da quella attuale vale dalla fattura successiva. Scendere di livello si fa disdicendo e abbonandosi di nuovo alla scadenza.

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
  Allora l'abbonamento è "Expired" e i piani in vendita sono "mysync-mensile" a 4,99 EUR al mese con IVA 22% inclusa e "mysync-annuale"
  E Luca vede i suoi conti ma non può crearne uno nuovo (402)
```

### UC17-S2

Test: `UC17_AbbonamentoMySync.S2_RinnovoConCheckout`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Rinnovare con il pagamento di Stripe
  Dato che l'abbonamento di Luca è scaduto
  Quando Luca sceglie "mysync-annuale" e preme "Abbonati"
  Allora riceve l'indirizzo della pagina di pagamento di Stripe per quel piano ed è diventato cliente Stripe con la sua email
  Quando paga e torna in MySync
  Allora l'abbonamento è "Active" con il piano annuale, la carta usata e il prossimo rinnovo
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

### UC17-S10

Test: `UC17_AbbonamentoMySync.S10_SoloPianiInVendita`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Solo un piano in vendita
  Quando Luca chiede il pagamento di un piano archiviato, oppure senza piano
  Allora la richiesta è rifiutata con "InvalidValue", oppure "Required", su "planCode"
```

### UC17-S11

Test: `UC17_AbbonamentoMySync.S11_ProvaSoloLaPrimaVolta`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: La prova vale una volta
  Dato il piano "mysync-mensile" con 14 giorni di prova
  Quando Luca apre il pagamento due volte
  Allora solo la prima volta il pagamento ha la prova
```

### UC17-S12

Test: `UC17_AbbonamentoMySync.S12_FeatureFuoriDalTier`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Feature fuori dal tier
  Dato che Anna paga un piano di tier "Base" e Mario uno di tier "Plus"
  Quando aprono la proiezione
  Allora Anna riceve 403 "feature-not-in-tier" per "projection" e Mario la vede
```

### UC17-S13

Test: `UC17_AbbonamentoMySync.S13_PianoDiUnLivelloSconosciuto`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Piano di un livello che MySync non dichiara
  Quando Luca chiede il pagamento di un piano di tier 4
  Allora la richiesta è rifiutata con "InvalidValue" su "planCode" e il piano non compare tra quelli in vendita
```

### UC17-S14

Test: `UC17_AbbonamentoMySync.S14_ManifestPubblico`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Il manifest di MySync
  Quando qualcuno chiede il manifest di MySync, anche senza token
  Allora riceve l'id "mysync" e i tier "Base", "Plus" e "Pro"
```

### UC17-S15

Test: `UC17_AbbonamentoMySync.S15_PassareAUnTierSuperiore`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Passare a un piano di un tier più alto
  Dato che Mario paga un piano di tier "Plus"
  Quando apre i piani, sceglie "mysync-annuale" (tier "Pro") e preme "Passa a questo piano"
  Allora solo "mysync-annuale" gli è proposto come piano superiore
  E Stripe sposta il suo abbonamento su "mysync-annuale" e Mario ha subito il tier "Pro"
```

### UC17-S16

Test: `UC17_AbbonamentoMySync.S16_SoloVersoUnTierSuperiore`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: Solo verso un tier più alto, solo con l'abbonamento attivo
  Quando Mario, di tier "Plus", chiede di passare a "mysync-mensile", anch'esso "Plus"
  Allora la richiesta è rifiutata con "NotAnUpgrade" su "planCode"
  Quando Luca, con l'abbonamento scaduto, chiede di passare a "mysync-annuale"
  Allora la richiesta è rifiutata con "NotActive" su "membership"
  E senza piano la richiesta è rifiutata con "Required" su "planCode", e Stripe non riceve nulla
```

### UC17-S17

Test: `UC17_AbbonamentoMySync.S17_DifferenzaRifiutata`

```gherkin
# language: it
Funzionalità: Abbonamento a MySync

Scenario: La carta rifiuta la differenza
  Dato che la carta di Mario rifiuta il prossimo addebito
  Quando passa a "mysync-annuale"
  Allora la richiesta è rifiutata con "PaymentFailed" su "membership"
  E Mario resta "Active" con il tier "Plus" e l'errore "card_declined"
```
