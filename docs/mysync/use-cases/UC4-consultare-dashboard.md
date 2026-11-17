# UC4 – Consultare la dashboard

**Attore:** Utente
**Obiettivo:** capire a colpo d'occhio quanto vale oggi il proprio patrimonio e cosa è successo di recente.

## Precondizioni
- L'utente ha almeno un conto (altrimenti si apre UC1).

## Flusso principale
1. L'utente apre MySync.
2. Il sistema mostra:
   - il **patrimonio netto** di oggi (la somma dei saldi dei conti) e i **debiti residui**;
   - i **conti**, ognuno con tipo, nome, tag e saldo di oggi;
   - i **debiti**, ognuno con tipo, nome, conto collegato e residuo; gli estinti in fondo, segnati come tali;
   - gli **ultimi movimenti** (i 10 più recenti per data, poi per inserimento), con data, importo con segno, conto, categoria ed eventuale debito;
   - l'**andamento del patrimonio** degli ultimi 30 giorni (UC7);
   - la **proiezione**: "Tra 12 mesi" con il patrimonio netto totale previsto, calcolato con i parametri salvati o, se non ce ne sono, con quelli proposti (UC14). Un clic apre la proiezione.
3. Dalla dashboard l'utente può registrare una spesa o un ricavo (UC5), aggiungere un conto (UC2) o un debito (UC3) finché non ha raggiunto i limiti, e aprire l'andamento su altri periodi (UC7).

## Flussi alternativi
- **2a. Nessun movimento:** al posto dell'elenco il sistema invita a registrare la prima spesa o il primo ricavo.
- **2b. Nessun debito:** la sezione debiti mostra solo "Nuovo debito" (se `MySync:MaxDebts` > 0).
- **2c. Saldo negativo:** il saldo del conto è evidenziato (D7).

## Regole
- R1. Saldo di un conto oggi = saldo iniziale + ricavi − spese con data fino a oggi.
- R2. Residuo di un debito = residuo iniziale − spese collegate al debito.
- R3. Patrimonio netto = Σ saldi dei conti; debiti residui = Σ residui dei debiti. I debiti non si sottraggono dal patrimonio (D9).
- R4. I conti sono ordinati per data di apertura, i debiti attivi per residuo decrescente.
- R5. Tutti i valori sono calcolati dai movimenti al momento della richiesta: nessun saldo salvato che possa divergere *(shortcut accettabile per i volumi di una persona; da rivedere solo se le risposte diventano lente)*.
- R6. Il riquadro della proiezione usa sempre 12 mesi e tutti i conti (UC14-R1): con gli switch di UC12 spenti dice "patrimonio totale" per non confondersi con il patrimonio di tutti i giorni. Con gli importi nascosti mostra `••••`.

## Scenari di accettazione

Test: [`UC4_ConsultareDashboard`](../../../src/MySync/Tests/FinHorizon.MySync.Backend.Tests/UC4_ConsultareDashboard.cs)

### UC4-S1

Test: `UC4_ConsultareDashboard.S1_Riepilogo`

```gherkin
# language: it
Funzionalità: Consultare la dashboard

Scenario: Riepilogo del patrimonio
  Dato che "Mario" ha il conto "Conto Intesa" con saldo iniziale 2.500,00 € e il conto "Contanti" con saldo iniziale 200,00 €
  E ha il mutuo "Mutuo casa" con residuo 120.000,00 €
  E ha registrato su "Conto Intesa" il ricavo "Stipendio" di 1.800,00 € e la spesa "Bollette" di 150,00 €
  Quando "Mario" apre la dashboard
  Allora il saldo di "Conto Intesa" è 4.150,00 € e quello di "Contanti" è 200,00 €
  E il patrimonio netto è 4.350,00 € e i debiti residui 120.000,00 €
```

### UC4-S2

Test: `UC4_ConsultareDashboard.S2_UltimiMovimenti`

```gherkin
# language: it
Funzionalità: Consultare la dashboard

Scenario: Solo gli ultimi 10 movimenti, dal più recente
  Dato che "Mario" ha registrato 12 movimenti in 12 giorni diversi
  Quando "Mario" apre la dashboard
  Allora vede 10 movimenti
  E il primo è quello con la data più recente
```

### UC4-S3

Test: `UC4_ConsultareDashboard.S3_RataRiduceIlResiduo`

```gherkin
# language: it
Funzionalità: Consultare la dashboard

Scenario: Il residuo tiene conto delle rate
  Dato che "Mario" ha il mutuo "Mutuo casa" con residuo 120.000,00 €
  E ha registrato la spesa di 650,00 € collegata a "Mutuo casa"
  Quando "Mario" apre la dashboard
  Allora il residuo di "Mutuo casa" è 119.350,00 €
```

### UC4-S4

Test: `UC4_ConsultareDashboard.S4_SoloIPropriDati`

```gherkin
# language: it
Funzionalità: Consultare la dashboard

Scenario: La dashboard mostra solo i dati dell'utente
  Dato che "Mario" e "Anna" hanno ciascuno i propri conti e movimenti
  Quando "Anna" apre la dashboard
  Allora vede solo i suoi conti, debiti e movimenti
  E il patrimonio netto è calcolato solo sui suoi dati
```

### UC4-S6

Test: [`UC4_ConsultareDashboard.S6_RiquadroProiezione`](../../../src/MySync/FinHorizon.MySync.Frontend/src/pages/dashboard/ui/UC4_RiquadroProiezione.test.tsx) (frontend, Vitest)

```gherkin
# language: it
Funzionalità: Consultare la dashboard

Scenario: Il riquadro della proiezione
  Dato che "Mario" ha una liquidità di 1.000,00 € e i parametri proposti di UC14-S2
  Quando apre la dashboard e preme "Mostra"
  Allora il riquadro della proiezione dice "Tra 12 mesi: 9.400,00 €"
  Quando preme il riquadro
  Allora si apre la proiezione
```
