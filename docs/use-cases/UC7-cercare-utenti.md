# UC7 – Cercare gli utenti

**Attore:** Amministratore
**Sistemi coinvolti:** Keycloak
**Obiettivo:** trovare un utente nella vista utenti del portale.

## Flusso principale
1. L'amministratore apre la vista utenti e scrive un nome o un'email; può filtrare per cliente, profilo e stato (mock 1f).
2. Il sistema mostra gli utenti corrispondenti, di qualunque cliente, a pagine, con i loro profili e i clienti a cui quei profili appartengono, lo stato (UC19-R1) e l'ultimo accesso.

## Flussi alternativi
- **2a. Nessun utente corrisponde:** il sistema mostra un elenco vuoto.
- **2b. Keycloak non è raggiungibile:** il sistema segnala che il servizio non è disponibile.

## Regole
- R1. La ricerca è globale, non limitata a un cliente.
- R2. Cerca in nome, cognome ed email, senza distinzione di maiuscole.
- R3. Profili che non esistono più (cancellati con UC5) non vengono mostrati.
- R4. I filtri si combinano con la ricerca; il totale conta tutti gli utenti che corrispondono, non solo la pagina.
- R5. Ordine: cognome, nome.

## Scenari di accettazione

Test: [`UC7_CercareUtenti`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC7_CercareUtenti.cs)

### UC7-S1

Test: `UC7_CercareUtenti.S1_RicercaPerNomeConProfiliEClienti`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: Ricerca per nome con profili e clienti
  Dato che l'utente "Mario Rossi" ha il profilo "Operatore" del cliente "Alfa Srl"
  E esiste l'utente "Anna Bianchi"
  Quando l'amministratore cerca "mario"
  Allora vede solo "Mario Rossi" con il profilo "Operatore" del cliente "Alfa Srl"
```

### UC7-S2

Test: `UC7_CercareUtenti.S2_RicercaPerEmailSenzaMaiuscole`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: Ricerca per email senza distinzione di maiuscole
  Dato che esiste l'utente con email "anna.bianchi@example.com"
  Quando l'amministratore cerca "ANNA.BIANCHI@"
  Allora vede l'utente "Anna Bianchi"
```

### UC7-S3

Test: `UC7_CercareUtenti.S3_RicercaGlobaleSuTuttiIClienti`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: La ricerca è globale su tutti i clienti
  Dato che "Mario Rossi" ha un profilo di "Alfa Srl" e "Mario Verdi" un profilo di "Beta Snc"
  Quando l'amministratore cerca "mario"
  Allora vede sia "Mario Rossi" sia "Mario Verdi"
```

### UC7-S4

Test: `UC7_CercareUtenti.S4_ProfiliCancellatiNonMostrati`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: I profili che non esistono più non vengono mostrati
  Dato che l'utente "Mario Rossi" ha in Keycloak un profilo esistente e uno cancellato
  Quando l'amministratore cerca "mario"
  Allora vede solo il profilo esistente
```

### UC7-S5

Test: `UC7_CercareUtenti.S5_NessunaCorrispondenza`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: Nessun utente corrisponde
  Dato che esiste l'utente "Anna Bianchi"
  Quando l'amministratore cerca "zeta"
  Allora vede un elenco vuoto
```

### UC7-S6

Test: `UC7_CercareUtenti.S6_KeycloakNonRaggiungibile`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: Keycloak non raggiungibile
  Dato che Keycloak non risponde
  Quando l'amministratore cerca "mario"
  Allora il sistema segnala che il servizio non è disponibile
```

### UC7-S7

Test: `UC7_CercareUtenti.S7_FiltriPerClienteEStato`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: Filtrare per cliente e stato
  Dato che "Mario Rossi" e "Anna Bianchi" hanno profili di "Alfa Srl", "Luca Verdi" di "Beta Snc", e "Anna Bianchi" è disattivata
  Quando l'amministratore filtra per "Alfa Srl" e stato "Attivo"
  Allora vede solo "Mario Rossi", su un totale di 1
```

### UC7-S8

Test: `UC7_CercareUtenti.S8_PagineEOrdine`

```gherkin
# language: it
Funzionalità: Cercare gli utenti

Scenario: Pagine e ordine
  Dato che esistono 3 utenti
  Quando l'amministratore chiede la pagina 2 da 2
  Allora vede il terzo utente per cognome e il totale 3
```
