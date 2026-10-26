# UC26 – Controllare lo stato dei servizi collegati

**Attore:** Amministratore
**Epica:** EXP-09 · mock 1b "Servizi collegati"
**Obiettivo:** sapere subito se un problema del portale dipende da un sistema esterno.

## Flusso principale
1. L'amministratore apre lo stato dei servizi (Impostazioni e Panoramica).
2. Il sistema verifica ogni servizio collegato e mostra per ciascuno stato, tempo di risposta e dettaglio: database, Keycloak (accesso), API utenti di Keycloak, elenco dei comuni, hook dei moduli, trasmissione delle fatture.

## Flussi alternativi
- **2a. Un servizio non risponde entro 5 secondi:** risulta "Non raggiungibile" con il motivo; gli altri vengono verificati comunque.

## Regole
- R1. Stati: **Operativo**, **Degradato** (risponde ma con problemi, es. moduli in errore di provisioning), **Non raggiungibile**, **Non configurato** (es. SDI).
- R2. Ogni verifica ha un timeout di 5 secondi; la pagina non fallisce mai per un servizio.
- R3. Lo stato complessivo è il peggiore tra quelli dei servizi configurati.

## Scenari di accettazione

Test: [`UC26_StatoServizi`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC26_StatoServizi.cs)

### UC26-S1

Test: `UC26_StatoServizi.S1_TuttiOperativi`

```gherkin
# language: it
Funzionalità: Stato dei servizi

Scenario: Tutti i servizi rispondono
  Quando l'amministratore apre lo stato dei servizi
  Allora database, Keycloak, API utenti ed elenco comuni sono "Operativo" con il tempo di risposta
  E la trasmissione delle fatture è "Non configurato"
```

### UC26-S2

Test: `UC26_StatoServizi.S2_KeycloakGiu`

```gherkin
# language: it
Funzionalità: Stato dei servizi

Scenario: API utenti non raggiungibile
  Dato che l'API utenti di Keycloak non risponde
  Quando l'amministratore apre lo stato dei servizi
  Allora l'API utenti è "Non raggiungibile", lo stato complessivo pure, e il database resta "Operativo"
```

### UC26-S3

Test: `UC26_StatoServizi.S3_ModuliInErrore`

```gherkin
# language: it
Funzionalità: Stato dei servizi

Scenario: Moduli con provisioning fallito
  Dato che il provisioning di "warehouse" per "Alfa Srl" è fallito
  Quando l'amministratore apre lo stato dei servizi
  Allora gli hook dei moduli sono "Degradato" con 1 modulo in errore
```
