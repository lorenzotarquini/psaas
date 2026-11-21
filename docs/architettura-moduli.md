# Architettura dei moduli: contratto, tier e abbonamenti

Decisa il 2026-11-20. Un modulo (es. MySync) è l'implementazione di un **contratto** della piattaforma. Il contratto
dice quali livelli di abbonamento esistono (tier); il modulo dichiara cosa dà ciascun livello e, a runtime, decide da
solo come comportarsi in base al tier che l'utente ha pagato. Associando un piano a un modulo e a un tier si ottiene
l'abbonamento per feature di tutta la piattaforma.

```
 configurazione (solo quando l'amministratore entra)       runtime (ogni accesso)
 ┌──────────────┐  piano = modulo + tier + prezzo + IVA   ┌────────┐      ┌──────────────────────────┐
 │ Admin Portal │ ───────────────────────────────────────▶│ Stripe │◀─────│ modulo (es. MySync)      │
 └──────────────┘                                         └────────┘ tier │  implementa il contratto │
        ▲ legge i tier dal manifest                                 pagato│  decide feature e limiti │
        │                                                                 └──────────────────────────┘
 ┌──────────────────────────── contratto Module (FinHorizon.Modules.Sdk) ─────────────────────────────┐
 │ manifest con tier 1·2·3 (nome, feature, limiti) · nessun tier pagato = sola lettura · downgrade     │
 └─────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Regole del contratto

- **C1. Livelli.** Esistono tre livelli, ordinati: Tier 1 < Tier 2 < Tier 3. Un modulo ne dichiara da 1 a 3 nel
  manifest; un livello superiore dà almeno quanto quello inferiore.
- **C2. Dichiarazione.** Per ogni livello il modulo dichiara nome mostrato all'utente, **feature** (chiavi stabili,
  es. `projection`) e **limiti** (chiave → numero massimo, es. `accounts: 10`).
- **C3. Diritto d'accesso.** Un utente ha un tier di un modulo se ha un abbonamento **pagato** (Stripe `active`,
  `trialing` o, mentre Stripe riprova, `past_due`) a un piano associato a quel modulo; il tier è quello del piano.
  Altrimenti non ha tier.
- **C4. Nessun tier pagato.** Il modulo si apre in **sola lettura**: si vedono i propri dati, ogni modifica risponde
  `402` (`membership-expired`), e si propongono i piani.
- **C5. Feature.** Una feature che il tier dell'utente non comprende risponde `403` con
  `type: "feature-not-in-tier"` e la feature richiesta; il frontend propone di passare a un tier che la comprende.
- **C6. Downgrade.** Scendere di tier non cancella mai dati. Oltre un limite non si crea niente di nuovo
  (`LimitReached`); quello che esiste resta visibile e modificabile.
- **C7. Fonte.** A runtime il riferimento è solo Stripe: il modulo verifica da sé (copia locale aggiornata dai webhook
  e al ritorno dal pagamento). L'Admin Portal non è nel percorso dell'utente.

## Chi fa cosa

| Parte | Responsabilità |
|---|---|
| `FinHorizon.Modules.Sdk` | Il contratto: tier nel manifest, diritto d'accesso (`ModuleEntitlement`), regole C4–C6 come tipi e codici condivisi |
| `FinHorizon.Modules.Billing` (nuova, condivisa) | Stripe per qualsiasi modulo: clienti per utente, piani del modulo con il loro tier, Checkout, portale clienti, stato dell'abbonamento con tier, verifica dei webhook |
| Admin Portal | Catalogo dei moduli con i loro tier (dal manifest, UC15); piani = modulo + **tier** + prezzo + IVA, pubblicati su Stripe (UC16, `metadata.tier`) |
| Modulo (MySync) | Dichiara i suoi tier; legge il tier pagato con la libreria; applica limiti e feature; sola lettura senza tier |

## I tier di MySync (valori iniziali, modificabili nel manifest)

| Tier | Nome | Conti | Debiti | Abbonamenti | Proiezione |
|---|---|---|---|---|---|
| 1 | Base | 2 | 1 | 5 | no |
| 2 | Plus | 10 | 10 | 20 | sì |
| 3 | Pro | 100 | 50 | 100 | sì |

Limiti: `accounts`, `debts`, `subscriptions` (sostituiscono `MySync:MaxAccounts`, `MaxDebts`, `MaxSubscriptions`,
che restano solo come valori di riserva senza manifest). Feature: `projection` (UC14 e il riquadro della dashboard).

## Piano d'implementazione

| # | Passo | Dove | Fatto quando | Stato |
|---|---|---|---|---|
| 1 | Contratto: `ModuleTier` (livello, nome, feature, limiti) nel manifest; `ModuleEntitlement` (tier, stato, `CanWrite`, `Has(feature)`, `Limit(chiave)`); codici `membership-expired`, `feature-not-in-tier` | `Shared/FinHorizon.Modules.Sdk` | test unitari delle regole C1–C3 | ✅ |
| 2 | Libreria Stripe condivisa, estratta da MySync e resa indipendente dal modulo; il tier si legge da `metadata.tier` del prodotto | `Shared/FinHorizon.Modules.Billing` | MySync la usa; prova sandbox verde | ✅ |
| 3 | Admin Portal: il manifest registra i tier (UC15, validazione C1–C2); i moduli li salvano; il piano sceglie un **tier dichiarato** dal modulo (UC16); `metadata.tier` su Stripe; migration con i tier di MySync | Admin Portal | scenari UC15/UC16 nuovi verdi | ✅ |
| 4 | MySync: manifest con i tre tier; tier nella copia dell'abbonamento; limiti dal tier (`IResourceLimits`); `projection` vietata senza il tier (C5); manifest esposto per la registrazione | MySync backend | scenari UC17 nuovi verdi, suite verde | ✅ |
| 5 | Frontend: Admin Portal sceglie il tier nel piano e lo mostra; MySync mostra il tier dei piani e quello attuale, e la proiezione bloccata con invito a cambiare piano | frontend | test Vitest verdi | ✅ |
| 6 | Documenti (UC15, UC16, UC17 di MySync, API, STRIPE.md) e prova sulla sandbox | docs | — | ✅ |

Fuori ambito ora: pacchetti di più moduli in un piano, organizzazioni (B2B), fattura elettronica.
