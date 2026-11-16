# UC32 – Assegnare un modulo a un utente

**Attore:** Amministratore
**Sistemi coinvolti:** Keycloak, il modulo (oggi MySync)
**Obiettivo:** decidere chi può entrare in un modulo, utente per utente. Il primo modulo gestito così è MySync, che l'utente attiva pagando.

## Flusso principale

1. L'amministratore apre un utente (UC19) e trova la sezione "Moduli" con i moduli del catalogo.
2. Attiva l'interruttore di MySync.
3. Il sistema dà all'utente il ruolo di realm Keycloak `module-mysync`, creandolo se ancora non esiste, e registra l'operazione nello storico.
4. Al login successivo l'utente entra in MySync.

## Flussi alternativi

- **2a. L'amministratore toglie MySync:** il sistema toglie il ruolo e chiude tutte le sessioni Keycloak dell'utente. Al primo rinnovo del token MySync lo rimanda al login, e dopo il login l'utente vede "Non hai accesso a MySync" e non entra.
- **3a. Modulo sconosciuto o deprecato:** la richiesta è rifiutata e nulla cambia.
- **3b. Keycloak non risponde, o il service account del portale non può creare il ruolo (manca `manage-realm`):** il sistema segnala che il servizio non è disponibile e nulla cambia.

## Regole

- R1. Si assegna solo un modulo del catalogo non deprecato. Un modulo deprecato si può ancora togliere.
- R2. Il modulo di un utente è il ruolo di realm `module-<id del modulo>` (es. `module-mysync`). Il portale crea il ruolo in Keycloak la prima volta che assegna il modulo a qualcuno: non serve crearlo a mano.
- R3. Togliere un modulo chiude le sessioni dell'utente: i token già emessi restano validi solo fino alla loro scadenza (di norma 5 minuti).
- R4. L'assegnazione è per utente, indipendente dalla sottoscrizione del cliente (UC17).
- R5. Assegnazioni e rimozioni vanno nello storico dei clienti dell'utente (UC12): `user.module.granted`, `user.module.revoked`.
- R6. MySync accetta solo i token con il ruolo `module-mysync`. Il ruolo `enduser` del gruppo `end_users` da solo non basta.

## Scenari di accettazione

Test: [`UC32_ModuliUtente`](../../src/AdminPortal/Tests/FinHorizon.AdminPortal.Backend.Tests/UC32_ModuliUtente.cs)

### UC32-S1

Test: `UC32_ModuliUtente.S1_AssegnareMySync`

```gherkin
# language: it
Funzionalità: Moduli dell'utente

Scenario: Assegnare MySync
  Dato che "Mario Rossi" è un utente di "Alfa Srl"
  Quando l'amministratore gli assegna "mysync"
  Allora Mario ha il ruolo Keycloak "module-mysync"
  E lo storico di "Alfa Srl" registra l'assegnazione
```

### UC32-S2

Test: `UC32_ModuliUtente.S2_TogliereMySyncChiudeLeSessioni`

```gherkin
# language: it
Funzionalità: Moduli dell'utente

Scenario: Togliere MySync
  Dato che "Mario Rossi" ha "mysync"
  Quando l'amministratore glielo toglie
  Allora Mario non ha più moduli
  E le sue sessioni Keycloak sono chiuse
```

### UC32-S3

Test: `UC32_ModuliUtente.S3_ModuloSconosciutoODeprecato`

```gherkin
# language: it
Funzionalità: Moduli dell'utente

Scenario: Modulo che non si può assegnare
  Quando l'amministratore assegna a Mario un modulo che non esiste o è deprecato
  Allora la richiesta è rifiutata sul campo "moduleId" e Mario non riceve nulla
```

### UC32-S4

Test: `UC32_ModuliUtente.S4_UtenteInesistente`

```gherkin
# language: it
Funzionalità: Moduli dell'utente

Scenario: Utente che non esiste
  Quando l'amministratore legge, assegna o toglie un modulo a un utente che non esiste
  Allora riceve "non trovato"
```

### UC32-S5

Test: `UC32_ModuliUtente.S5_KeycloakNonRaggiungibile`

```gherkin
# language: it
Funzionalità: Moduli dell'utente

Scenario: Keycloak non raggiungibile
  Dato che Keycloak non risponde
  Quando l'amministratore assegna "mysync" a Mario
  Allora il sistema segnala che il servizio non è disponibile
```

Lato MySync: [GEN-S3](../mysync/use-cases/README.md) verifica che senza `module-mysync` ogni operazione risponda 403.
