# Stripe Billing – abbonamenti, pagamenti e fatture

Decisione del 2026-11-17: la filiera commerciale delle organizzazioni passa a **Stripe Billing**. Stripe è la fonte di verità per sottoscrizioni, rinnovi, prove, pro-rata, pagamenti, tentativi ripetuti e email ai clienti; l'Admin Portal ne è lo specchio e reagisce ai suoi eventi (webhook).

Obiettivo: nessun intervento manuale.

- pagamento → riuscito → email con fattura;
- pagamento → non riuscito → email di notifica → nuovi tentativi → (tutti falliti) sospensione;
- nuovo cliente → iscrizione → pagamento riuscito → email con fattura → moduli attivi.

## MySync

L'abbonamento a MySync (4,99 € al mese, per utente) è gestito dal backend di MySync direttamente con Stripe: [UC17 di MySync](../mysync/use-cases/UC17-abbonamento-mysync.md), decisione D21. Cliente Stripe = utente Keycloak del gruppo `end_users`; copia dello stato in `mysync-db`; con l'abbonamento scaduto MySync è in sola lettura. Gli utenti che usavano MySync prima di Stripe risultano scaduti e rinnovano dalle Impostazioni. Prodotto, prezzo (lookup key `mysync-month-499-eur`) e configurazione del portale clienti li crea MySync al primo uso. L'AppHost avvia un secondo container `stripe-cli-mysync` che inoltra gli eventi a `POST /api/membership/webhook` (stesso segreto `whsec_…`). Gli eventi delle organizzazioni (Admin Portal) e quelli di MySync arrivano a entrambi i webhook: ognuno ignora i clienti che non sono suoi.

Prova contro la sandbox vera dell'adattatore (cliente, Checkout, portale, abbonamento pagato e carta rifiutata, poi cancella il cliente):

```bash
STRIPE_SANDBOX_KEY=sk_test_… dotnet test src/MySync/Tests/FinHorizon.MySync.Infrastructure.Tests --filter StripeSandbox
```

## Chi fa cosa

| Passaggio | Chi | Come |
|---|---|---|
| Rinnovo, prova, pro-rata sul cambio piano | Stripe | Subscription |
| Email fattura e ricevuta, pagamento fallito, carta in scadenza, fine prova | Stripe | Dashboard › Settings › Billing › Subscriptions and emails |
| Nuovi tentativi e cosa fare quando falliscono tutti | Stripe | Smart Retries, Dashboard › Billing › Revenue recovery |
| Il cliente aggiorna la carta o disdice | Stripe | Customer Portal (pagina ospitata) |
| Iscrizione e primo pagamento | Stripe | Checkout (pagina ospitata) |
| Piani → Product/Price, organizzazione → Customer | Admin Portal | API Stripe (Stripe.net) |
| Stato dell'organizzazione (Trial, Attivo, Sospeso), moduli, audit | Admin Portal | webhook |
| Fattura elettronica SDI | Admin Portal + intermediario | webhook `invoice.paid` (fase 5) |

## Cosa cambia rispetto alla fatturazione interna

La fatturazione interna (UC23, UC24, fase 8 di [PROGRESS.md](PROGRESS.md)) viene sostituita: niente più rinnovo giornaliero (`BillingJob`), numerazione interna, pagamenti registrati a mano e solleciti calcolati dal portale. Fino alla fase 3 resta attiva così com'è, poi UC23 e UC24 vengono riscritti.

Il prezzo bloccato di UC16-R3 / UC17-R1 resta vero anche con Stripe: un Price non si modifica, cambiare il prezzo di un piano crea un nuovo Price e le sottoscrizioni in corso restano sul vecchio.

## Fasi

| Fase | Contenuto | Stato |
|---|---|---|
| 0 | Questo documento, use case dei webhook (UC33) | ✅ |
| 1 | Fondamenta: Stripe.net, chiavi come parametri Aspire, Stripe CLI in Aspire, endpoint webhook con firma e idempotenza | ✅ codice e test · prova con la sandbox da fare |
| 2 | Piani → Product/Price (UC16 riscritto) | da fare |
| 3 | Organizzazione → Customer, sottoscrizione → Stripe Subscription; eventi → stato e moduli; configurazione Dashboard (email, retry, IVA); UC17, UC23, UC24 riscritti | da fare |
| 4 | Iscrizione self-service con Checkout e Customer Portal (nuovo use case) | da decidere: dove sta la pagina pubblica |
| 5 | Fattura elettronica SDI tramite intermediario | da decidere: quale intermediario |

## Configurazione

| Dove | Chiave | Note |
|---|---|---|
| AppHost (`appsettings.Development.json` o user secrets, mai in git) | `Parameters:stripe-secret-key` | `sk_test_…` della sandbox; la usa la Stripe CLI, dalla fase 2 anche il backend |
| AppHost (idem) | `Parameters:stripe-webhook-secret` | `whsec_…` della Stripe CLI, vedi sotto |
| Backend | `Stripe:WebhookSecret` | arriva dall'AppHost; vuoto = webhook rifiutati con `503` |

### Webhook in locale

L'AppHost avvia il container `stripe-cli` (`stripe listen`), che riceve gli eventi della sandbox e li inoltra a `POST /api/stripe/webhook` del backend. Il segreto di firma della CLI è legato alla chiave e resta lo stesso fra un avvio e l'altro; si ricava una volta con:

```bash
docker run --rm stripe/stripe-cli:v1.55.6 listen --print-secret --api-key <sk_test_…>
```

e si salva come `Parameters:stripe-webhook-secret` dell'AppHost. Per provare un evento a mano, con l'AppHost avviato:

```bash
docker run --rm stripe/stripe-cli:v1.55.6 trigger invoice.paid --api-key <sk_test_…>
```

In produzione l'endpoint si registra nella Dashboard (Developers › Webhooks) e il suo segreto prende il posto di quello della CLI.

## Sandbox

- Le email ai clienti in sandbox arrivano solo agli indirizzi dei membri del team Stripe: per le prove usare quello del ruolo sviluppatore come email del cliente.
- Prodotti, prezzi, clienti e sottoscrizioni non si creano a mano nella Dashboard: li crea il backend.
- Il tempo si fa passare con i test clock di Stripe (rinnovi, fine prova, tentativi).
