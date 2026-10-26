# UC31 – Entrare nella piattaforma come un utente

**Attore:** Amministratore
**Sistemi coinvolti:** Keycloak (token exchange)
**Epica:** EXP-12 · mock 1d "Impersona"
**Obiettivo:** vedere quello che vede un utente del cliente per aiutarlo, in modo tracciato e limitato nel tempo.

## Flusso principale
1. Dalla scheda del cliente l'amministratore sceglie "Impersona", un utente attivo del cliente, il motivo e la durata.
2. Il sistema chiede a Keycloak una sessione dell'utente (token exchange) e apre la piattaforma con quella sessione in una nuova scheda.
3. L'impersonificazione va nell'audit log e nello storico del cliente.

## Flussi alternativi
- **1a. Impersonificazione non configurata:** il pulsante è disattivato con il motivo; l'API risponde `409`.
- **1b. Utente non attivo (invitato, sospeso, disattivato) o inesistente:** non si impersona (`409` / `404`).
- **1c. Motivo mancante o troppo corto, durata fuori 5–60 minuti:** errore sul campo.
- **2a. Keycloak rifiuta lo scambio:** `503` con il motivo nel log.

## Regole
- R1. Motivo obbligatorio (almeno 10 caratteri), durata 5–60 minuti (default 30).
- R2. Il token non passa mai da URL interrogabili né dai log: la piattaforma lo riceve nel frammento (`#`) dell'indirizzo.
- R3. Ogni impersonificazione è registrata con amministratore, utente, motivo e durata.
- R4. Configurazione: `Impersonation:PlatformUrl` e `Impersonation:Audience` (client della piattaforma); in Keycloak token exchange abilitato e ruolo `impersonation` al service account.

## Scenari di accettazione

Test: [`UC31_Impersonificazione`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC31_Impersonificazione.cs)

### UC31-S1

Test: `UC31_Impersonificazione.S1_Impersonare`

```gherkin
# language: it
Funzionalità: Impersonificazione

Scenario: Impersonare un utente attivo
  Dato che "Mario Rossi" è un utente attivo di "Alfa Srl"
  Quando l'amministratore lo impersona per 15 minuti con motivo "Verifica segnalazione 4521"
  Allora riceve l'indirizzo della piattaforma con la sessione di "Mario Rossi" valida 15 minuti
  E lo storico di "Alfa Srl" registra impersonificazione, motivo e durata
```

### UC31-S2

Test: `UC31_Impersonificazione.S2_UtenteNonAttivo`

```gherkin
# language: it
Funzionalità: Impersonificazione

Scenario: Utente non attivo
  Dato che "Giulia Ferri" non ha ancora attivato l'account
  Quando l'amministratore prova a impersonarla
  Allora la richiesta è rifiutata come conflitto
```

### UC31-S3

Test: `UC31_Impersonificazione.S3_MotivoEDurata`

```gherkin
# language: it
Funzionalità: Impersonificazione

Scenario: Motivo e durata non validi
  Quando l'amministratore impersona senza motivo e per 2 ore
  Allora la richiesta è rifiutata con "Required" sul motivo e "InvalidValue" sulla durata
```

### UC31-S4

Test: `UC31_Impersonificazione.S4_NonConfigurata`

```gherkin
# language: it
Funzionalità: Impersonificazione

Scenario: Impersonificazione non configurata
  Dato che l'impersonificazione non è configurata
  Quando l'amministratore controlla se è disponibile e prova a impersonare
  Allora risulta non disponibile e la richiesta è rifiutata come conflitto
```
