# UC8 – Sapere chi sono

**Attore:** Amministratore
**Obiettivo:** vedere con quale identità e quali ruoli si sta usando il portale.

## Flusso principale
1. Il portale chiede chi è l'amministratore collegato.
2. Il sistema restituisce identificativo Keycloak, nome, email e ruoli, letti dal token di accesso.

## Flussi alternativi
- **1a. Nessun token, token scaduto o emesso per un'altra applicazione:** accesso negato (non autenticato).
- **1b. Token valido ma senza ruolo `Administrator`:** accesso negato (non autorizzato).

## Regole
- R1. Le informazioni vengono solo dal token: nessun dato dell'amministratore è salvato in PSAAS.

## Scenari di accettazione

Test: [`UC8_ChiSono`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC8_ChiSono.cs)

### UC8-S1

Test: `UC8_ChiSono.S1_IdentitaLettaDalToken`

```gherkin
# language: it
Funzionalità: Sapere chi sono

Scenario: Identità letta dal token
  Dato che l'amministratore è autenticato come "admin.psaas" con email "admin@psaas.it" e ruolo "Administrator"
  Quando chiede chi è
  Allora vede il suo identificativo Keycloak, "admin.psaas", "admin@psaas.it" e il ruolo "Administrator"
```

### UC8-S2

Test: `UC8_ChiSono.S2_SenzaToken`

```gherkin
# language: it
Funzionalità: Sapere chi sono

Scenario: Nessun token
  Quando chiede chi è senza essere autenticato
  Allora l'accesso è negato come non autenticato
```

### UC8-S3

Test: `UC8_ChiSono.S3_TokenScaduto`

```gherkin
# language: it
Funzionalità: Sapere chi sono

Scenario: Token scaduto
  Quando chiede chi è con un token scaduto
  Allora l'accesso è negato come non autenticato
```

### UC8-S4

Test: `UC8_ChiSono.S4_TokenPerUnAltraApplicazione`

```gherkin
# language: it
Funzionalità: Sapere chi sono

Scenario: Token emesso per un'altra applicazione
  Quando chiede chi è con un token la cui audience non è "adminportal-api"
  Allora l'accesso è negato come non autenticato
```

### UC8-S5

Test: `UC8_ChiSono.S5_SenzaRuoloAdministrator`

```gherkin
# language: it
Funzionalità: Sapere chi sono

Scenario: Utente autenticato senza ruolo Administrator
  Quando chiede chi è con un token valido ma senza ruolo "Administrator"
  Allora l'accesso è negato come non autorizzato
```
