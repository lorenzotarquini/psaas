# UC10 – Modificare conti e debiti

**Attore:** Utente
**Obiettivo:** correggere o aggiornare i propri conti e debiti, e aggiungerne di nuovi entro i limiti, da un'unica sezione.

## Flusso principale
1. Dal menu l'utente apre **Conti e debiti**.
2. Il sistema mostra due elenchi:
   - **conti**, con tipo, tag, data di apertura, saldo iniziale e saldo attuale;
   - **debiti**, con tipo, tag, conto collegato, residuo iniziale, residuo attuale (o "Estinto") e rata mensile.
3. Per modificare un conto l'utente preme la matita, cambia i campi e preme "Salva".
4. Il sistema avverte che **il patrimonio attuale e il suo andamento verranno ricalcolati** e chiede se procedere.
5. L'utente conferma; il sistema salva e aggiorna saldi, patrimonio e andamento.
6. Per modificare un debito l'utente preme la matita, cambia i campi e salva (nessun avviso: i debiti non entrano nel patrimonio, D9).
7. Con "Nuovo conto" e "Nuovo debito" l'utente ne aggiunge altri (UC2, UC3).

## Flussi alternativi
- **4a. L'utente annulla:** nulla cambia e il modulo resta aperto con le sue modifiche.
- **5a. Nome già usato da un altro conto (o debito) dell'utente:** `NotUnique` sul nome; il record stesso non conta.
- **6a. Residuo iniziale sotto le rate già pagate:** `InvalidValue` sul residuo.
- **6b. Conto collegato inesistente o di un altro utente:** `InvalidValue` sul conto.
- **7a. Limite raggiunto:** il pulsante "Nuovo conto" (o "Nuovo debito") resta visibile ma **disabilitato**, con il messaggio che spiega il limite. Lo stesso vale nelle card della dashboard.
- **\*a. Conto o debito inesistente o di un altro utente:** `404`.

## Regole
- R1. Di un conto si modificano tipo, nome, descrizione, tag e saldo iniziale; la data di apertura no, perché i movimenti dipendono da lei (D3).
- R2. Modificare un conto chiede conferma, perché saldo, patrimonio netto e andamento si ricalcolano dal nuovo saldo iniziale, anche nel passato.
- R3. Di un debito si modificano tipo, nome, descrizione, tag, conto collegato, residuo iniziale e rata mensile (UC3-R8, che si può anche togliere); la data di creazione no.
- R4. Il residuo iniziale di un debito non può scendere sotto quanto già pagato con le rate, altrimenti il residuo diventerebbe negativo. Se è uguale, il debito risulta estinto.
- R5. Nomi unici per utente come alla creazione (UC2-R2, UC3-R2), escluso il record che si sta modificando.
- R6. I pulsanti di aggiunta sono disabilitati quando il numero di conti (o debiti) ha raggiunto `MySync:MaxAccounts` (o `MySync:MaxDebts`); il backend rifiuta comunque oltre il limite (`LimitReached`).

## Scenari di accettazione

Test: [`UC10_ModificareContiEDebiti`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC10_ModificareContiEDebiti.cs)

### UC10-S1

Test: `UC10_ModificareContiEDebiti.S1_ModificaContoRicalcolaIlPatrimonio`

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Scenario: Modificare un conto ricalcola il patrimonio
  Dato che "Mario" ha aperto il 2026-11-10 il conto "Credem" con saldo iniziale 6.000,00 €
  E il 2026-11-12 ha speso 100,00 € da "Credem"
  Quando cambia il conto in "Credem online", banca virtuale, saldo iniziale 7.600,00 €
  Allora il saldo di "Credem online" è 7.500,00 € e la data di apertura resta il 2026-11-10
  E il patrimonio netto è 7.500,00 €
  E l'andamento parte da 7.600,00 €
```

### UC10-S2

Test: `UC10_ModificareContiEDebiti.S2_NomeGiaUsatoDaUnAltroConto`

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Scenario: Nome già usato da un altro conto
  Dato che "Mario" ha i conti "Credem" e "Revolut"
  Quando rinomina "Credem" in "revolut"
  Allora la richiesta è rifiutata con "NotUnique" sul nome
```

### UC10-S3

Test: `UC10_ModificareContiEDebiti.S3_ModificaDebito`

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Scenario: Modificare un debito
  Dato che "Mario" ha il mutuo "Mutuo Prima Casa" di 170,00 € collegato a "Credem"
  Quando lo rinomina "Mutuo casa", lo collega a "Revolut" e porta il residuo a 200,00 €
  Allora i debiti residui sono 200,00 €
```

### UC10-S4

Test: `UC10_ModificareContiEDebiti.S4_ResiduoSottoIlGiaPagato`

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Scenario: Residuo sotto il già pagato
  Dato che "Mario" ha il finanziamento "Auto" di 300,00 € e ha già pagato una rata di 100,00 €
  Quando porta il residuo iniziale a 99,99 €
  Allora la richiesta è rifiutata con "InvalidValue" sul residuo
  E il residuo di "Auto" resta 200,00 €
```

### UC10-S5

Test: `UC10_ModificareContiEDebiti.S5_RecordDiUnAltroUtente`

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Schema dello scenario: Conto o debito di un altro utente
  Quando "Anna" modifica <record> di "Mario"
  Allora la risposta è "non trovato"
  E <record> di "Mario" è com'era

  Esempi:
    | record        |
    | il conto      |
    | il debito     |
```

### UC10-S6

Test: [`UC10_ModificareContiEDebiti.S6_AvvisoPrimaDiModificareUnConto`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/portfolio/ui/UC10_ModificareContiEDebiti.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Scenario: Avviso prima di modificare un conto
  Quando "Mario" cambia il saldo iniziale di "Credem" e preme "Salva"
  Allora vede l'avviso che il patrimonio attuale e il suo andamento verranno ricalcolati
  Quando annulla
  Allora il conto non è stato modificato
  Quando preme di nuovo "Salva" e poi "Procedi"
  Allora il conto è modificato
```

### UC10-S7

Test: `UC10_ModificareContiEDebiti.S7_AggiuntaDisabilitataOltreIlLimite` (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Scenario: Aggiunta disabilitata oltre il limite
  Dato che "MySync:MaxAccounts" vale 2 e "Mario" ha già 2 conti
  Quando apre "Conti e debiti"
  Allora "Nuovo conto" è disabilitato con il messaggio "Hai raggiunto il numero massimo di conti."
  E "Nuovo debito" è attivo
```

### UC10-S8

Test: `UC10_ModificareContiEDebiti.S8_ModificareLaRataMensile`

```gherkin
# language: it
Funzionalità: Modificare conti e debiti

Scenario: Cambiare e togliere la rata mensile
  Dato che "Mario" ha il finanziamento "Auto" con rata mensile 250,00 €
  Quando cambia la rata mensile in 300,00 €
  Allora "Auto" ha rata mensile 300,00 € e il residuo non è cambiato
  Quando toglie la rata mensile
  Allora "Auto" non ha rata mensile
```
