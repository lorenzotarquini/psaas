# UC29 – Aprire la panoramica del giorno

**Attore:** Amministratore
**Epica:** mock 1b (sostituisce la Home "chi sono")
**Obiettivo:** sapere in un colpo d'occhio come sta la piattaforma e cosa fare oggi.

## Flusso principale
1. L'amministratore apre la Panoramica: saluto, data, quante cose ci sono da gestire e quante urgenti.
2. Vede gli indicatori: clienti attivi (e nuovi nel mese), in prova (e quante scadono entro 7 giorni), utenti (e creati nel mese), moduli installati (e quanti in beta).
3. Vede la coda "Da gestire" (UC28), filtrabile per area, con l'azione diretta per ogni voce: riprova il provisioning, apri la fattura, converti la prova, proponi l'upgrade, reinvia gli inviti.
4. Vede i servizi collegati (UC26) e l'attività recente (UC12).

## Flussi alternativi
- **2a. Keycloak non raggiungibile:** gli utenti restano vuoti; il resto si vede.
- **3a. Nulla da gestire:** la coda lo dice.

## Regole
- R1. "Questo mese" = dal primo del mese UTC; clienti attivi = stato Attivo; nuovi = attivi creati nel mese.
- R2. Inviti non completati: utenti con invito aperto da più di 5 giorni, in una sola voce per tutti i clienti (UC28); "Reinvia inviti" li reinvia tutti e conta i reinvii (UC19-R3).
- R3. Urgenti = voci critiche della coda.

## Scenari di accettazione

Test: [`UC29_Panoramica`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC29_Panoramica.cs)

### UC29-S1

Test: `UC29_Panoramica.S1_Indicatori`

```gherkin
# language: it
Funzionalità: Panoramica

Scenario: Gli indicatori
  Dato che "Alfa Srl" e "Beta Snc" sono attivi, "Gamma Spa" è in prova che finisce tra 3 giorni, ci sono 3 utenti e 2 moduli oltre al core di cui 1 in beta
  Quando l'amministratore apre la Panoramica
  Allora vede 2 clienti attivi (2 nel mese), 1 in prova (1 in scadenza), 3 utenti, 3 moduli (1 in beta)
```

### UC29-S2

Test: `UC29_Panoramica.S2_InvitiNonCompletati`

```gherkin
# language: it
Funzionalità: Panoramica

Scenario: Inviti non completati e reinvio
  Dato che "Giulia Ferri" e "Mario Rossi" hanno l'invito aperto da 6 giorni e "Anna Bianchi" da 1
  Quando l'amministratore apre la coda e sceglie "Reinvia inviti"
  Allora la coda dice 2 utenti, e Keycloak reinvia l'invito solo a loro due
```
