# UC6 – Creare un utente

**Attore:** Amministratore
**Sistemi coinvolti:** Keycloak
**Obiettivo:** dare accesso a PSAAS a una persona.

## Flusso principale
1. L'amministratore inserisce nome, cognome, email e data di nascita della persona.
2. Sceglie zero o più profili, anche di clienti diversi.
3. Il sistema verifica i dati e crea l'utente in Keycloak.
4. Keycloak invia alla persona un'email per impostare la password.
5. Il sistema mostra l'utente creato.

## Flussi alternativi
- **3a. Dati non validi:** il sistema non crea nulla e indica, campo per campo, cosa correggere.
- **3b. Email già usata da un altro utente:** il sistema non crea nulla e segnala il duplicato sull'email.
- **3c. Un profilo scelto non esiste:** il sistema non crea nulla e segnala il profilo non valido.
- **3d. Keycloak non è raggiungibile:** il sistema non crea nulla e segnala che il servizio non è disponibile.

## Regole
- R1. Nome, cognome, email e data di nascita sono obbligatori.
- R2. L'email è anche lo username ed è unica su tutta la piattaforma.
- R3. Nessun vincolo sulla data di nascita oltre a essere una data valida.
- R4. I profili sono facoltativi; un utente può avere profili di clienti diversi.
- R5. In Keycloak si salvano solo i profili dell'utente (attributo `profileIds`); i suoi clienti si ricavano dai profili.
- R6. La password non passa mai dall'Admin Portal.

## Scenari di accettazione

Test: [`UC6_CreareUtente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC6_CreareUtente.cs)

### UC6-S1

Test: `UC6_CreareUtente.S1_UtenteConProfiliDiClientiDiversi`

```gherkin
# language: it
Funzionalità: Creare un utente

Scenario: Utente con profili di clienti diversi
  Dato che il cliente "Alfa Srl" ha il profilo "Operatore" e il cliente "Beta Snc" ha il profilo "Revisore"
  Quando l'amministratore crea "Mario Rossi", email "mario.rossi@example.com", nato il "1980-05-17", con i profili "Operatore" e "Revisore"
  Allora l'utente è creato con username "mario.rossi@example.com"
  E risulta con il profilo "Operatore" di "Alfa Srl" e "Revisore" di "Beta Snc"
  E Keycloak ha inviato a "mario.rossi@example.com" l'email per impostare la password
```

### UC6-S2

Test: `UC6_CreareUtente.S2_UtenteSenzaProfili`

```gherkin
# language: it
Funzionalità: Creare un utente

Scenario: Utente senza profili
  Quando l'amministratore crea "Anna Bianchi" senza profili
  Allora l'utente è creato senza profili
```

### UC6-S3

Test: `UC6_CreareUtente.S3_DatiObbligatoriMancanti`

```gherkin
# language: it
Funzionalità: Creare un utente

Scenario: Dati obbligatori mancanti
  Quando l'amministratore crea un utente senza compilare nessun campo
  Allora la richiesta è rifiutata con "Required" su nome, cognome, email e data di nascita
  E nessun utente è stato creato in Keycloak
```

### UC6-S4

Test: `UC6_CreareUtente.S4_EmailNonValida`

```gherkin
# language: it
Funzionalità: Creare un utente

Scenario: Email non valida
  Quando l'amministratore crea un utente con email "non-una-email"
  Allora la richiesta è rifiutata con "InvalidFormat" sull'email
```

### UC6-S5

Test: `UC6_CreareUtente.S5_EmailGiaUsata`

```gherkin
# language: it
Funzionalità: Creare un utente

Scenario: Email già usata da un altro utente
  Dato che esiste l'utente con email "mario.rossi@example.com"
  Quando l'amministratore crea un utente con email "MARIO.ROSSI@example.com"
  Allora la richiesta è rifiutata con "NotUnique" sull'email
  E esiste ancora un solo utente con quell'email
```

### UC6-S6

Test: `UC6_CreareUtente.S6_ProfiloInesistente`

```gherkin
# language: it
Funzionalità: Creare un utente

Scenario: Profilo inesistente
  Quando l'amministratore crea un utente con un profilo che non esiste
  Allora la richiesta è rifiutata con "InvalidValue" su quel profilo
  E nessun utente è stato creato in Keycloak
```

### UC6-S7

Test: `UC6_CreareUtente.S7_KeycloakNonRaggiungibile`

```gherkin
# language: it
Funzionalità: Creare un utente

Scenario: Keycloak non raggiungibile
  Dato che Keycloak non risponde
  Quando l'amministratore crea un utente valido
  Allora il sistema segnala che il servizio non è disponibile
```
