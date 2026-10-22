# UC19 – Gestire un utente

**Attore:** Amministratore
**Sistemi coinvolti:** Keycloak
**Epica:** EXP-06 · mock 1f
**Obiettivo:** vedere lo stato di un utente e intervenire: dati, profili, invito, password, MFA, disattivazione.

## Flusso principale
1. Dalla vista utenti (UC7) l'amministratore apre un utente nel pannello laterale.
2. Vede stato, ultimo accesso, profili raggruppati per cliente e dettagli (ID Keycloak, data di nascita, creato il/da, inviti inviati).
3. Modifica nome, cognome, data di nascita, profili (anche di altri clienti) e l'obbligo di MFA, e salva.
4. Oppure reinvia l'invito, chiede il reset della password, disattiva o riattiva l'utente.

## Flussi alternativi
- **3a. Dati non validi, profilo inesistente o limite utenti del piano raggiunto per un nuovo cliente:** stessi codici di UC6, niente viene salvato.
- **4a. Azione non consentita nello stato attuale** (reinvio a chi ha già attivato l'account, reset a chi è disattivato o deve ancora attivarsi): il sistema rifiuta con `409`.
- **Utente inesistente:** `404`. **Keycloak non raggiungibile:** `503`.

## Regole
- R1. Stati: **Invito inviato** (non ha ancora impostato la password), **Attivo**, **Sospeso** (attivo in Keycloak ma tutti i clienti dei suoi profili sono sospesi o cessati), **Disattivato** (non può accedere).
- R2. L'email è lo username e non si modifica.
- R3. Reinvio invito e reset password fanno inviare a Keycloak l'email con il link: la password non passa mai dal portale. Il reinvio conta gli inviti e ricorda l'ultimo.
- R4. MFA richiesta: Keycloak chiede di configurare l'OTP al prossimo accesso.
- R5. L'ultimo accesso viene dagli eventi di login di Keycloak; se non disponibili resta vuoto senza bloccare.
- R6. Ogni modifica e azione va nell'audit log (UC12), nello storico di ogni cliente dei profili dell'utente.

## Scenari di accettazione

Test: [`UC19_GestireUtente`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC19_GestireUtente.cs)

### UC19-S1

Test: `UC19_GestireUtente.S1_DettaglioConStatoEInvito`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Dettaglio di un utente appena creato
  Dato che "Giulia Ferri" è stata creata dall'amministratore "admin.psaas" e non ha ancora impostato la password
  Quando l'amministratore apre "Giulia Ferri"
  Allora vede lo stato "Invito inviato", 1 invito inviato e "creata da admin.psaas"
```

### UC19-S2

Test: `UC19_GestireUtente.S2_ModificaDatiEProfili`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Modificare dati e profili
  Dato che "Mario Rossi" ha il profilo "Operatore" di "Alfa Srl"
  Quando l'amministratore gli dà anche "Revisore" di "Beta Snc", cambia il cognome in "Russo" e richiede la MFA
  Allora "Mario Russo" ha i due profili, la MFA richiesta e lo storico di entrambi i clienti lo registra
```

### UC19-S3

Test: `UC19_GestireUtente.S3_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Dati non validi
  Quando l'amministratore salva "Mario Rossi" senza nome e con un profilo inesistente
  Allora la richiesta è rifiutata con "Required" sul nome e "InvalidValue" sul profilo
```

### UC19-S4

Test: `UC19_GestireUtente.S4_ReinvioInvito`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Reinviare l'invito
  Dato che "Giulia Ferri" ha l'invito in sospeso
  Quando l'amministratore reinvia l'invito
  Allora Keycloak invia di nuovo l'email e gli inviti risultano 2
```

### UC19-S5

Test: `UC19_GestireUtente.S5_AzioneNonConsentita`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Azione non consentita nello stato
  Dato che "Mario Rossi" ha già attivato l'account
  Quando l'amministratore prova a reinviargli l'invito
  Allora la richiesta è rifiutata come conflitto
```

### UC19-S6

Test: `UC19_GestireUtente.S6_DisattivaERiattiva`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Disattivare e riattivare
  Quando l'amministratore disattiva "Mario Rossi"
  Allora "Mario Rossi" risulta "Disattivato" e il reset della password è rifiutato
  E dopo la riattivazione risulta di nuovo "Attivo"
```

### UC19-S7

Test: `UC19_GestireUtente.S7_SospesoConIlCliente`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Sospeso con il cliente
  Dato che "Mario Rossi" ha solo profili di "Alfa Srl"
  Quando "Alfa Srl" viene sospeso
  Allora "Mario Rossi" risulta "Sospeso"
```

### UC19-S8

Test: `UC19_GestireUtente.S8_ResetPassword`

```gherkin
# language: it
Funzionalità: Gestire un utente

Scenario: Reset della password
  Dato che "Mario Rossi" è attivo
  Quando l'amministratore chiede il reset della password
  Allora Keycloak invia l'email per impostare una nuova password e lo storico lo registra
```
