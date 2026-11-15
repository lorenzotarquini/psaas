# UC11 – Gestire il proprio profilo

**Attore:** Utente
**Sistemi coinvolti:** Keycloak
**Obiettivo:** vedere e correggere i propri dati, reimpostare la password, scegliere lingua e tema.

## Flusso principale
1. Dal menu (ingranaggio) o cliccando il proprio nome in alto, l'utente apre **Impostazioni**.
2. Il sistema mostra nome, cognome, email e nome utente, letti dal token di Keycloak.
3. **Modificare i dati:** l'utente preme "Modifica i tuoi dati". MySync lo porta alla pagina di Keycloak per aggiornare il profilo; dopo il salvataggio Keycloak lo riporta in Impostazioni con i dati aggiornati.
4. **Reimpostare la password:** l'utente preme "Reimposta la password". MySync lo porta alla pagina di Keycloak per impostare la nuova password; poi torna in Impostazioni.
5. Nella stessa pagina sceglie lingua, tema chiaro o scuro e la valuta di visualizzazione (UC13); la valuta si salva subito in MySync e dashboard, andamento e totali si ricalcolano.

## Flussi alternativi
- **3a, 4a. L'utente annulla sulla pagina di Keycloak:** torna in MySync senza modifiche.

## Regole
- R1. I dati dell'utente vivono solo in Keycloak; MySync li legge dal token e non li salva. Fa eccezione la valuta di visualizzazione, una preferenza di MySync (UC13-R4).
- R2. Modifica dei dati e della password avvengono sulle pagine di Keycloak (*Application Initiated Actions* `UPDATE_PROFILE` e `UPDATE_PASSWORD` di keycloak-js): la password non passa mai da MySync e il backend non ha permessi di amministrazione sul realm.
- R3. Quali campi l'utente può cambiare (per esempio l'email) lo decide la configurazione del *User profile* del realm.

## Configurazione Keycloak (realm `PSAAS`)
- *Authentication → Required actions*: **Update Profile** e **Update Password** abilitate (lo sono di default).
- *Realm settings → User profile*: nome, cognome ed email modificabili dall'utente (`edit` per il ruolo `user`). L'email come username resta unica sulla piattaforma (Admin Portal, UC6-R2).

## Scenari di accettazione

Test: [`UC11_GestireIlProprioProfilo`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/settings/ui/UC11_GestireIlProprioProfilo.test.tsx) (frontend, Vitest)

### UC11-S1

Test: `UC11_GestireIlProprioProfilo.S1_DatiDalToken`

```gherkin
# language: it
Funzionalità: Gestire il proprio profilo

Scenario: I dati arrivano dal token
  Dato che "Mario Rossi" è entrato con l'email "mario.rossi@example.com"
  Quando apre Impostazioni
  Allora vede nome "Mario", cognome "Rossi", email e nome utente "mario.rossi@example.com"
```

### UC11-S2

Test: `UC11_GestireIlProprioProfilo.S2_ModificaDatiSuKeycloak`

```gherkin
# language: it
Funzionalità: Gestire il proprio profilo

Scenario: Modificare i dati
  Quando "Mario" preme "Modifica i tuoi dati"
  Allora viene portato alla pagina di Keycloak per aggiornare il profilo
```

### UC11-S3

Test: `UC11_GestireIlProprioProfilo.S3_ReimpostaPasswordSuKeycloak`

```gherkin
# language: it
Funzionalità: Gestire il proprio profilo

Scenario: Reimpostare la password
  Quando "Mario" preme "Reimposta la password"
  Allora viene portato alla pagina di Keycloak per impostare la nuova password
```
