# UC12 – Vista leggera e importi nascosti

> Ogni conto e debito appartiene a un **gruppo** (`PortfolioGroup` nel dominio): **di tutti i giorni**, sempre visibili; **patrimonio stabile e debiti a lungo termine** (Preziosi, Mutuo); **investimenti e depositi** (Conto deposito, Investimenti, PAC). Ogni gruppo opzionale ha il suo switch.

**Attore:** Utente
**Obiettivo:** vedere sulla dashboard il patrimonio "di tutti i giorni", senza le voci che cambiano poco (patrimonio stabile, investimenti e depositi), e poter nascondere le cifre quando qualcuno guarda lo schermo.

## Flusso principale
1. L'utente apre la dashboard. Sotto il titolo, al posto del sottotitolo, ci sono l'occhio con la scritta "Mostra" e gli switch **Patrimonio stabile e debiti a lungo termine** e **Investimenti e depositi** (spenti); gli importi sono nascosti (`••••`).
2. Ogni switch spento esclude il suo gruppo:
   - patrimonio netto, debiti residui, variazione a 30 giorni e grafico non lo contano;
   - le card Conti e Debiti non ne mostrano le voci e indicano quante ne sono nascoste, gruppo per gruppo.
3. L'utente accende uno switch, o tutti e due: la dashboard aggiunge quei gruppi.
4. L'utente preme l'**occhio** ("Mostra") e le cifre diventano visibili; il pulsante diventa l'occhio barrato con "Nascondi", che le nasconde di nuovo. Da nascosti, tutti gli importi della dashboard sono `••••`, grafico compreso (resta la forma, non le cifre). L'icona mostra cosa fa il clic.

## Regole
- R1. Gruppi: **patrimonio stabile e debiti a lungo termine** = conti Preziosi e debiti Mutuo; **investimenti e depositi** = conti Conto deposito, Investimenti, PAC, Moneta virtuale; tutto il resto è **di tutti i giorni**. La regola sta in un solo punto del dominio (`PortfolioScope`).
- R2. Ogni sessione di lavoro parte con gli switch spenti e gli importi nascosti. Le scelte dell'utente valgono per tutta la sessione: restano cambiando pagina o ricaricando, e si perdono chiudendo la scheda (sessionStorage del browser). Ogni switch è una scelta sola per dashboard e Andamento.
- R3. I movimenti verso o da un conto di un gruppo nascosto contano per la parte visibile: un trasferimento di 1.000 € da "Credem" a "Oro" abbassa di 1.000 € il patrimonio di tutti i giorni.
- R4. Conteggi e limiti di conti e debiti (UC2-R6, UC3-R5) contano sempre anche le voci nascoste.
- R5. Gli switch ci sono in dashboard e nella pagina Andamento (UC7); l'occhio solo in dashboard. La sezione Conti e debiti (UC10) mostra sempre tutto.
- R6. Le API restituiscono tutto per default: `includeStable=false` e `includeInvestments=false` escludono i due gruppi. Conti e debiti arrivano con il loro `group`.

## Scenari di accettazione

Test: [`UC12_VistaLeggera`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC12_VistaLeggera.cs)

### UC12-S1

Test: `UC12_VistaLeggera.S1_DashboardLeggera`

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Dashboard senza voci pesanti
  Dato che "Mario" ha "Credem" (6.000,00 €), "Oro" di tipo Preziosi (20.000,00 €),
    il mutuo "Mutuo Prima Casa" (80.000,00 €) e il finanziamento "Auto" (170,00 €)
  E ha trasferito 1.000,00 € da "Credem" a "Oro"
  Quando apre la dashboard con lo switch spento
  Allora il patrimonio netto è 5.000,00 € e i debiti residui 170,00 €
  E vede solo il conto "Credem" e il debito "Auto"
```

### UC12-S2

Test: `UC12_VistaLeggera.S2_DashboardCompleta`

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Dashboard completa
  Dato i dati di UC12-S1
  Quando accende lo switch
  Allora il patrimonio netto è 26.000,00 € e i debiti residui 80.170,00 €
```

### UC12-S3

Test: `UC12_VistaLeggera.S3_AndamentoLeggero`

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Andamento senza voci pesanti
  Dato i dati di UC12-S1
  Quando chiede l'andamento di 30 giorni con lo switch spento
  Allora parte da 6.000,00 € e arriva a 5.000,00 €, con una variazione di -1.000,00 € (-16,7%)
  E con lo switch acceso resta 26.000,00 € in ogni punto
```

### UC12-S4

Test: [`UC12_VistaLeggera.S4_SwitchPatrimonioPesante`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/dashboard/ui/UC12_VistaLeggera.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Lo switch
  Quando "Mario" apre la dashboard
  Allora lo switch "Patrimonio stabile e debiti a lungo termine" è spento e non vede "Oro" né "Mutuo Prima Casa"
  E le card dicono "1 bene stabile nascosto" e "1 debito a lungo termine nascosto"
  Quando accende lo switch e mostra gli importi
  Allora vede "Oro" e "Mutuo Prima Casa" e il patrimonio netto completo
```

### UC12-S5

Test: `UC12_VistaLeggera.S5_ImportiNascostiDiDefault` (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Importi nascosti di default
  Quando "Mario" apre la dashboard
  Allora nessun importo è leggibile: al loro posto c'è "••••"
  Quando preme "Mostra gli importi"
  Allora vede le cifre e il pulsante diventa "Nascondi gli importi"
```

### UC12-S6

Test: [`UC12_VistaLeggera.S6_AndamentoSenzaVociOpzionaliDiDefault`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/net-worth/ui/UC12_AndamentoLeggero.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Andamento senza gruppi opzionali di default
  Quando "Mario" apre la pagina Andamento
  Allora i due switch sono spenti e l'andamento conta solo le voci di tutti i giorni
  Quando accende "Investimenti e depositi"
  Allora l'andamento comprende conti deposito, investimenti e PAC
  Quando accende anche "Patrimonio stabile e debiti a lungo termine"
  Allora l'andamento comprende tutto
```

### UC12-S7

Test: [`UC12_VistaLeggera.S7_SceltaRicordataNellaSessione`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/dashboard/ui/UC12_VistaLeggera.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: La scelta vale per tutta la sessione
  Dato che "Mario" in dashboard ha acceso lo switch e mostrato gli importi
  Quando apre la pagina Andamento
  Allora lo switch è acceso
  Quando torna in dashboard
  Allora gli importi sono ancora visibili
```

### UC12-S8

Test: [`UC12_VistaLeggera.S8_SoloInvestimentiEDepositi`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC12_VistaLeggera.cs)

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Solo investimenti e depositi
  Dato i dati di UC12-S1 e il PAC "PAC" con 3.000,00 €
  Quando accende solo "Investimenti e depositi"
  Allora il patrimonio netto è 8.000,00 € e i debiti residui 170,00 €
```

### UC12-S9

Test: [`UC12_VistaLeggera.S9_SwitchInvestimentiEDepositi`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/dashboard/ui/UC12_VistaLeggera.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Vista leggera e importi nascosti

Scenario: Lo switch investimenti e depositi
  Quando "Mario" apre la dashboard
  Allora non vede "PAC Fideuram" e la card Conti dice "1 investimento o deposito nascosto"
  Quando accende "Investimenti e depositi"
  Allora vede "PAC Fideuram", ma non "Oro"
```
