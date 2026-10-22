# UC17 – Gestire la sottoscrizione di un cliente

**Attore:** Amministratore
**Epica:** EXP-03 · mock 1c, 1d, 1e
**Obiettivo:** assegnare a un cliente un piano (con prova e add-on) e i moduli che ne derivano.

## Flusso principale
1. Dalla scheda del cliente (o dal passo 2 dell'onboarding) l'amministratore sceglie piano, prova gratuita e moduli aggiuntivi.
2. Il sistema verifica i limiti del piano, salva la sottoscrizione e attiva i moduli: quelli del piano, gli add-on, le loro dipendenze e il core.
3. Ogni modulo nuovo viene preparato per il cliente tramite il suo hook di provisioning (UC18).

## Flussi alternativi
- **2a. Il cliente supera i limiti del piano** (utenti o unità produttive): il sistema rifiuta con `LimitExceeded` sul piano.
- **2b. Piano archiviato o inesistente, add-on inesistente:** `InvalidValue`.

## Regole
- R1. Una sottoscrizione per cliente; i valori del piano (prezzo, periodicità, limiti) vengono copiati e restano quelli anche se il piano cambia.
- R2. La prova vale solo per la prima sottoscrizione e se il piano prevede giorni di prova: stato Trial fino alla fine della prova.
- R3. Un cliente in onboarding con sottoscrizione in prova, all'attivazione, passa a Trial; una sottoscrizione pagata su un cliente in Trial lo porta ad Attivo.
- R4. Cambiare piano mantiene inizio e rinnovo; i moduli non più inclusi vengono tolti.
- R5. Un nuovo utente con profili di un cliente che ha raggiunto il limite di utenti è rifiutato (`LimitExceeded` sul profilo, UC6).

## Scenari di accettazione

Test: [`UC17_Sottoscrizione`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC17_Sottoscrizione.cs)

### UC17-S1

Test: `UC17_Sottoscrizione.S1_ModuliDelPianoConDipendenze`

```gherkin
# language: it
Funzionalità: Sottoscrizione del cliente

Scenario: I moduli del piano arrivano con le dipendenze
  Dato che "hr" richiede "warehouse" e il piano "pro" include "hr"
  Quando l'amministratore sottoscrive "pro" per "Alfa Srl"
  Allora "Alfa Srl" ha attivi "core", "hr" e "warehouse"
```

### UC17-S2

Test: `UC17_Sottoscrizione.S2_ProvaGratuita`

```gherkin
# language: it
Funzionalità: Sottoscrizione del cliente

Scenario: Prova gratuita
  Dato che il piano "pro" prevede 14 giorni di prova e "Alfa Srl" è in onboarding
  Quando l'amministratore sottoscrive "pro" in prova e attiva il cliente
  Allora la sottoscrizione è in "Trial" fino a 14 giorni da oggi e il cliente risulta "Trial"
```

### UC17-S3

Test: `UC17_Sottoscrizione.S3_LimiteUtentiDelPiano`

```gherkin
# language: it
Funzionalità: Sottoscrizione del cliente

Scenario: Il piano non basta per gli utenti del cliente
  Dato che "Alfa Srl" ha 2 utenti e il piano "base" ne ammette 1
  Quando l'amministratore sottoscrive "base"
  Allora la richiesta è rifiutata con "LimitExceeded" sul piano
```

### UC17-S4

Test: `UC17_Sottoscrizione.S4_PrezzoBloccato`

```gherkin
# language: it
Funzionalità: Sottoscrizione del cliente

Scenario: Il prezzo resta quello della sottoscrizione
  Dato che "Alfa Srl" ha sottoscritto "pro" a 99 €
  Quando l'amministratore porta "pro" a 129 €
  Allora la sottoscrizione di "Alfa Srl" resta a 99 €
```

### UC17-S5

Test: `UC17_Sottoscrizione.S5_CambioPianoTogliModuli`

```gherkin
# language: it
Funzionalità: Sottoscrizione del cliente

Scenario: Cambiando piano i moduli non inclusi vengono tolti
  Dato che "Alfa Srl" ha "pro" con "warehouse"
  Quando l'amministratore passa a "base" senza "warehouse"
  Allora "Alfa Srl" non ha più "warehouse"
```

### UC17-S6

Test: `UC17_Sottoscrizione.S6_NuovoUtenteOltreIlLimite`

```gherkin
# language: it
Funzionalità: Sottoscrizione del cliente

Scenario: Nuovo utente oltre il limite del piano
  Dato che "Alfa Srl" ha il piano "base" con 1 utente al massimo e già 1 utente
  Quando l'amministratore crea un utente con un profilo di "Alfa Srl"
  Allora la richiesta è rifiutata con "LimitExceeded" su quel profilo
```
