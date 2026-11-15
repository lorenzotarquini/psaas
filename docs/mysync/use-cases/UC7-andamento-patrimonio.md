# UC7 – Vedere l'andamento del patrimonio

**Attore:** Utente
**Obiettivo:** capire se il proprio patrimonio cresce o cala nel tempo, e perché (conti o debiti).

## Flusso principale
1. Dalla dashboard l'utente apre l'andamento (la dashboard mostra già gli ultimi 30 giorni).
2. Sceglie il periodo: 30 giorni, 90 giorni, 12 mesi, tutto (dalla data di apertura del primo conto).
3. Il sistema mostra un grafico con una serie di punti, uno per giorno, con:
   - **patrimonio netto**, somma dei saldi dei conti (linea principale);
   - **debiti residui**, somma dei residui dei debiti, accanto e non sottratti (D9).
4. Passando sopra un punto l'utente legge data e i tre valori.
5. Il sistema mostra anche la variazione del patrimonio netto sul periodo, in euro e in percentuale.

## Flussi alternativi
- **2a. Il periodo inizia prima del primo conto:** il grafico parte dalla data di apertura del primo conto.
- **3a. Un solo giorno di dati** (configurazione appena fatta): il sistema mostra il valore di oggi e il messaggio che l'andamento si costruirà con i movimenti.
- **5a. Patrimonio di partenza pari a 0:** la percentuale non si mostra (solo la variazione in euro).

## Regole
- R1. Il valore di ogni giorno *d* usa le regole di UC4 limitate ai movimenti con data ≤ *d*; un conto o un debito conta solo dal giorno della sua creazione.
- R2. Un debito conta con il suo residuo iniziale dal giorno di creazione, ridotto dalle rate dalla loro data.
- R3. Per "12 mesi" e "tutto" il sistema può restituire un punto per settimana o per mese (l'ultimo giorno del periodo), per tenere il grafico leggibile; il punto finale è sempre oggi.
- R4. Un movimento registrato con una data passata cambia i punti da quella data in poi.
- R5. Il grafico rispetta tema chiaro e scuro, ha una tabella dei dati accessibile come alternativa e funziona da telefono.

## Scenari di accettazione

Test: [`UC7_AndamentoPatrimonio`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC7_AndamentoPatrimonio.cs)

### UC7-S1

Test: `UC7_AndamentoPatrimonio.S1_AndamentoGiornaliero`

```gherkin
# language: it
Funzionalità: Vedere l'andamento del patrimonio

Scenario: Andamento di pochi giorni
  Dato che oggi è il 2026-11-15
  E "Mario" ha creato il 2026-11-12 il conto "Conto Intesa" con saldo iniziale 1.000,00 €
  E il 2026-11-13 ha creato il finanziamento "Auto" con residuo 500,00 €
  E il 2026-11-14 ha registrato una rata di 100,00 € di "Auto" da "Conto Intesa"
  Quando "Mario" chiede l'andamento degli ultimi 30 giorni
  Allora i punti sono:
    | data       | patrimonio netto | debiti residui |
    | 2026-11-12 | 1.000,00         | 0,00           |
    | 2026-11-13 | 1.000,00         | 500,00         |
    | 2026-11-14 | 900,00           | 400,00         |
    | 2026-11-15 | 900,00           | 400,00         |
```

### UC7-S2

Test: `UC7_AndamentoPatrimonio.S2_MovimentoConDataPassata`

```gherkin
# language: it
Funzionalità: Vedere l'andamento del patrimonio

Scenario: Un movimento con data passata cambia i punti successivi
  Dato l'andamento di UC7-S1
  Quando "Mario" registra oggi un ricavo di 200,00 € su "Conto Intesa" con data 2026-11-13
  Allora il patrimonio netto è 1.200,00 € il 2026-11-13 e 1.100,00 € dal 2026-11-14 in poi
  E il punto del 2026-11-12 non è cambiato
```

### UC7-S3

Test: `UC7_AndamentoPatrimonio.S3_VariazioneSulPeriodo`

```gherkin
# language: it
Funzionalità: Vedere l'andamento del patrimonio

Scenario: Variazione sul periodo
  Dato che il patrimonio netto di "Mario" era 10.000,00 € 30 giorni fa ed è 10.500,00 € oggi
  Quando "Mario" chiede l'andamento degli ultimi 30 giorni
  Allora la variazione è +500,00 €, pari a +5,0%
```

### UC7-S4

Test: `UC7_AndamentoPatrimonio.S4_PeriodoNonValido`

```gherkin
# language: it
Funzionalità: Vedere l'andamento del patrimonio

Scenario: Periodo non previsto
  Quando "Mario" chiede l'andamento per il periodo "7 anni"
  Allora la richiesta è rifiutata con "InvalidValue" sul periodo
```
