# UC24 – Seguire incassi, solleciti e morosità

**Attore:** Amministratore
**Epica:** EXP-08 · mock 1b (insoluti)
**Obiettivo:** sapere cosa è da incassare, registrare i pagamenti, sollecitare e sospendere chi non paga.

## Flusso principale
1. L'amministratore apre Fatturazione: vede da incassare, scaduto e incassato nel mese, e l'elenco delle fatture filtrabile per stato e cliente.
2. Apre una fattura e ne vede righe, totali, stato e storia (solleciti, pagamento).
3. Quando arriva un bonifico registra il pagamento con data e riferimento: la fattura è Pagata.
4. Ogni giorno il sistema segna come Scadute le fatture oltre la scadenza, invia un sollecito ogni 7 giorni e sospende il cliente scaduto da più di 30 giorni.
5. Una fattura emessa per errore si annulla con un motivo: il sistema emette la nota di credito dello stesso importo.

## Flussi alternativi
- **3a. Fattura già pagata o annullata:** il pagamento è rifiutato (`409`).
- **5a. Fattura pagata:** non si annulla (`409`): serve un rimborso, fuori dal portale.
- **4a. Cliente già sospeso, in disdetta o cessato:** nessuna nuova sospensione.

## Regole
- R1. Stati: Emessa, Scaduta, Pagata, Annullata. Una nota di credito nasce già chiusa (Pagata = compensata).
- R2. Data di pagamento obbligatoria e non futura; riferimento facoltativo (es. CRO).
- R3. Sollecito: registrato sulla fattura (numero e data) e nello storico; l'email partirà con le comunicazioni (EXP-11).
- R4. Sospensione per morosità: automatica, con motivo "Morosità: fattura N", autore "Sistema"; la riattivazione resta all'amministratore (UC13). Soglia in configurazione (`Billing:SuspendAfterDays`, default 30).
- R5. Pagamenti, solleciti, annullamenti e sospensioni vanno nell'audit log.

## Scenari di accettazione

Test: [`UC24_IncassiSolleciti`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC24_IncassiSolleciti.cs)

### UC24-S1

Test: `UC24_IncassiSolleciti.S1_RegistrarePagamento`

```gherkin
# language: it
Funzionalità: Incassi e solleciti

Scenario: Registrare un pagamento
  Dato che "Alfa Srl" ha una fattura emessa
  Quando l'amministratore registra il pagamento di oggi con riferimento "CRO 123"
  Allora la fattura è "Pagata" con quel riferimento
  E un secondo pagamento è rifiutato
```

### UC24-S2

Test: `UC24_IncassiSolleciti.S2_ScadutaESollecito`

```gherkin
# language: it
Funzionalità: Incassi e solleciti

Scenario: Fattura scaduta e sollecito
  Dato che "Alfa Srl" ha una fattura emessa
  Quando passano 31 giorni e gira la fatturazione
  Allora la fattura è "Scaduta" con 1 sollecito
  E dopo altri 7 giorni ha 2 solleciti
```

### UC24-S3

Test: `UC24_IncassiSolleciti.S3_SospensionePerMorosita`

```gherkin
# language: it
Funzionalità: Incassi e solleciti

Scenario: Sospensione per morosità
  Dato che la fattura di "Alfa Srl" è scaduta da più di 30 giorni
  Quando gira la fatturazione
  Allora "Alfa Srl" è "Sospeso" con motivo "Morosità: fattura <numero>"
```

### UC24-S4

Test: `UC24_IncassiSolleciti.S4_AnnullareConNotaDiCredito`

```gherkin
# language: it
Funzionalità: Incassi e solleciti

Scenario: Annullare una fattura
  Dato che "Alfa Srl" ha una fattura emessa di 122 €
  Quando l'amministratore la annulla con motivo "Errore di listino"
  Allora la fattura è "Annullata" e c'è una nota di credito di 122 € collegata
```

### UC24-S5

Test: `UC24_IncassiSolleciti.S5_RiepilogoEFiltri`

```gherkin
# language: it
Funzionalità: Incassi e solleciti

Scenario: Riepilogo e filtri
  Dato che "Alfa Srl" ha una fattura pagata e "Beta Snc" una scaduta
  Quando l'amministratore apre Fatturazione e filtra le scadute
  Allora il riepilogo mostra lo scaduto di "Beta Snc" e l'incassato del mese di "Alfa Srl"
  E l'elenco filtrato contiene solo la fattura di "Beta Snc"
```

### UC24-S6

Test: `UC24_IncassiSolleciti.S6_PagamentoNonValido`

```gherkin
# language: it
Funzionalità: Incassi e solleciti

Scenario: Pagamento non valido
  Quando l'amministratore registra un pagamento senza data o con una data futura
  Allora la richiesta è rifiutata con "Required" o "InvalidValue" sulla data
```
