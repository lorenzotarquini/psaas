# UC27 – Inviare una comunicazione ai clienti

**Attore:** Amministratore
**Epica:** EXP-11 · mock 1c "Invia comunicazione"
**Obiettivo:** avvisare un gruppo di clienti (manutenzioni, novità, scadenze) dal portale, senza strumenti esterni.

## Flusso principale
1. L'amministratore scrive oggetto e testo e sceglie i canali: annuncio nella piattaforma, email, o entrambi.
2. Sceglie i destinatari: per stato del cliente, piano, modulo attivo, oppure i clienti selezionati nell'elenco (mock 1c).
3. Il sistema mostra quanti clienti riceveranno la comunicazione e i primi nomi.
4. L'amministratore invia: il sistema registra la comunicazione, invia le email alla PEC di ciascun cliente e registra l'esito per cliente.
5. La comunicazione compare nell'elenco delle comunicazioni e nello storico di ogni cliente raggiunto.

## Flussi alternativi
- **1a. Oggetto o testo mancanti, nessun canale, nessun destinatario:** il sistema non invia nulla e indica il campo.
- **4a. Email non configurata (SMTP):** l'annuncio viene registrato, le email risultano "Non inviata" con il motivo.
- **4b. Un'email viene rifiutata:** quel cliente risulta "Errore" con il motivo, gli altri proseguono.

## Regole
- R1. Filtri diversi in AND, valori dello stesso filtro in OR; una selezione esplicita ignora gli altri filtri. I clienti cessati non ricevono mai comunicazioni.
- R2. Oggetto fino a 150 caratteri, testo fino a 5000; massimo 1000 destinatari per invio.
- R3. L'annuncio nella piattaforma resta visibile fino alla data indicata (facoltativa, al massimo 90 giorni).
- R4. L'email va alla PEC del cliente; con le comunicazioni partono anche i solleciti delle fatture scadute (UC24-R3).
- R5. Ogni invio va nell'audit log, una riga per cliente raggiunto.

## Scenari di accettazione

Test: [`UC27_Comunicazioni`](../../src/tests/PSAAS.AdminPortal.Backend.Tests/UC27_Comunicazioni.cs)

### UC27-S1

Test: `UC27_Comunicazioni.S1_AnteprimaDestinatari`

```gherkin
# language: it
Funzionalità: Comunicazioni ai clienti

Scenario: Anteprima dei destinatari
  Dato che "Alfa Srl" e "Beta Snc" hanno il piano "pro", "Gamma Spa" il piano "base" e "Delta Srl" è cessata con "pro"
  Quando l'amministratore sceglie i clienti del piano "pro"
  Allora l'anteprima conta 2 clienti, "Alfa Srl" e "Beta Snc"
```

### UC27-S2

Test: `UC27_Comunicazioni.S2_InvioEmailEAnnuncio`

```gherkin
# language: it
Funzionalità: Comunicazioni ai clienti

Scenario: Invio di email e annuncio
  Dato che "Alfa Srl" e "Beta Snc" sono attivi
  Quando l'amministratore invia "Manutenzione sabato" come annuncio ed email ai clienti attivi
  Allora le due PEC ricevono l'email, la comunicazione ha 2 destinatari "Inviata"
  E lo storico di "Alfa Srl" la registra
```

### UC27-S3

Test: `UC27_Comunicazioni.S3_SelezioneEsplicita`

```gherkin
# language: it
Funzionalità: Comunicazioni ai clienti

Scenario: Clienti selezionati nell'elenco
  Dato che ci sono "Alfa Srl", "Beta Snc" e "Gamma Spa"
  Quando l'amministratore invia a "Alfa Srl" e "Gamma Spa" selezionati
  Allora solo loro ricevono la comunicazione
```

### UC27-S4

Test: `UC27_Comunicazioni.S4_EmailRifiutata`

```gherkin
# language: it
Funzionalità: Comunicazioni ai clienti

Scenario: Email rifiutata per un cliente
  Dato che la casella di "Beta Snc" rifiuta le email
  Quando l'amministratore invia un'email ai clienti attivi
  Allora "Alfa Srl" risulta "Inviata" e "Beta Snc" "Errore" con il motivo
```

### UC27-S5

Test: `UC27_Comunicazioni.S5_DatiNonValidi`

```gherkin
# language: it
Funzionalità: Comunicazioni ai clienti

Scenario: Comunicazione incompleta
  Quando l'amministratore invia senza oggetto, senza canali e a un piano che nessuno ha
  Allora la richiesta è rifiutata con "Required" su oggetto e canali e "InvalidValue" sui destinatari
```

### UC27-S6

Test: `UC27_Comunicazioni.S6_SollecitoViaEmail`

```gherkin
# language: it
Funzionalità: Comunicazioni ai clienti

Scenario: Il sollecito parte per email
  Dato che la fattura di "Alfa Srl" è scaduta
  Quando gira la fatturazione giornaliera
  Allora la PEC di "Alfa Srl" riceve il sollecito con il numero della fattura
```
