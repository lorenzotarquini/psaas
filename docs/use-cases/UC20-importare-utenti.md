# UC20 – Importare utenti da CSV

**Attore:** Amministratore
**Sistemi coinvolti:** Keycloak
**Epica:** EXP-06 · mock 1f "Importa CSV"
**Obiettivo:** creare molti utenti in una volta, vedendo prima quali righe sono valide.

## Flusso principale
1. L'amministratore sceglie un file CSV con le colonne `nome, cognome, email, dataNascita, profili`.
2. Il sistema mostra l'anteprima: per ogni riga i valori letti e gli errori, con gli stessi codici di UC6.
3. L'amministratore conferma: il sistema crea gli utenti delle righe valide (UC6) e riporta l'esito di ognuna.

## Flussi alternativi
- **2a. Nessuna riga valida:** la conferma non è possibile.
- **3a. Un'email risulta già usata al momento della creazione:** quella riga fallisce con `NotUnique`, le altre proseguono.

## Regole
- R1. `profili`: coppie `Cliente:Profilo` separate da `|`, con ragione sociale e nome del profilo come nel portale (senza distinzione di maiuscole). Una coppia che non esiste è un errore della riga (`profiles` = `InvalidValue`).
- R2. Un'email ripetuta nel file è un errore dalla seconda volta (`NotUnique`).
- R3. Massimo 500 righe per file.
- R4. Ogni utente creato va nell'audit log come in UC6.

## Scenari di accettazione

Test: [`UC20_ImportareUtenti`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC20_ImportareUtenti.cs)

### UC20-S1

Test: `UC20_ImportareUtenti.S1_AnteprimaConErrori`

```gherkin
# language: it
Funzionalità: Importare utenti da CSV

Scenario: Anteprima con righe non valide
  Dato un file con una riga valida, una con email non valida e una con il profilo "Alfa Srl:Nessuno"
  Quando l'amministratore lo carica
  Allora l'anteprima segna valida la prima riga, "InvalidFormat" sull'email della seconda e "InvalidValue" sui profili della terza
  E nessun utente è stato creato
```

### UC20-S2

Test: `UC20_ImportareUtenti.S2_ConfermaCreaLeRigheValide`

```gherkin
# language: it
Funzionalità: Importare utenti da CSV

Scenario: La conferma crea le righe valide
  Dato un file con due righe valide e una non valida
  Quando l'amministratore conferma l'importazione
  Allora vengono creati i due utenti con i loro profili e la riga non valida è riportata con il suo errore
```

### UC20-S3

Test: `UC20_ImportareUtenti.S3_EmailRipetutaNelFile`

```gherkin
# language: it
Funzionalità: Importare utenti da CSV

Scenario: Email ripetuta nel file
  Dato un file in cui "mario@example.com" compare due volte
  Quando l'amministratore lo carica
  Allora la seconda riga ha "NotUnique" sull'email
```
