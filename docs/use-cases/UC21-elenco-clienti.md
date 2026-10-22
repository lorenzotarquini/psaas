# UC21 – Filtrare, ordinare ed esportare i clienti

**Attore:** Amministratore
**Epica:** UC1 esteso · EXP-02/03/04 · mock 1c
**Obiettivo:** trovare i clienti per stato, piano, modulo, provincia e scadenza, con i dati commerciali in tabella, ed esportarli.

## Flusso principale
1. L'amministratore apre l'elenco clienti: in alto le schede Tutti, Attivi, Trial, Onboarding, Sospesi, Rinnovo < 30 gg, ciascuna con il numero di clienti.
2. Combina ricerca (UC1) e filtri per piano (anche più di uno), modulo attivo, provincia e rinnovo entro N giorni; ordina per ragione sociale, sede, stato, piano o rinnovo.
3. Per ogni cliente vede PEC, sede, stato, piano, moduli, utenti rispetto al limite del piano e rinnovo.
4. Esporta in CSV i clienti filtrati, oppure solo quelli selezionati.

## Flussi alternativi
- **2a. Ordinamento o filtro non valido:** `400` con il parametro (`sort`, `status`, `renewalWithinDays`) = `InvalidValue`.
- **3a. Keycloak non raggiungibile:** l'elenco si vede lo stesso, senza il numero di utenti.

## Regole
- R1. I numeri delle schede contano tutti i clienti, indipendentemente da ricerca e filtri; "Rinnovo < 30 gg" conta i clienti non cessati la cui data di rinnovo (fine prova per chi è in prova) è entro 30 giorni, scadute comprese.
- R2. Filtri diversi si combinano in AND; più piani o più stati in OR.
- R3. La provincia è la sigla del comune della sede, ricavata dall'elenco dei comuni al salvataggio dell'anagrafica.
- R4. Ordine predefinito: ragione sociale; a parità di valore, ragione sociale.
- R5. L'export contiene al massimo 5000 clienti, con le colonne ragione sociale, P.IVA, PEC, comune, provincia, stato, piano, rinnovo.

## Scenari di accettazione

Test: [`UC21_ElencoClienti`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC21_ElencoClienti.cs)

### UC21-S1

Test: `UC21_ElencoClienti.S1_ConteggiPerScheda`

```gherkin
# language: it
Funzionalità: Elenco clienti

Scenario: I numeri delle schede
  Dato che ci sono 2 clienti attivi, 1 in onboarding e 1 sospeso, e uno degli attivi rinnova tra 10 giorni
  Quando l'amministratore apre l'elenco
  Allora le schede mostrano Tutti 4, Attivi 2, Onboarding 1, Sospesi 1, Rinnovo < 30 gg 1
```

### UC21-S2

Test: `UC21_ElencoClienti.S2_FiltroPerPianoEStato`

```gherkin
# language: it
Funzionalità: Elenco clienti

Scenario: Filtrare per piano e stato
  Dato che "Alfa Srl" ha il piano "pro", "Beta Snc" il piano "base" e "Gamma Spa" nessun piano
  Quando l'amministratore filtra per i piani "pro" e "base" e lo stato attivo
  Allora vede "Alfa Srl" e "Beta Snc" con il nome del piano
```

### UC21-S3

Test: `UC21_ElencoClienti.S3_FiltroPerModuloEProvincia`

```gherkin
# language: it
Funzionalità: Elenco clienti

Scenario: Filtrare per modulo e provincia
  Dato che "Alfa Srl" a Roma ha il modulo "warehouse" e "Beta Snc" a Milano no
  Quando l'amministratore filtra per il modulo "warehouse" e poi per la provincia "MI"
  Allora vede prima solo "Alfa Srl" e poi solo "Beta Snc"
```

### UC21-S4

Test: `UC21_ElencoClienti.S4_OrdinamentoERinnovo`

```gherkin
# language: it
Funzionalità: Elenco clienti

Scenario: Ordinare per rinnovo
  Dato che "Alfa Srl" rinnova tra un mese e "Beta Snc" tra un anno
  Quando l'amministratore ordina per rinnovo decrescente e filtra il rinnovo entro 60 giorni
  Allora l'ordine è "Beta Snc", "Alfa Srl" e il filtro lascia solo "Alfa Srl"
```

### UC21-S5

Test: `UC21_ElencoClienti.S5_UtentiRispettoAlLimite`

```gherkin
# language: it
Funzionalità: Elenco clienti

Scenario: Utenti rispetto al limite
  Dato che "Alfa Srl" ha 2 utenti e il piano ne ammette 50
  Quando l'amministratore apre l'elenco
  Allora la riga di "Alfa Srl" mostra 2 utenti su 50 e i moduli del piano
```

### UC21-S6

Test: `UC21_ElencoClienti.S6_EsportaCsv`

```gherkin
# language: it
Funzionalità: Elenco clienti

Scenario: Esportare in CSV
  Dato che ci sono "Alfa Srl" e "Beta Snc"
  Quando l'amministratore esporta solo "Beta Snc"
  Allora riceve un CSV con l'intestazione e la sola riga di "Beta Snc"
```

### UC21-S7

Test: `UC21_ElencoClienti.S7_ParametriNonValidi`

```gherkin
# language: it
Funzionalità: Elenco clienti

Scenario: Parametri non validi
  Quando l'amministratore ordina per "colore" o filtra il rinnovo entro 0 giorni
  Allora la richiesta è rifiutata con "InvalidValue" sul parametro
```
