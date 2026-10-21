# UC9 – Consultare la panoramica dei profili

**Attore:** Amministratore
**Obiettivo:** vedere tutti i profili esistenti sulla piattaforma.

## Flusso principale
1. L'amministratore apre la panoramica dei profili.
2. Il sistema mostra tutti i profili di tutti i clienti, ordinati per nome, ciascuno con il cliente a cui appartiene.

## Flussi alternativi
- **2a. Non esistono profili:** il sistema mostra un elenco vuoto.

## Regole
- R1. Solo consultazione.

## Scenari di accettazione

Test: [`UC9_PanoramicaProfili`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC9_PanoramicaProfili.cs)

### UC9-S1

Test: `UC9_PanoramicaProfili.S1_TuttiIProfiliConIlLoroCliente`

```gherkin
# language: it
Funzionalità: Consultare la panoramica dei profili

Scenario: Tutti i profili di tutti i clienti, con il loro cliente
  Dato che "Alfa Srl" ha il profilo "Operatore" e "Beta Snc" ha il profilo "Amministrativo"
  Quando l'amministratore apre la panoramica dei profili
  Allora vede "Amministrativo" di "Beta Snc" e poi "Operatore" di "Alfa Srl"
```

### UC9-S2

Test: `UC9_PanoramicaProfili.S2_NessunProfilo`

```gherkin
# language: it
Funzionalità: Consultare la panoramica dei profili

Scenario: Nessun profilo
  Dato che non esistono profili
  Quando l'amministratore apre la panoramica dei profili
  Allora vede un elenco vuoto
```
