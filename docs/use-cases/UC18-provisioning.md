# UC18 – Seguire il provisioning dei moduli

**Attore:** Amministratore
**Epica:** EXP-03 · mock 1b, 1d
**Obiettivo:** sapere se i moduli di un cliente sono pronti e rimediare quando la preparazione fallisce.

## Flusso principale
1. Quando un modulo viene attivato per un cliente, il sistema chiama il suo hook di provisioning.
2. Se risponde, il modulo è Attivo; se no, è "Provisioning fallito" con l'errore.
3. Il sistema riprova da solo ogni pochi minuti, fino a 3 tentativi; l'amministratore può riprovare quando vuole.

## Flussi alternativi
- **2a. Modulo senza hook:** non c'è niente da preparare, è subito Attivo.

## Regole
- R1. Stati del modulo del cliente: In provisioning, Attivo, Provisioning fallito (con errore e numero di tentativi).
- R2. Riuscite e fallimenti vanno nello storico del cliente (UC12) e nella coda "Da gestire" della Panoramica.

## Scenari di accettazione

Test: [`UC18_Provisioning`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC18_Provisioning.cs)

### UC18-S1

Test: `UC18_Provisioning.S1_HookFallito`

```gherkin
# language: it
Funzionalità: Provisioning dei moduli

Scenario: Hook che non risponde
  Dato che l'hook del modulo "warehouse" non risponde
  Quando "Alfa Srl" sottoscrive un piano con "warehouse"
  Allora "warehouse" risulta "Provisioning fallito" con l'errore e "core" risulta attivo
```

### UC18-S2

Test: `UC18_Provisioning.S2_Riprova`

```gherkin
# language: it
Funzionalità: Provisioning dei moduli

Scenario: Riprovare il provisioning
  Dato che "warehouse" di "Alfa Srl" è fallito e l'hook ora risponde
  Quando l'amministratore riprova
  Allora "warehouse" risulta attivo dopo 2 tentativi
```

### UC18-S3

Test: `UC18_Provisioning.S3_NelloStorico`

```gherkin
# language: it
Funzionalità: Provisioning dei moduli

Scenario: Fallimento e riuscita nello storico
  Dato che "warehouse" di "Alfa Srl" è fallito e poi riuscito
  Quando l'amministratore apre lo storico di "Alfa Srl"
  Allora vede sia il fallimento sia la riuscita del provisioning
```
