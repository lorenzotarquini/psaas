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
- **3b. Modulo deprecato:** l'amministratore lo marca come deprecato; resta ai clienti che lo hanno.

## Regole
- R1. Il modulo **core** esiste sempre, è in ogni piano e non si depreca.
- R2. `id`: lettere minuscole, cifre e trattini; `version`: semantica (`1.4.0`).
- R3. Un modulo può richiedere solo moduli già nel catalogo, non se stesso.
- R4. Stati: Beta, Disponibile, Deprecato.
- R5. Registrazioni e cambi di stato vanno nell'audit log (UC12).

## Scenari di accettazione

Test: [`UC15_CatalogoModuli`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC15_CatalogoModuli.cs)

### UC15-S1

Test: `UC15_CatalogoModuli.S1_IlCoreCeSempre`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Il core è sempre nel catalogo
  Quando l'amministratore apre il catalogo dei moduli
  Allora vede il modulo "core" disponibile
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

Test: `UC15_CatalogoModuli.S5_DeprecareMaNonIlCore`

```gherkin
# language: it
Funzionalità: Catalogo dei moduli

Scenario: Deprecare un modulo, ma non il core
  Quando l'amministratore depreca "warehouse" e poi prova a deprecare "core"
  Allora "warehouse" risulta deprecato e la richiesta per "core" è rifiutata
```
