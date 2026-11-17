# UC33 – Ricevere gli eventi di Stripe

**Attore:** Stripe (sistema esterno)
**Epica:** EXP-08 · [STRIPE.md](../admin-portal/STRIPE.md) fase 1
**Obiettivo:** che ogni cambiamento di Stripe (pagamento riuscito o fallito, sottoscrizione creata, rinnovata o chiusa) arrivi all'Admin Portal una e una sola volta, e solo se viene davvero da Stripe.

## Flusso principale
1. Stripe invia un evento firmato a `POST /api/stripe/webhook`.
2. Il sistema verifica la firma con il segreto del webhook.
3. Il sistema registra l'evento (id, tipo, data di Stripe, data di ricezione) e risponde `200`.
4. L'amministratore vede gli ultimi eventi ricevuti (`GET /api/stripe/events`).

Le reazioni ai singoli tipi di evento (stato dell'organizzazione, moduli, fatture) arrivano con le fasi 2–5: oggi ogni evento viene solo registrato.

## Flussi alternativi
- **2a. Firma assente, sbagliata o più vecchia di 5 minuti:** `400`, nulla registrato.
- **2b. Segreto del webhook non configurato:** `503`, nulla registrato; Stripe riproverà.
- **3a. Evento già ricevuto (stesso id):** `200`, nessuna nuova registrazione.

## Regole
- R1. La firma (`Stripe-Signature`) è l'unica autenticazione: l'endpoint non chiede un token Keycloak, perché Stripe non ne ha.
- R2. Un evento è registrato una sola volta per id (`evt_…`), anche se arriva due volte insieme.
- R3. Anche i tipi di evento che il portale non usa sono registrati e confermati con `200`, così Stripe non li ripete.
- R4. Nei log solo id e tipo dell'evento: nessun dato del cliente.
- R5. Elenco degli eventi: solo amministratori, dal più recente, al massimo 200 (default 50).

## Scenari di accettazione

Test: [`UC33_EventiStripe`](../../src/AdminPortal/Tests/FinHorizon.AdminPortal.Backend.Tests/UC33_EventiStripe.cs)

### UC33-S1

Test: `UC33_EventiStripe.S1_EventoFirmato`

```gherkin
# language: it
Funzionalità: Eventi di Stripe

Scenario: Evento firmato
  Quando Stripe invia l'evento "invoice.paid" con una firma valida
  Allora la risposta è 200
  E l'amministratore vede l'evento "invoice.paid" tra quelli ricevuti
```

### UC33-S2

Test: `UC33_EventiStripe.S2_FirmaNonValida`

```gherkin
# language: it
Funzionalità: Eventi di Stripe

Scenario: Firma assente o sbagliata
  Quando arriva un evento senza firma, oppure firmato con un altro segreto
  Allora la risposta è 400
  E tra gli eventi ricevuti non c'è nulla
```

### UC33-S3

Test: `UC33_EventiStripe.S3_FirmaScaduta`

```gherkin
# language: it
Funzionalità: Eventi di Stripe

Scenario: Firma troppo vecchia
  Quando arriva un evento firmato correttamente 10 minuti fa
  Allora la risposta è 400
```

### UC33-S4

Test: `UC33_EventiStripe.S4_EventoRipetuto`

```gherkin
# language: it
Funzionalità: Eventi di Stripe

Scenario: Lo stesso evento arriva due volte
  Quando Stripe invia due volte lo stesso evento
  Allora entrambe le risposte sono 200
  E l'evento compare una sola volta tra quelli ricevuti
```

### UC33-S5

Test: `UC33_EventiStripe.S5_SegretoMancante`

```gherkin
# language: it
Funzionalità: Eventi di Stripe

Scenario: Webhook non configurato
  Dato che il segreto del webhook non è configurato
  Quando Stripe invia un evento
  Allora la risposta è 503
```

### UC33-S6

Test: `UC33_EventiStripe.S6_SoloAmministratori`

```gherkin
# language: it
Funzionalità: Eventi di Stripe

Scenario: L'elenco degli eventi è riservato
  Quando qualcuno senza token chiede gli eventi ricevuti
  Allora la risposta è 401
```
