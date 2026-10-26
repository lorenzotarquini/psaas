# UC28 – Vedere cosa c'è da gestire

**Attore:** Amministratore
**Epica:** EXP-11 · mock 1b (campanella, coda "Da gestire")
**Obiettivo:** accorgersi subito di quello che richiede un intervento, senza aprire ogni pagina.

## Flusso principale
1. La campanella in alto mostra quante cose ci sono da gestire; aprendola l'amministratore vede l'elenco, dal più urgente.
2. Ogni voce dice cosa succede, a quale cliente, e porta alla pagina dove risolverlo.

## Flussi alternativi
- **1a. Nulla da gestire:** la campanella non ha numero e l'elenco lo dice.

## Regole
- R1. Voci: provisioning di un modulo fallito (critico), fattura scaduta (critico), prova che finisce entro 7 giorni (attenzione), cliente al 90% o più degli utenti del piano (attenzione), bozza di onboarding ferma da più di 7 giorni (informazione).
- R2. Le voci si calcolano al momento: spariscono da sole quando il problema è risolto (nessuno stato "letto").
- R3. Ordine: critico, attenzione, informazione; a parità, dalla più vecchia.

## Scenari di accettazione

Test: [`UC28_Notifiche`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC28_Notifiche.cs)

### UC28-S1

Test: `UC28_Notifiche.S1_CodaOrdinata`

```gherkin
# language: it
Funzionalità: Cose da gestire

Scenario: Coda ordinata per gravità
  Dato che "Alfa Srl" ha una fattura scaduta, "Beta Snc" una prova che finisce tra 3 giorni e "Gamma Spa" una bozza ferma da 10 giorni
  Quando l'amministratore apre la campanella
  Allora vede 3 voci: la fattura di "Alfa Srl", la prova di "Beta Snc", la bozza di "Gamma Spa"
```

### UC28-S2

Test: `UC28_Notifiche.S2_SparisceQuandoRisolto`

```gherkin
# language: it
Funzionalità: Cose da gestire

Scenario: La voce sparisce quando è risolta
  Dato che "Alfa Srl" ha una fattura scaduta
  Quando l'amministratore registra il pagamento
  Allora la campanella non ha più voci
```

### UC28-S3

Test: `UC28_Notifiche.S3_LimiteUtenti`

```gherkin
# language: it
Funzionalità: Cose da gestire

Scenario: Cliente vicino al limite di utenti
  Dato che il piano di "Alfa Srl" ammette 10 utenti e ne ha 9
  Quando l'amministratore apre la campanella
  Allora vede che "Alfa Srl" ha 9 utenti su 10
```

### UC28-S4

Test: `UC28_Notifiche.S4_ModuloFallito`

```gherkin
# language: it
Funzionalità: Cose da gestire

Scenario: Provisioning fallito
  Dato che il provisioning di "warehouse" per "Alfa Srl" è fallito
  Quando l'amministratore apre la campanella
  Allora vede il modulo "warehouse" di "Alfa Srl" da riprovare
```
