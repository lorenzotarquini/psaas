# UC15 – Indicare le spese fisse

**Attore:** Utente
**Obiettivo:** dire quali categorie di spesa sono fisse (affitto, bollette, abbonamenti), così la proiezione separa le spese fisse da quelle variabili.
**Use case di supporto a:** UC14

## Flusso principale
1. Nella proiezione (UC14) l'utente apre **"Quali spese sono fisse?"**.
2. Il sistema elenca le sue categorie di spesa, ognuna con un interruttore "Fissa".
3. L'utente accende o spegne un interruttore.
4. Il sistema salva subito la scelta e ricalcola le spese fisse e variabili proposte.

## Flussi alternativi
- **2a. Nessuna categoria di spesa:** il sistema spiega che le categorie nascono registrando le spese (UC5, UC6).
- **4a. Categoria di ricavo:** `InvalidValue` su `fixed`, perché solo le spese sono fisse o variabili.
- **4b. Categoria inesistente o di un altro utente:** `404`.

## Regole
- R1. Ogni categoria di spesa è **fissa** o **variabile**. Nasce variabile, anche quando è creata al volo (UC6), così la registrazione di un movimento non cambia.
- R2. Il tipo cambia solo i parametri proposti della proiezione. Movimenti, saldi e andamento non cambiano.
- R3. Se l'utente ha già salvato una proiezione (UC14-R11), i valori salvati restano com'erano: la nuova classificazione si vede nei parametri proposti e con "Riparti dai miei dati".
- R4. È l'unica modifica di una categoria nell'MVP: nome ed eliminazione restano fuori ambito.

## Scenari di accettazione

Test: [`UC15_SpeseFisse`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC15_SpeseFisse.cs)

### UC15-S1

Test: `UC15_SpeseFisse.S1_UnaCategoriaDiventaFissa`

```gherkin
# language: it
Funzionalità: Indicare le spese fisse

Scenario: Una categoria diventa fissa
  Dato i movimenti di UC14-S1, con "Affitto" ancora variabile
  E i parametri proposti sono spese fisse 0,00 € e spese variabili 1.300,00 €
  Quando "Mario" indica "Affitto" come fissa
  Allora i parametri proposti sono spese fisse 700,00 € e spese variabili 600,00 €
  E il saldo di "Conto Intesa" non è cambiato
```

### UC15-S2

Test: `UC15_SpeseFisse.S2_UnaNuovaCategoriaEVariabile`

```gherkin
# language: it
Funzionalità: Indicare le spese fisse

Scenario: Una categoria nuova è variabile
  Quando "Mario" crea la categoria di spesa "Palestra"
  Allora "Palestra" è variabile
```

### UC15-S3

Test: `UC15_SpeseFisse.S3_UnRicavoNonEFisso`

```gherkin
# language: it
Funzionalità: Indicare le spese fisse

Scenario: Una categoria di ricavo non può essere fissa
  Dato che "Mario" ha la categoria di ricavo "Stipendio"
  Quando "Mario" indica "Stipendio" come fissa
  Allora la richiesta è rifiutata con "InvalidValue" su "fixed"
```

### UC15-S4

Test: `UC15_SpeseFisse.S4_CategoriaDiUnAltroUtente`

```gherkin
# language: it
Funzionalità: Indicare le spese fisse

Scenario: Categoria di un altro utente
  Dato che "Anna" ha la categoria di spesa "Affitto"
  Quando "Mario" indica la categoria "Affitto" di "Anna" come fissa
  Allora la risposta è "non trovato"
  E la categoria di "Anna" è ancora variabile
```
