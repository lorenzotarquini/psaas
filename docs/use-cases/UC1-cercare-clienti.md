# UC1 – Cercare i clienti

**Attore:** Amministratore
**Obiettivo:** trovare un cliente o scorrere l'elenco dei clienti.

## Flusso principale
1. L'amministratore apre l'elenco dei clienti.
2. Il sistema mostra i clienti ordinati per ragione sociale, a pagine.
3. L'amministratore scrive un testo di ricerca.
4. Il sistema mostra i clienti la cui ragione sociale, P.IVA o PEC contiene quel testo.

## Flussi alternativi
- **4a. Nessun cliente corrisponde:** il sistema mostra un elenco vuoto.

## Regole
- R1. La ricerca sulla ragione sociale ignora maiuscole, spazi e punteggiatura ("acme s.r.l." trova "ACME Srl").
- R2. La ricerca su P.IVA e PEC ignora le maiuscole.
- R3. Pagina di almeno 1 e al massimo 100 clienti; valori fuori intervallo sono rifiutati.
- R4. Per ogni cliente si vedono ragione sociale, P.IVA, P.IVA completa, PEC e città.

## Scenari di accettazione

Test: [`UC1_CercareClienti`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC1_CercareClienti.cs)

### UC1-S1

Test: `UC1_CercareClienti.S1_ElencoOrdinatoPerRagioneSocialeEPaginato`

```gherkin
# language: it
Funzionalità: Cercare i clienti

Scenario: Elenco ordinato per ragione sociale e diviso in pagine
  Dato che esistono i clienti "Gamma Spa", "Alfa Srl" e "Beta Snc"
  Quando l'amministratore chiede la pagina 1 con 2 clienti per pagina
  Allora vede "Alfa Srl" e "Beta Snc" su un totale di 3
  E nella pagina 2 vede "Gamma Spa"
```

### UC1-S2

Test: `UC1_CercareClienti.S2_RicercaPerRagioneSocialeIgnoraMaiuscoleEPunteggiatura`

```gherkin
# language: it
Funzionalità: Cercare i clienti

Scenario: La ricerca per ragione sociale ignora maiuscole, spazi e punteggiatura
  Dato che esistono i clienti "Alfa Srl" e "Beta Snc"
  Quando l'amministratore cerca "ALFA s.r.l."
  Allora vede solo "Alfa Srl"
```

### UC1-S3

Test: `UC1_CercareClienti.S3_RicercaPerPartitaIva`

```gherkin
# language: it
Funzionalità: Cercare i clienti

Scenario: Ricerca per P.IVA
  Dato che esiste il cliente "Alfa Srl" con P.IVA completa "IT11111111111"
  E il cliente "Beta Snc" con P.IVA completa "IT22222222222"
  Quando l'amministratore cerca "it2222"
  Allora vede solo "Beta Snc"
```

### UC1-S4

Test: `UC1_CercareClienti.S4_RicercaPerPec`

```gherkin
# language: it
Funzionalità: Cercare i clienti

Scenario: Ricerca per PEC
  Dato che esiste il cliente "Alfa Srl" con PEC "alfa@pec.it"
  E il cliente "Beta Snc" con PEC "beta@pec.it"
  Quando l'amministratore cerca "BETA@PEC"
  Allora vede solo "Beta Snc"
```

### UC1-S5

Test: `UC1_CercareClienti.S5_NessunaCorrispondenzaDaElencoVuoto`

```gherkin
# language: it
Funzionalità: Cercare i clienti

Scenario: Nessun cliente corrisponde alla ricerca
  Dato che esiste il cliente "Alfa Srl"
  Quando l'amministratore cerca "zeta"
  Allora vede un elenco vuoto con totale 0
```

### UC1-S6

Test: `UC1_CercareClienti.S6_PaginazioneFuoriIntervalloRifiutata`

```gherkin
# language: it
Funzionalità: Cercare i clienti

Schema dello scenario: Paginazione fuori intervallo
  Quando l'amministratore chiede la pagina <pagina> con <dimensione> clienti per pagina
  Allora la richiesta è rifiutata come non valida

  Esempi:
    | pagina | dimensione |
    | 0      | 20         |
    | 1      | 0          |
    | 1      | 101        |
```

### UC1-S7

Test: `UC1_CercareClienti.S7_IlRiepilogoMostraLaCitta`

```gherkin
# language: it
Funzionalità: Cercare i clienti

Scenario: Il riepilogo di un cliente mostra i dati principali e la città
  Dato che esiste il cliente "Alfa Srl" con sede a "Roma"
  Quando l'amministratore apre l'elenco dei clienti
  Allora per "Alfa Srl" vede P.IVA, P.IVA completa, PEC e la città "Roma"
```
