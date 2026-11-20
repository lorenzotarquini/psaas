# UC16 – Gestire i piani commerciali

**Attore:** Amministratore
**Epica:** EXP-02
**Obiettivo:** definire cosa si vende: per ogni modulo uno o più piani con prezzo e IVA, pubblicati su Stripe come abbonamenti acquistabili ([STRIPE.md](../admin-portal/STRIPE.md)).

## Flusso principale
1. L'amministratore crea un piano con codice, nome, descrizione, **il modulo** a cui dà accesso, prezzo, periodicità, **aliquota IVA** (e natura se 0%), prezzo IVA inclusa o esclusa, giorni di prova e, per le organizzazioni, limiti (utenti, unità produttive).
2. Il sistema verifica, **pubblica il piano su Stripe** (prodotto, prezzo e aliquota IVA) e lo salva.
3. Un piano si modifica o si archivia (non più vendibile): Stripe viene aggiornato allo stesso modo.

## Flussi alternativi
- **2a. Dati non validi o codice già usato:** il sistema non salva nulla e indica il campo.
- **2b. Stripe non risponde:** `503`, il sistema non salva nulla; l'amministratore riprova.
- **2c. Stripe non configurato** (nessuna chiave, es. un ambiente senza pagamenti): il piano si salva ma risulta "non pubblicato".

## Regole
- R1. Codice unico, minuscolo; prezzo ≥ 0; periodicità mensile o annuale; prova 0–90 giorni; limiti ≥ 1 o assenti (illimitato).
- R2. **Un piano dà accesso a un solo modulo**, che deve esistere nel catalogo (`Required` se manca, `InvalidValue` se sono più di uno o sconosciuto).
- R3. Modificare un piano non cambia le sottoscrizioni già in corso, che conservano prezzo e limiti di quando sono state fatte (UC17). Su Stripe: cambiare prezzo, periodicità, IVA inclusa/esclusa crea un **nuovo prezzo** che diventa quello in vendita; chi è già abbonato resta sul vecchio. Nome e descrizione aggiornano il prodotto.
- R4. IVA: aliquota tra 22, 10, 5, 4 e 0 (predefinita 22); con 0 serve la **natura** (`N1`–`N7`, anche con sottocodice come `N2.1`), con un'aliquota maggiore di 0 la natura non va indicata. In Stripe diventa un'aliquota IVA (Tax Rate) italiana, creata una volta e riusata.
- R5. Su Stripe un piano è un prodotto (`metadata.module` = il modulo, `metadata.plan_code` = il codice) con il suo prezzo predefinito (lookup key `plan-<codice>`): così i moduli (es. MySync) trovano i propri piani su Stripe senza chiamare l'Admin Portal.

## Scenari di accettazione

Test: [`UC16_Piani`](../../src/AdminPortal/Tests/FinHorizon.AdminPortal.Backend.Tests/UC16_Piani.cs)

### UC16-S1

Test: `UC16_Piani.S1_CreareUnPiano`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Creare un piano
  Quando l'amministratore crea il piano "pro" a 99 € al mese con al massimo 50 utenti e il modulo "warehouse"
  Allora il piano "pro" include solo "warehouse"
```

### UC16-S2

Test: `UC16_Piani.S2_CodiceDuplicato`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Codice già usato
  Dato che esiste il piano "pro"
  Quando l'amministratore crea un altro piano "pro"
  Allora la richiesta è rifiutata con "NotUnique" sul codice
```

### UC16-S3

Test: `UC16_Piani.S3_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Dati non validi
  Quando l'amministratore crea un piano con prezzo negativo, periodicità "settimanale" e un modulo inesistente
  Allora la richiesta è rifiutata con "InvalidValue" su prezzo, periodicità e modulo
```

### UC16-S4

Test: `UC16_Piani.S4_PubblicatoSuStripe`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Il piano è in vendita su Stripe
  Quando l'amministratore crea il piano "mysync-mensile" del modulo "mysync" a 4,99 € al mese con IVA 22% inclusa
  Allora su Stripe c'è il prodotto del piano per il modulo "mysync" con il prezzo di 4,99 € al mese e l'IVA 22% inclusa
  E il piano risulta pubblicato
```

### UC16-S5

Test: `UC16_Piani.S5_NuovoPrezzoSuStripe`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Cambiare il prezzo crea un nuovo prezzo su Stripe
  Dato il piano "mysync-mensile" pubblicato a 4,99 €
  Quando l'amministratore lo porta a 5,99 €
  Allora su Stripe il prezzo in vendita è 5,99 € e quello da 4,99 € resta per chi è già abbonato
  Quando cambia solo il nome
  Allora il prezzo su Stripe non cambia
```

### UC16-S6

Test: `UC16_Piani.S6_UnSoloModulo`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Un piano, un modulo
  Quando l'amministratore crea un piano senza modulo, oppure con due moduli
  Allora la richiesta è rifiutata con "Required", oppure "InvalidValue", sui moduli
```

### UC16-S7

Test: `UC16_Piani.S7_NaturaIva`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: IVA a zero senza natura
  Quando l'amministratore crea un piano con IVA 0% senza natura, oppure con IVA 7%
  Allora la richiesta è rifiutata con "Required" sulla natura, oppure "InvalidValue" sull'aliquota
```

### UC16-S8

Test: `UC16_Piani.S8_StripeNonRisponde`

```gherkin
# language: it
Funzionalità: Piani commerciali

Scenario: Stripe non risponde
  Dato che Stripe non risponde
  Quando l'amministratore crea un piano
  Allora la risposta è 503 e il piano non c'è
```
