# UC6 – Creare una categoria al volo

**Attore:** Utente
**Obiettivo:** usare una categoria che non esiste ancora senza interrompere la registrazione del movimento.
**Use case di supporto a:** UC5

## Flusso principale
1. Nel campo Categoria di UC5 l'utente scrive; il sistema suggerisce le sue categorie dello stesso tipo del movimento (spesa o ricavo) che contengono il testo.
2. Nessuna corrisponde: il sistema offre "Crea «testo»".
3. L'utente sceglie "Crea".
4. Il sistema crea la categoria per quel tipo e la seleziona nel movimento, che resta compilato.

## Flussi alternativi
- **1a. Una categoria corrisponde:** l'utente la sceglie e non crea nulla.
- **4a. Nome non valido:** il sistema non crea nulla e segnala l'errore sul campo.
- **4b. Esiste già una categoria con lo stesso nome e tipo** (per esempio creata da un'altra scheda): il sistema non ne crea un'altra e seleziona quella esistente.

## Regole
- R1. Una categoria ha un nome (obbligatorio, max 40 caratteri) e un tipo (spesa o ricavo).
- R2. Il nome è unico tra le categorie dell'utente dello stesso tipo, ignorando maiuscole e spazi iniziali e finali: "Regali" può esistere sia come spesa sia come ricavo.
- R3. Le categorie sono personali; MySync non ne crea di predefinite nell'MVP.
- R4. La creazione è idempotente: chiedere una categoria che esiste già restituisce quella esistente (risposta 200 invece di 201), non un errore.
- R5. La categoria nasce anche se poi l'utente non salva il movimento.

## Scenari di accettazione

Test: [`UC6_CreareCategoria`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC6_CreareCategoria.cs)

### UC6-S1

Test: `UC6_CreareCategoria.S1_CategoriaCreata`

```gherkin
# language: it
Funzionalità: Creare una categoria al volo

Scenario: Nuova categoria di spesa
  Dato che "Mario" non ha la categoria di spesa "Palestra"
  Quando "Mario" crea la categoria di spesa "Palestra"
  Allora la categoria è creata
  E è suggerita quando "Mario" scrive "pal" in una spesa
  E non è suggerita quando scrive "pal" in un ricavo
```

### UC6-S2

Test: `UC6_CreareCategoria.S2_CategoriaGiaEsistente`

```gherkin
# language: it
Funzionalità: Creare una categoria al volo

Scenario: Categoria già esistente
  Dato che "Mario" ha la categoria di spesa "Palestra"
  Quando "Mario" crea la categoria di spesa " palestra "
  Allora riceve la categoria "Palestra" esistente
  E "Mario" ha ancora una sola categoria di spesa "Palestra"
```

### UC6-S3

Test: `UC6_CreareCategoria.S3_StessoNomeTipiDiversi`

```gherkin
# language: it
Funzionalità: Creare una categoria al volo

Scenario: Stesso nome per spesa e ricavo
  Dato che "Mario" ha la categoria di spesa "Regali"
  Quando "Mario" crea la categoria di ricavo "Regali"
  Allora la categoria di ricavo "Regali" è creata
```

### UC6-S4

Test: `UC6_CreareCategoria.S4_NomeNonValido`

```gherkin
# language: it
Funzionalità: Creare una categoria al volo

Schema dello scenario: Nome non valido
  Quando "Mario" crea una categoria di spesa con nome "<nome>"
  Allora la richiesta è rifiutata con "<codice>" sul nome

  Esempi:
    | nome           | codice            |
    |                | Required          |
    | 41 caratteri   | MaxLengthExceeded |
```

### UC6-S5

Test: [`UC6_CreareCategoria.S5_MovimentoConNuovaCategoria`](../../../src/MySync/FinHorizon.MySync.Frontend/src/features/edit-transaction/ui/UC6_CreareCategoria.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Creare una categoria al volo

Scenario: Il movimento resta compilato
  Dato che "Mario" sta registrando una spesa di 40,00 € da "Conto Intesa"
  Quando scrive "Palestra" nella categoria e sceglie "Crea «Palestra»"
  Allora la categoria "Palestra" è selezionata
  E importo e conto sono ancora 40,00 € e "Conto Intesa"
```
