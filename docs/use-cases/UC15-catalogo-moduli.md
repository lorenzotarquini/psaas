# UC15 – Gestire il catalogo dei moduli

**Attore:** Amministratore
**Epica:** EXP-01
**Obiettivo:** sapere quali moduli esistono, in che versione e con quali requisiti, e registrarne di nuovi.

## Flusso principale
1. L'amministratore apre il catalogo: per ogni modulo vede versione, stato, dipendenze, permessi e quanti clienti lo usano.
2. Registra un modulo incollando il suo manifest JSON (id, nome, versione, dipendenze, permessi, hook di provisioning).
3. Il sistema verifica il manifest e lo salva; un manifest con un id già presente aggiorna il modulo (nuova versione).

## Flussi alternativi
- **3a. Manifest non valido:** il sistema non salva nulla e indica il campo (`id`, `version`, `requires[i]`, `provisionUrl`).
- **3a'. Modificare un modulo:** dalla riga del modulo l'amministratore apre "Modifica": il manifest attuale (tier compresi) si apre già compilato, lo cambia e salva; l'id non cambia.
- **3b. Modulo deprecato:** l'amministratore lo marca come deprecato; resta ai clienti che lo hanno.

## Regole
- R1. Non c'è un modulo obbligatorio: ogni modulo, MySync compreso, si include nei piani e si depreca come gli altri. Il catalogo nasce con MySync.
- R2. `id`: lettere minuscole, cifre e trattini; `version`: semantica (`1.4.0`).
- R3. Un modulo può richiedere solo moduli già nel catalogo, non se stesso.
- R4. Stati: Beta, Disponibile, Deprecato.
- R5. Registrazioni e cambi di stato vanno nell'audit log (UC12).
- R6. Il manifest dichiara i **tier** del modulo ([architettura-moduli.md](../architettura-moduli.md), C1–C2): livelli 1–3 senza ripetizioni, un nome, feature e limiti con chiavi minuscole, limiti ≥ 0; un livello superiore non dà meno di quello inferiore (`InvalidValue` su `tiers[i]`). Un modulo senza tier si vende per intero (organizzazioni). MySync ha i suoi tre tier (Base, Plus, Pro), registrati dalla migration e pubblicati da MySync in `GET /api/module/manifest`.
- R7. Un modulo del catalogo si modifica dall'Admin Portal (`PUT /api/modules/{id}`) con le stesse verifiche della registrazione; l'id resta quello (`InvalidValue` su `id` se il manifest ne porta un altro), un modulo inesistente risponde `404`. Le modifiche valgono per i piani nuovi: i prodotti già pubblicati su Stripe tengono il tier che hanno.

## Scenari di accettazione

Test: [`UC15_CatalogoModuli`](../../src/AdminPortal/Tests/FinHorizon.AdminPortal.Backend.Tests/UC15_CatalogoModuli.cs)

### UC15-S1

Test: `UC15_CatalogoModuli.S1_MySyncNelCatalogoSenzaCore`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Il catalogo nasce con MySync e senza core
  Quando l'amministratore apre il catalogo dei moduli
  Allora vede il modulo "mysync" disponibile e nessun modulo "core"
```

### UC15-S2

Test: `UC15_CatalogoModuli.S2_RegistrareDaManifest`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Registrare un modulo dal manifest
  Quando l'amministratore registra il manifest del modulo "warehouse" versione "1.4.0" con il permesso "warehouse.stock.edit"
  Allora il catalogo contiene "warehouse" in versione "1.4.0" con quel permesso
```

### UC15-S3

Test: `UC15_CatalogoModuli.S3_ManifestNonValido`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Manifest non valido
  Quando l'amministratore registra un manifest con id "Magazzino!", versione "uno" e un requisito inesistente
  Allora la richiesta è rifiutata con "InvalidFormat" su id e versione e "InvalidValue" sul requisito
```

### UC15-S4

Test: `UC15_CatalogoModuli.S4_NuovaVersione`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Una nuova versione aggiorna il modulo
  Dato che il catalogo contiene "warehouse" in versione "1.4.0"
  Quando l'amministratore registra "warehouse" in versione "1.5.0"
  Allora il catalogo contiene un solo "warehouse", in versione "1.5.0"
```

### UC15-S5

Test: `UC15_CatalogoModuli.S5_DeprecareUnModulo`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Deprecare un modulo
  Quando l'amministratore depreca "warehouse" e poi prova a dargli uno stato inesistente
  Allora "warehouse" risulta deprecato e la richiesta con lo stato inesistente è rifiutata
```

### UC15-S6

Test: `UC15_CatalogoModuli.S6_TierDalManifest`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Il manifest dichiara i tier
  Quando l'amministratore registra "warehouse" con i tier "Base" e "Pro" (Pro con la feature "reports" e 1000 articoli)
  Allora il catalogo mostra i due tier di "warehouse" con feature e limiti
  E MySync ha i tier "Base", "Plus" e "Pro"
```

### UC15-S7

Test: `UC15_CatalogoModuli.S7_TierNonValidi`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Un livello superiore che dà meno
  Quando l'amministratore registra un modulo il cui tier 2 non ha una feature del tier 1
  Allora la richiesta è rifiutata con "InvalidValue" su quel tier
```

### UC15-S8

Test: `UC15_CatalogoModuli.S8_ModificareUnModulo`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Modificare un modulo dall'Admin Portal
  Dato il modulo "warehouse" alla versione "1.0.0", senza tier
  Quando l'amministratore lo modifica portandolo alla versione "2.0.0" con i tier "Essenziale" (3 articoli) e "Pro"
  Allora il catalogo mostra "warehouse" alla versione "2.0.0" con quei tier
  E un manifest con un altro id è rifiutato con "InvalidValue" su "id"
  E la modifica di un modulo inesistente risponde 404
```
