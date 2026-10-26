# UC30 – Cercare in tutta la console

**Attore:** Amministratore
**Epica:** mock 1b–1f (barra ⌘K)
**Obiettivo:** arrivare a un cliente o a un utente da qualunque pagina, senza passare dagli elenchi.

## Flusso principale
1. L'amministratore preme ⌘K (Ctrl+K) o clicca la barra di ricerca in alto e scrive almeno 2 caratteri.
2. Il sistema mostra i clienti (ragione sociale, P.IVA, PEC) e gli utenti (nome, cognome, email) che corrispondono, e le azioni rapide.
3. Con frecce e Invio, o con il mouse, apre il cliente o l'utente.

## Flussi alternativi
- **2a. Keycloak non raggiungibile:** i clienti si vedono, gli utenti no, con l'avviso.
- **1a. Meno di 2 caratteri:** solo le azioni rapide.

## Regole
- R1. Al massimo 5 clienti e 5 utenti, con le stesse regole di UC1 e UC7.
- R2. Scorciatoie: ⌘K/Ctrl+K apre la ricerca, Esc chiude, N apre il nuovo cliente (quando non si sta scrivendo in un campo).

## Scenari di accettazione

Test: [`UC30_RicercaGlobale`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC30_RicercaGlobale.cs)

### UC30-S1

Test: `UC30_RicercaGlobale.S1_ClientiEUtenti`

```gherkin
# language: it
Funzionalità: Ricerca globale

Scenario: Clienti e utenti nella stessa ricerca
  Dato il cliente "Ferri Costruzioni Srl" e l'utente "Giulia Ferri"
  Quando l'amministratore cerca "ferri"
  Allora vede il cliente e l'utente
```

### UC30-S2

Test: `UC30_RicercaGlobale.S2_PerPartitaIva`

```gherkin
# language: it
Funzionalità: Ricerca globale

Scenario: Ricerca per P.IVA
  Dato il cliente "Alfa Srl" con P.IVA "IT12345678901"
  Quando l'amministratore cerca "12345678"
  Allora vede "Alfa Srl"
```

### UC30-S3

Test: `UC30_RicercaGlobale.S3_KeycloakGiu`

```gherkin
# language: it
Funzionalità: Ricerca globale

Scenario: Utenti non disponibili
  Dato che Keycloak non risponde
  Quando l'amministratore cerca "alfa"
  Allora vede i clienti e l'indicazione che gli utenti non sono disponibili
```

### UC30-S4

Test: `UC30_RicercaGlobale.S4_TroppoCorto`

```gherkin
# language: it
Funzionalità: Ricerca globale

Scenario: Testo troppo corto
  Quando l'amministratore cerca "a"
  Allora la richiesta è rifiutata con "InvalidValue" sul testo
```
