# UC14 – Attivare un cliente con l'onboarding guidato

**Attore:** Amministratore
**Epica:** EXP-04 · mock 1e
**Obiettivo:** preparare un nuovo cliente in più passi, potendo interrompere e riprendere, e attivarlo solo alla fine.

## Flusso principale
1. **Anagrafica:** l'amministratore compila dati legali e sede (UC3, con controllo dei campi univoci UC11 e ricerca comuni UC10), codice SDI e referente commerciale. Passando al passo successivo il sistema salva il cliente come **bozza** (stato Onboarding).
2. **Piano e moduli:** sceglie piano, prova gratuita e moduli (EXP-02/03, quando disponibili).
3. **Organizzazione:** definisce profili e unità produttive (UC5).
4. **Primi utenti:** crea i primi utenti con i loro profili (UC6).
5. **Riepilogo:** verifica e attiva; il cliente passa ad Attivo (o Trial se il piano prevede la prova).

## Flussi alternativi
- **1a. Dati non validi:** come UC3; nessuna bozza viene creata.
- **2–4a. Passo saltato:** l'amministratore può saltare organizzazione e utenti e attivare subito.
- **xa. Interruzione:** la bozza resta in stato Onboarding e si riprende dall'elenco clienti.
- **xb. Annullamento:** la bozza viene eliminata con tutto ciò che contiene; un cliente già attivato non può essere eliminato (si cessa, UC13).

## Regole
- R1. Una bozza (Onboarding) occupa già ragione sociale, P.IVA e PEC.
- R2. Solo una bozza si può eliminare (`DELETE`); per gli altri clienti la risposta è `409`.
- R3. Codice SDI facoltativo: 7 caratteri (6 per la pubblica amministrazione), lettere e cifre; altrimenti `InvalidFormat`.
- R4. Avvio, attivazione e annullamento della bozza compaiono nello storico (UC12).

## Scenari di accettazione

Test: [`UC14_OnboardingCliente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC14_OnboardingCliente.cs)

### UC14-S1

Test: `UC14_OnboardingCliente.S1_BozzaInOnboarding`

```gherkin
# language: it
Funzionalità: Onboarding guidato

Scenario: Il primo passo salva una bozza
  Quando l'amministratore compila l'anagrafica di "Alfa Srl" con codice SDI "m5uxcr1" e referente "Marta Russo" e passa al passo successivo
  Allora il cliente "Alfa Srl" esiste in stato "Onboarding" con codice SDI "M5UXCR1" e referente "Marta Russo"
  E lo storico contiene l'avvio dell'onboarding
```

### UC14-S2

Test: `UC14_OnboardingCliente.S2_AttivazioneDellaBozza`

```gherkin
# language: it
Funzionalità: Onboarding guidato

Scenario: Attivare la bozza alla fine
  Dato che "Alfa Srl" è una bozza
  Quando l'amministratore conclude l'onboarding
  Allora il cliente risulta "Attivo"
```

### UC14-S3

Test: `UC14_OnboardingCliente.S3_AnnullareLaBozza`

```gherkin
# language: it
Funzionalità: Onboarding guidato

Scenario: Annullare la bozza la elimina
  Dato che "Alfa Srl" è una bozza con il profilo "Operatore"
  Quando l'amministratore annulla l'onboarding
  Allora "Alfa Srl" non esiste più e la ragione sociale è di nuovo libera
```

### UC14-S4

Test: `UC14_OnboardingCliente.S4_ClienteAttivoNonSiElimina`

```gherkin
# language: it
Funzionalità: Onboarding guidato

Scenario: Un cliente già attivato non si elimina
  Dato che il cliente "Alfa Srl" è attivo
  Quando l'amministratore prova a eliminarlo
  Allora la richiesta è rifiutata come conflitto e il cliente esiste ancora
```

### UC14-S5

Test: `UC14_OnboardingCliente.S5_CodiceSdiNonValido`

```gherkin
# language: it
Funzionalità: Onboarding guidato

Scenario: Codice SDI non valido
  Quando l'amministratore compila l'anagrafica con codice SDI "ABC"
  Allora la richiesta è rifiutata con "InvalidFormat" sul codice SDI
```
