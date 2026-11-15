# UC1 – Configurare MySync al primo accesso

**Attore:** Utente
**Obiettivo:** dire a MySync dove sono i propri soldi e cosa si deve ancora pagare, per avere subito una dashboard significativa.

## Precondizioni
- L'utente è autenticato con ruolo `enduser` e non ha ancora nessun conto.

## Flusso principale
1. L'utente apre MySync per la prima volta.
2. Il sistema vede che non ha conti e mostra la configurazione guidata invece della dashboard.
3. **Passo "Conti":** l'utente crea uno o più conti (UC2). Il sistema mostra i conti creati e quanti ne può ancora aggiungere.
4. L'utente passa al passo successivo.
5. **Passo "Debiti":** l'utente crea zero o più debiti (UC3), scegliendo il conto collegato tra quelli appena creati.
6. L'utente conclude la configurazione.
7. Il sistema apre la dashboard (UC4), con il patrimonio netto (la somma dei saldi iniziali) e i debiti residui.

## Flussi alternativi
- **4a. Nessun conto creato:** il sistema non permette di passare ai debiti e chiede di creare almeno un conto.
- **5a. L'utente salta i debiti:** conclude subito; potrà aggiungerli dopo dalla dashboard.
- **1a. L'utente ha già almeno un conto:** il sistema apre direttamente la dashboard.
- **1b. L'utente interrompe a metà:** i conti e i debiti già creati restano; al ritorno, se ha almeno un conto, vede la dashboard.

## Regole
- R1. La configurazione è completa quando esiste almeno un conto; non c'è uno stato "configurazione completata" da salvare a parte.
- R2. Ogni conto e debito si salva appena creato (UC2, UC3): la configurazione non è una transazione unica da confermare alla fine.
- R3. Conti e debiti si possono aggiungere anche dopo, dalla dashboard, fino ai limiti `MySync:MaxAccounts` e `MySync:MaxDebts`.

## Scenari di accettazione

Test: [`UC1_ConfigurareMySync`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/setup/ui/UC1_ConfigurareMySync.test.tsx) (frontend, Vitest + Testing Library)

### UC1-S1

Test: `UC1_ConfigurareMySync.S1_PrimoAccessoMostraLaConfigurazione`

```gherkin
# language: it
Funzionalità: Configurare MySync al primo accesso

Scenario: Al primo accesso si apre la configurazione
  Dato che "Mario" non ha nessun conto
  Quando "Mario" apre MySync
  Allora vede il passo "Conti" della configurazione guidata
  E non vede la dashboard
```

### UC1-S2

Test: `UC1_ConfigurareMySync.S2_SenzaContiNonSiProsegue`

```gherkin
# language: it
Funzionalità: Configurare MySync al primo accesso

Scenario: Senza conti non si passa ai debiti
  Dato che "Mario" è al passo "Conti" e non ha creato conti
  Quando prova a passare al passo "Debiti"
  Allora resta al passo "Conti" con l'invito a creare almeno un conto
```

### UC1-S3

Test: `UC1_ConfigurareMySync.S3_ConfigurazioneCompleta`

```gherkin
# language: it
Funzionalità: Configurare MySync al primo accesso

Scenario: Configurazione con un conto e un debito
  Dato che "Mario" non ha nessun conto
  Quando crea il conto corrente "Conto Intesa" con saldo iniziale 2.500,00 €
  E crea il mutuo "Mutuo casa" collegato a "Conto Intesa" con residuo 120.000,00 €
  E conclude la configurazione
  Allora vede la dashboard con patrimonio netto 2.500,00 € e debiti residui 120.000,00 €
```

### UC1-S4

Test: `UC1_ConfigurareMySync.S4_ConUnContoSiApreLaDashboard`

```gherkin
# language: it
Funzionalità: Configurare MySync al primo accesso

Scenario: Chi ha già un conto vede la dashboard
  Dato che "Mario" ha il conto "Conto Intesa"
  Quando "Mario" apre MySync
  Allora vede la dashboard
```
