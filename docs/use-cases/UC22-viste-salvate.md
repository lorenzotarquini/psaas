# UC22 – Salvare le viste dell'elenco

**Attore:** Amministratore
**Epica:** mock 1c "+ Salva vista"
**Obiettivo:** ritrovare con un clic una combinazione di filtri usata spesso.

## Flusso principale
1. Dopo aver impostato ricerca, filtri e ordinamento, l'amministratore sceglie "Salva vista" e le dà un nome.
2. La vista compare accanto alle schede dell'elenco; un clic riapplica i filtri.
3. L'amministratore può eliminare una sua vista.

## Flussi alternativi
- **1a. Nome vuoto, oltre 40 caratteri o già usato tra le sue viste:** `Required`, `MaxLengthExceeded`, `NotUnique` sul nome.

## Regole
- R1. Le viste sono personali: ognuno vede ed elimina solo le proprie (l'autore viene dal token).
- R2. Una vista ricorda i parametri dell'elenco così come sono nell'indirizzo della pagina.
- R3. Massimo 20 viste per amministratore e area.

## Scenari di accettazione

Test: [`UC22_VisteSalvate`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC22_VisteSalvate.cs)

### UC22-S1

Test: `UC22_VisteSalvate.S1_SalvareERitrovare`

```gherkin
# language: it
Funzionalità: Viste salvate

Scenario: Salvare e ritrovare una vista
  Quando l'amministratore salva la vista "Pro a Milano" con i filtri piano "pro" e provincia "MI"
  Allora tra le sue viste trova "Pro a Milano" con quei filtri
```

### UC22-S2

Test: `UC22_VisteSalvate.S2_NomeGiaUsato`

```gherkin
# language: it
Funzionalità: Viste salvate

Scenario: Nome già usato
  Dato che l'amministratore ha la vista "Pro a Milano"
  Quando salva un'altra vista "pro a milano"
  Allora la richiesta è rifiutata con "NotUnique" sul nome
```

### UC22-S3

Test: `UC22_VisteSalvate.S3_VistePersonali`

```gherkin
# language: it
Funzionalità: Viste salvate

Scenario: Le viste sono personali
  Dato che un altro amministratore ha salvato la vista "Sue"
  Quando l'amministratore elenca le sue viste o prova a eliminare "Sue"
  Allora non la vede e l'eliminazione risponde "non trovata"
```

### UC22-S4

Test: `UC22_VisteSalvate.S4_Eliminare`

```gherkin
# language: it
Funzionalità: Viste salvate

Scenario: Eliminare una vista
  Dato che l'amministratore ha la vista "Pro a Milano"
  Quando la elimina
  Allora non è più tra le sue viste
```
