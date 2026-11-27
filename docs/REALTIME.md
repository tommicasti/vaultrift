# Vaultrift in tempo reale

Questo documento raccoglie le decisioni prese per la versione in tempo reale e multigiocatore. Si sviluppa sul branch `feature/realtime`; `dev` e `main` restano il gioco a turni pubblicato finché la nuova versione non è pronta.

Va aggiornato a ogni traguardo: quando una decisione cambia, si corregge qui, non solo nel codice.

---

## Decisioni del 26 novembre 2026

- **Solo tempo reale.** Il gioco a turni è **congelato**: niente funzioni nuove, solo correzioni. Resta online su `main` finché il tempo reale non ha tutto. Al rilascio i salvataggi vengono migrati e il codice dei turni viene eliminato.
- **Vista 2D anche in tempo reale**, come secondo modo di disegnare gli snapshot. Si fa dopo i contenuti.
- **La difficoltà appartiene al mondo, non all'eroe.** Con lo stesso eroe si gioca a Normal, Hard o Hardcore in mondi diversi. Le difficoltà affrontate si mostrano insieme ai trofei.
- **Hardcore**: se l'eroe muore, quel mondo scompare. L'eroe **tiene livello, esperienza e punti statistica** (spesi e da spendere) e **perde equipaggiamento, zaino, monete e incantesimi imparati**; riparte con il kit iniziale della sua classe e deve iniziare un mondo nuovo.
- **I mondi appartengono all'account**, non all'eroe: qualunque eroe dell'account può entrare in qualunque mondo dell'account. Ogni mondo ha la sua difficoltà e il suo piano.
- Gli eroi Hardcore restano fuori dalla cooperativa, come deciso in precedenza.
- **Schermata iniziale**:
  - l'eroe mostra il suo nome, con sotto classe e livello; se il nome non viene scritto se ne genera uno a caso;
  - dopo *Continue* si sceglie il mondo: continuare quello esistente o crearne uno nuovo, scegliendo la difficoltà;
  - il collegamento dei vecchi codici di salvataggio è stato tolto, perché ormai c'è l'account.
- **Ordine dei lavori**:
  1. forzieri, scale, cambio di piano dell'ospite, telecamera verticale (fatto);
  2. **pacchetto 1** — eroi e mondi dell'account, difficoltà del mondo, regola Hardcore (fatto);
  3. **pacchetto 2** — mercante, pietra del viaggio, tomba; poteri dei boss, Spaccatura del piano 21 con totem e Custode, schermata di vittoria (fatto);
  4. **pacchetto 3** — terreni dei biomi (lava, acqua, cristalli, spore, correnti), effetti degli oggetti che mancano e i 12 poteri esotici (fatto);
  5. **pacchetto 4** — client: mappa, trofei nei mondi, vista 2D, predizione lato client, telefoni vecchi (fatto);
  6. **pacchetto 5** — rilascio: migrazione, eliminazione dei turni, Docker e Container Apps con il blocco dei costi.

## 1. Il modello: mondi alla Valheim

- Ogni account può creare uno o più **mondi** (fino a 24). Il mondo appartiene all'account; chi lo apre è l'**ospite**.
- Un mondo aperto in cooperativa ha un **codice d'invito**: chi lo riceve può entrare con il proprio eroe. Un mondo aperto in solitaria non accetta altri giocatori.
- Al massimo **6 giocatori** per mondo.
- Se l'ospite esce, il mondo si chiude per tutti; gli altri vengono salvati e tornano al menu.
- Chi si disconnette esce dalla partita; quando rientra con il codice ricompare all'ingresso del piano su cui si trova il gruppo.

### Eroe portatile

L'eroe appartiene all'**account**, non al mondo: livello, esperienza, punti, equipaggiamento, zaino, monete, slot rapidi e incantesimi lo seguono in qualunque mondo.

Questo sostituisce la regola del gioco a turni per cui lo zaino apparteneva alla partita e la partita successiva partiva vuota.

Un eroe molto più forte del mondo in cui entra è accettato, come in Valheim. La regola esistente sull'esperienza (−25% per ogni livello sopra il mostro, minimo 10%) basta a evitare che salga a spese degli altri.

### Deposito dell'account

- Un numero limitato di posti condivisi tra tutti gli eroi dell'account, ampliabile con le monete.
- Si usa **solo fuori dal mondo**, dalla schermata degli eroi: niente svuotamento dello zaino a metà piano.
- Contiene la **forgia**: le armi del drago non occupano posti e non si consumano; ogni eroe può prenderne una copia della propria classe. Il tier dipende dai trofei del Custode, come oggi.

## 2. Regole in cooperativa

| Tema | Regola |
|---|---|
| Difficoltà | In cooperativa solo **Hard**. Hardcore resta per il gioco in solitaria. |
| Morte | Come Valheim: l'equipaggiamento resta nella tomba, l'eroe rinasce all'ingresso del piano con l'arma di scorta e deve tornare a recuperarlo. Solo il proprietario può riprendere la propria tomba; gli altri possono difenderla. |
| Bottino | **Condiviso**: il server tira il bottino una volta per ogni giocatore del gruppo; gli oggetti cadono a terra per tutti e li prende chi arriva prima. In quattro ci sono circa quattro volte gli oggetti, quindi la quantità per giocatore resta giusta. |
| Scaling | Salute e numero dei mostri, e salute dei boss, crescono con il numero di giocatori. I valori si tarano giocando. |
| Piani | Tutto il gruppo sta sullo **stesso piano**. Il cambio di piano (scale e pietra del viaggio) lo decide l'ospite; chi è lontano viene portato con lui. |
| Totem del boss | Lo paga chi lo colpisce. |

## 3. Struttura

| Pezzo | Contenuto | Dove vive |
|---|---|---|
| Account | Accesso, trofei | Azure SQL (esiste) |
| Deposito | Posti dell'account e forgia | Azure SQL (nuova tabella) |
| Eroe | Classe, progressione, equipaggiamento, zaino, monete | Azure SQL (nuova tabella) |
| Mondo | Ospite, codice, biomi, piano più profondo, piani completati, totem, tombe per eroe | Azure SQL (nuova tabella) |
| Stanza | Il mondo caricato in memoria con i giocatori connessi, il piano attivo e il ciclo di gioco | Solo in memoria, sul server |

### Motore

- Il server ha l'**autorità**: i client mandano solo intenzioni, il server decide l'esito. È lo stesso principio del gioco a turni, ma il mondo avanza da solo con un ciclo a **20 tick al secondo**.
- Il motore in tempo reale è nuovo e **riusa** ciò che esiste: generatore dei piani, cataloghi (oggetti, mostri, biomi, incantesimi), calcolo del danno, elementi, bottino.
- Cambia il modello del tempo: posizioni continue invece di caselle, velocità in caselle al secondo, tempi di ricarica in secondi.
- Il motore resta C# puro, senza dipendenze web, così la stessa stanza può girare su Azure oggi e dentro un eseguibile desktop domani.

### Rete

- **SignalR** (WebSocket) tra client e stanza.
- Il client manda gli input (direzione di movimento, dove guarda, attacco, abilità, oggetti).
- Il server manda lo stato del piano e gli eventi del tick (colpi, morti, bottino), che il client usa per animazioni e log come oggi.
- Il proprio eroe si muove subito sullo schermo (predizione) e viene corretto dal server; gli altri eroi e i mostri sono mostrati con una piccola interpolazione.

### Client

- Restano sia la vista **3D** sia la vista **2D** (dall'alto, con movimento fluido).
- Su telefono: **joystick virtuale** a sinistra, attacco a destra con abilità, pozioni e incantesimi attorno.

## 4. Hosting

| Tappa | Dove gira il server | Chi gioca | Costo |
|---|---|---|---|
| Sviluppo | Il PC di sviluppo | Il PC e altri dispositivi della stessa rete di casa | 0 |
| Con gli amici | Azure Container Apps (consumo) | Chiunque abbia il codice, dal browser | 0 entro la quota gratuita |
| Steam (futuro) | Il PC di chi ospita | Giocatori Steam, tramite i relay di Steam | 0 |

### Perché Container Apps e non App Service F1

App Service F1 non regge il tempo reale: 60 minuti di CPU al giorno, 5 connessioni WebSocket, 165 MB di traffico in uscita al giorno. Il piano B1 basterebbe ma costa circa 12–13 € al mese anche se nessuno gioca.

Container Apps a consumo si spegne quando nessuno gioca e ogni mese include gratis 180.000 vCPU-secondi e 360.000 GiB-secondi: con un contenitore da 0,25 vCPU e 0,5 GB sono circa 200 ore di gioco. I primi 100 GB al mese di traffico in uscita da Azure sono gratuiti.

Alla tappa "con gli amici" tutto (sito, API, account, deposito, stanze) si sposta in un unico Container App, così c'è un solo indirizzo e un solo cookie; App Service F1 va in pensione. Azure SQL resta con l'offerta gratuita. Le immagini Docker si pubblicano su GitHub Container Registry, gratuito per i repository pubblici.

### Controllo dei costi

- Il budget di Azure **avvisa ma non blocca**. Con una sottoscrizione a consumo non esiste un limite di spesa che si possa attivare.
- Protezioni previste:
  - una sola istanza del Container App (`max replicas = 1`) e spegnimento automatico quando non c'è nessuno;
  - limiti nell'applicazione: numero massimo di stanze aperte e durata massima di una stanza senza input;
  - blocco manuale: nel portale, Container App → **Revisioni** → disattivare la revisione attiva;
  - blocco automatico: il budget al 100% avvia un *action group* che esegue un runbook di Azure Automation e disattiva il Container App. Si configura insieme alla pubblicazione.

## 5. Traguardi

1. **Sensazione di gioco**: un eroe si muove liberamente in un piano vero con collisioni sui muri; un tipo di mostro lo vede, lo insegue e lo attacca; l'eroe attacca in mischia. Vista 3D, server sul PC di sviluppo.
2. **Combattimento completo**: tutte le armi (fendente, affondo, frecce come proiettili), elementi, bottino condiviso, forzieri, morte con tomba, scale.
3. **Eroe, mondo e deposito**: nuove tabelle, salvataggi, forgia, migrazione dei salvataggi esistenti.
4. **Multiplayer**: ingresso con codice fino a 6 giocatori, scaling, cambio piano deciso dall'ospite, disconnessione e rientro.
5. **Contenuti**: poteri dei boss, terreni, incantesimi, esotici, comandi da telefono, vista 2D.
6. **Pubblicazione**: Docker, GitHub Container Registry, Azure Container Apps, pipeline, blocco automatico dei costi.

## 6. Salvataggi esistenti

Nessun eroe e nessuna partita del gioco a turni vanno persi. Con il pacchetto 1 la conversione avviene da sola alla prima apertura del deposito (dettagli nello stato dei lavori); tomba, piani completati, totem e forgia si aggiungono quando arrivano nel tempo reale. La conversione completa prevista:

- l'**eroe** (classe, nome, livello, esperienza, punti, equipaggiamento, zaino, monete, slot rapidi, incantesimi) diventa un eroe dell'account;
- il **dungeon** (biomi, piano più profondo, piani completati, totem) diventa un mondo dell'account con l'eroe come unico ospite;
- la tomba, se c'è, resta nel mondo con il suo contenuto;
- le armi del drago già possedute passano nella forgia.

Fino alla pubblicazione della versione in tempo reale, il gioco a turni resta online e i salvataggi continuano a funzionare come oggi.

## 7. Stato dei lavori

### Traguardo 1 — sensazione di gioco (fatto)

Cosa c'è:

- **Motore** (`src/Vaultrift.Engine/Realtime`): `LiveFloor` avanza a 20 tick al secondo con passo fisso.
  - Posizioni continue in caselle: il centro della casella (x, y) è il punto (x, y).
  - Eroi e mostri sono cerchi: raggio 0,3 l'eroe, 0,35 i mostri, 0,55 i boss.
  - Il movimento separa gli assi (`Collision.Slide`), così contro un muro si scivola invece di fermarsi.
  - Velocità: eroe 3,8 caselle/s (4,6 per le classi veloci); mostri 1,8 / 2,6 / 3,6 secondo la velocità del tipo.
- **Combattimento**:
  - l'eroe colpisce il mostro più vicino entro 1 casella, in un arco di 120° davanti a sé; ricarica 0,55 s (0,45 per le classi veloci);
  - i mostri caricano il colpo per 0,5 s prima di colpire: allontanarsi in quel momento schiva il colpo. Il client lo mostra con un cerchio rosso a terra che si riempie fino alla portata del colpo, e con l'animazione d'attacco rallentata in modo che l'affondo cada alla fine della carica (i mostri costruiti in codice hanno una posa dedicata, `Charge`: si ritraggono, tremano e scattano in avanti);
  - il danno usa le regole del gioco a turni (`DamageCalculator`): schivata, parata, critico.
- **Inseguimento**: i mostri si svegliano entro 5 caselle se esiste un percorso breve fino all'eroe. Se la linea fino all'eroe è libera (`Collision.ClearPath`) vanno dritti; altrimenti seguono un *flow field* (distanze calcolate dal server a partire dalla casella dell'eroe), così aggirano i muri. Senza nessuno da inseguire tornano a casa, si curano e si riaddormentano.
- **Morte**: l'eroe rinasce all'ingresso dopo 3 s con la salute piena.
- **Server**: `RoomHub` (SignalR, `/hubs/room`, richiede l'accesso) apre una stanza per connessione. Il piano è il primo di una partita generata come nel gioco a turni (`LiveArena`). `RoomTicker` fa avanzare tutte le stanze e manda uno snapshot a ogni tick; se un tick arriva in ritardo, recupera fino a 3 tick di arretrato.
- **Limiti di costo già attivi**: al massimo 12 stanze aperte (`Realtime:MaxRooms`); una stanza senza input per 10 minuti viene chiusa.
- **Client** (`web/src/realtime`): scena 3D con mappa, decorazioni, modelli e animazioni del gioco a turni. Gli altri elementi sono disegnati 80 ms nel passato e interpolati tra due snapshot, usando il tempo del server: il movimento resta fluido anche se i pacchetti arrivano irregolari. Animazioni ed effetti degli eventi partono con lo stesso ritardo di 80 ms, così coincidono con le posizioni mostrate. Lo scarto tra orologio del server e del client si stima sul minimo dei ritardi degli ultimi 2 secondi e si corregge gradualmente, così i pacchetti in ritardo non fanno scattare la scena. Numeri del danno, barre della vita sopra i mostri, zoom con rotellina o pulsanti.

Come provarlo in locale:

1. avvia l'API come al solito e il client con `npm run dev` (il proxy di Vite inoltra anche `/hubs`, con WebSocket);
2. accedi con il tuo account;
3. nella schermata iniziale, sotto la scelta della classe, il riquadro **Real-time prototype** apre un piano con la classe scelta;
4. WASD o frecce per muoversi, Spazio o clic per attaccare, Esc per uscire.

### Traguardo 2, prima parte — l'eroe vero (fatto)

La scelta chiave: **l'eroe della stanza è il `Player` del motore a turni**, non una copia. `LiveHero` lo avvolge e aggiunge solo ciò che serve al tempo reale (posizione continua, direzione, input, tempi di ricarica). Zaino, equipaggiamento, statistiche, punti, slot rapidi, pozioni, rune, incantesimi imparati, monete ed esperienza usano quindi le stesse regole e gli stessi test del gioco a turni.

- **Un turno vale un secondo.** Tutto ciò che nel gioco a turni dura "N turni" in tempo reale dura N secondi: potenziamenti delle pozioni, ricarica dell'abilità, rigenerazione (1 PV ogni 3 s, mana ogni 2 s, più gli effetti degli oggetti). `LiveFloor` fa questa manutenzione una volta al secondo.
- **Ricompense**: chi uccide un mostro riceve monete ed esperienza con le formule di `Progression`; i livelli danno punti statistica come sempre.
- **Bottino condiviso**: per ogni eroe presente il server tira il bottino del mostro (`VaultLoot`, lo stesso del gioco a turni) e lo fa cadere sulla casella libera più vicina. Passandoci sopra lo si raccoglie; con lo zaino pieno resta a terra e compare un avviso al massimo ogni 2 s.
- **Azioni dello zaino** via SignalR (`Equip`, `Unequip`, `Use`, `UseQuickSlot`, `AssignQuickSlot`, `SpendPoint`, `Discard`, `DiscardJunk`, `Mark`, `Learn`, `Preview`): non fanno mai passare un turno. Equipaggiare e usare oggetti passano da `LiveActions`, che chiama direttamente i metodi di `Player` senza il "fine turno" del gioco a turni; le azioni che già non consumavano turni chiamano i metodi di `Game` esistenti. Rispondono con gli eventi (descritti dal client con gli stessi testi del gioco a turni) e lo stato aggiornato dell'eroe.
- **Stato dell'eroe nello snapshot**: giocatore, zaino e capienza viaggiano solo quando cambiano (contatore di versione), al massimo 5 volte al secondo; salute e mana viaggiano a ogni tick. Gli oggetti a terra viaggiano solo quando cambiano.
- **Client**: barra di stato e zaino sono gli stessi componenti del gioco a turni (`StatusBar`, `InventoryScreen` con il ritratto 3D); Tab apre lo zaino, Q R F usano gli slot rapidi; oggetti a terra con lo stesso modello del 3D a turni; messaggi in basso a sinistra; "+XP" e "Level up!" sopra l'eroe; l'aspetto dell'eroe cambia quando cambi arma.

### Cooperativa, mira e telefono (prima versione, fatto)

- **Mondi con codice**: chi crea un mondo (`Create`) ne diventa l'ospite e riceve un codice di 6 caratteri (stesso alfabeto dei codici di salvataggio, senza lettere ambigue). Gli altri entrano con `Join(codice, eroe)`, fino a 6 eroi. Se l'ospite esce il mondo si chiude per tutti; se esce un altro, il suo eroe lascia il piano.
- **Scaling**: la salute dei mostri cresce del 60% per ogni eroe oltre il primo (`LiveRules.ExtraHeroHealth`); quando qualcuno esce torna giù mantenendo le ferite in proporzione. I mostri inseguono l'eroe più vicino.
- **Bottino condiviso** come deciso: un tiro per ogni eroe presente, a terra per tutti, lo prende chi arriva prima.
- **Snapshot per giocatore**: la parte comune (eroi, mostri, eventi, oggetti a terra) si calcola una volta per tick; ogni giocatore riceve in più solo il proprio stato dell'eroe e, quando cambia, l'elenco del gruppo con l'equipaggiamento di ciascuno, per disegnare le armi giuste sugli altri eroi.
- **Mira**: il server accetta una direzione di mira (input `aimX`, `aimY`); se è zero l'eroe guarda dove cammina e il colpo si gira da solo verso il mostro più vicino a portata. A terra un ventaglio chiaro mostra l'area del colpo (portata e arco, mandati dal server) e un anello bianco segna il mostro che verrebbe colpito. Come il client usa la mira è descritto in "Telecamera, prima persona e telefono".
- **Muri**: il taglio dei muri non dipende più dalla riga dell'eroe, quindi non spariscono e ricompaiono mentre cammini. In compenso la telecamera è più alta e si abbassano sempre e solo i muri rivolti verso la telecamera.
- **Scatti**: il client mostra il mondo 100 ms nel passato e, se uno snapshot arriva in ritardo, prolunga il movimento per al massimo 120 ms invece di fermarsi e saltare. I mostri che si sovrappongono si spingono via più dolcemente (25% della sovrapposizione per tick).
- **Telefono**: joystick virtuale a sinistra (la velocità segue quanto lo spingi), a destra attacco, slot rapidi, zaino e uscita. Compaiono solo sui dispositivi touch.

### Combattimento vero (seconda parte del traguardo 2, fatto)

- **Profili d'arma** (`AttackProfile`): ogni tipo d'arma colpisce in modo diverso. Il danno resta quello del gioco a turni (`DamageCalculator`: schivata, parata, critico, elementi); cambiano forma, portata e ritmo.

| Arma | Stile | Portata | Arco | Ricarica | Bersagli |
|---|---|---|---|---|---|
| spada, mazza, pugni | fendente | 1,0 | 120° | 0,55 s | 1 |
| pugnale | stoccata | 0,9 | 90° | 0,42 s | 1 |
| ascia | ampio fendente | 1,15 | 170° | 0,7 s | tutti nell'arco |
| lancia | affondo | 2,0 | 50° | 0,65 s | 2 |
| arco | tiro | 6 | — | 0,7 s | 1 (tutti con le frecce perforanti) |
| bastone con incantesimo | lancio | 6 | — | 0,8 s | secondo la forma |

  La portata si somma al raggio del mostro; gli eroi veloci ricaricano il 15% prima. Un bastone senza mana (o senza incantesimo attivo) dà un colpo di striscio senza elemento.
- **Proiettili** (`LiveProjectile`): frecce a 14 caselle/s, incantesimi a 11, avanzano a passi di 0,25 caselle e si fermano sui muri. Un tiro a bruciapelo (mostro entro 1,3 caselle) colpisce di striscio, come nel gioco a turni. Le frecce perforanti attraversano i mostri e dal secondo in poi fanno il 75% del danno.
- **Incantesimi** con le forme del gioco a turni: dardo (proiettile), esplosione (proiettile che scoppia nel punto d'impatto, raggio = dimensione + 0,5), catena (proiettile che rimbalza sui mostri vicini), linea (raggio istantaneo largo 0,9 caselle fino al primo muro), nova e tempesta (area intorno al mago). Il mana si paga al lancio; i tasti 1–4 scelgono l'incantesimo attivo.
- **Elementi in secondi**: fuoco brucia per i turni del gioco a turni, un danno al secondo, con l'esperienza a chi l'ha acceso; ghiaccio congela (il mostro si ferma e perde la carica) e dopo lascia un periodo di immunità; fulmine salta a un mostro entro 1,8 caselle; vento spinge indietro di una casella (i boss no). Probabilità e potenziamenti sono quelli di `ElementRules`.
- **Abilità di classe** (E, o il pulsante sul telefono), con ricarica in secondi: Baluardo riduce i danni presi, Furia aumenta quelli inflitti, Invisibilità fa perdere l'eroe ai mostri e il primo colpo diventa un agguato, Sovraccarico allarga il prossimo incantesimo, Colpo potente scaglia una freccia perforante a danno moltiplicato e portata 8.
- **Pergamene**: dallo zaino o dagli slot rapidi lanciano il loro incantesimo come fosse del bastone, senza far passare turni.
- **Client**: la mira mostra un ventaglio per le armi da mischia e una linea per quelle a distanza, sempre con la portata vera mandata dal server; frecce e sfere magiche visibili in volo; anelli a terra per le esplosioni; mostri in fiamme o congelati riconoscibili; eroe semitrasparente quando è invisibile e anello colorato per Baluardo e Furia.

### Telecamera, prima persona e telefono (fatto)

- **Terza persona senza mira del mouse**: il mouse non orienta più l'eroe. WASD lo muovono e lo girano nella direzione di marcia; Spazio o clic attaccano il mostro più vicino a portata (mira assistita del server, la stessa del telefono). Il ventaglio a terra si gira verso quel mostro, così si vede chi verrà colpito.
- **Prima persona** (tasto V, pulsante 1P, o rotellina avanti al massimo dello zoom): clic per agganciare il mouse, che gira la visuale; W avanti, S indietro, A e D di lato rispetto a dove guardi; si colpisce dove si guarda (il client manda la direzione della visuale come mira). Mani e arma come nella prima persona del gioco a turni, mirino al centro, muri senza taglio. V, Esc o rotellina indietro tornano alla terza persona; la scelta viene ricordata. Solo con il mouse: sui telefoni resta la terza persona.
- **Schermo verticale**: la telecamera si allontana in proporzione a quanto lo schermo è più stretto che largo (fino a 2,4 volte), altrimenti sul telefono si vedevano solo due caselle. Nebbia allontanata di conseguenza.
- **Zoom sul telefono**: pizzico con due dita sulla scena e pulsanti + e − sopra il joystick.
- **Telefono più leggero**: sui dispositivi touch o con pochi core niente ombre e risoluzione massima 1,5x, come nel 3D a turni.
- **Barra in alto**: su schermi stretti la barra di stato e il riquadro del mondo stanno uno sotto l'altro invece di sovrapporsi; sfondo più scuro per leggerla sopra i pavimenti chiari.

### I propri eroi nei mondi e nuova schermata iniziale (fatto)

**Eroe portatile, prima versione.** Si entra in un mondo con uno dei propri eroi, gli stessi del gioco a turni: `Create(eroe)` e `Join(codice, eroe)` ricevono l'Id della partita e il server controlla che appartenga all'account.

- Nel mondo si usa il `Player` del salvataggio: livello, esperienza, monete, oggetti, equipaggiamento e zaino guadagnati nel mondo restano all'eroe.
- Il dungeon del gioco a turni non viene toccato: posizione, piano, mostri e turno restano com'erano. Un test lo verifica giocando dieci secondi in un mondo.
- **Il mondo è un dungeon a sé**: `LiveArena.NewWorld` genera una partita separata (codice `WORLD`, classe dell'ospite per il bottino, sempre difficoltà **Hard**). Mappa, bioma e mostri arrivano da lì, per tutti.
- **Salvataggio**: l'eroe si salva quando esce, quando il mondo si chiude (anche per inattività o allo spegnimento del server) e ogni 15 secondi se è cambiato qualcosa. Chi esce da caduto torna a casa con vita e mana pieni.
- **Un eroe alla volta in un mondo**: `RoomRegistry` tiene un registro degli eroi in gioco. Lo stesso eroe non può entrare in due mondi e, finché è in un mondo, le azioni del gioco a turni su di lui rispondono **409** `hero-in-world`, perché il server e il gioco a turni non modifichino lo stesso eroe insieme.
- Non entrano gli eroi **Hardcore**, che giocano solo da soli, né quelli caduti per sempre.
- Il mondo non si salvava ancora: quando l'ospite usciva, il mondo spariva. Superato dal pacchetto 1.

**Corretto: la mappa di chi entra.** Chi entrava in un mondo riceveva la mappa di un dungeon diverso (il suo), mentre posizioni e mostri erano quelli dell'ospite: vedeva un corridoio dove in realtà c'era una stanza. Ora la mappa arriva sempre dal mondo.

**Schermata iniziale.**
- Sfondo 3D (`HomeBackdrop`): una stanza del dungeon con un bioma scelto a caso a ogni apertura, decorata come i piani di quel bioma, con un falò (ceppi, pietre, fiamme, scintille e una luce che tremola). L'eroe selezionato siede su un tronco davanti al fuoco, con il suo equipaggiamento. La posa seduta è costruita piegando anche e ginocchia sopra l'animazione di riposo, perché il pacchetto di animazioni non ne ha una.
- **Scelta dell'eroe** con le frecce. Di default è l'ultimo usato, e la scelta resta memorizzata sul dispositivo. Senza eroi siede un cavaliere di base. Mentre si sceglie la classe di un nuovo eroe, al falò compare quella classe.
- Schede **Solo** e **Co-op**:
  - **Solo**: continua la partita a turni; c'è anche il collegamento dei vecchi codici.
  - **Co-op**: *Host a world* o il codice per entrare, con il motivo quando l'eroe non può. Un nuovo eroe creato da qui non può essere Hardcore e non avvia la partita a turni.
- Il riepilogo degli eroi dell'account (`GET /api/account`) contiene anche l'equipaggiamento indossato (`gear`), per vestire l'eroe al falò.
- Sui telefoni la scena usa meno risorse e l'inquadratura si allarga per lo schermo verticale.

### Forzieri, scale e piani successivi (fatto)

- **Forzieri**: vengono dai piani generati come nel gioco a turni. Un eroe che ci arriva vicino (0,8 caselle) li apre: monete a chi lo apre (`Progression.ChestCoins`) e un tiro di bottino di forziere **per ogni eroe presente**, a terra accanto al forziere. Se è un **mimic**, al posto del bottino salta fuori il mostro già sveglio.
- **Scale**: si aprono quando tutti i boss del piano sono a terra (`StairsUnsealed`, luce verde). Se il piano non ha boss sono aperte da subito. Quelle dell'ultimo piano non si aprono.
- **Cambio di piano**: lo decide l'ospite. Quando **l'ospite** sale sulle scale aperte, tutto il gruppo scende. Il server genera il piano successivo dal mondo (`WorldSession.CreateFloor`) e ci rimette gli eroi all'ingresso; chi era caduto si rialza con vita e mana pieni. Gli eroi e il mondo vengono salvati, e il client riceve nello snapshot un nuovo `LiveWelcome` (`NewFloor`) con mappa, bioma, forzieri, scale e oggetti. Il tempo del server continua senza ripartire da zero, così l'interpolazione non si inceppa. Chi non è l'ospite e sale sulle scale vede *"Only the host can lead the party down."*
- **Ultimo piano**: per ora si arriva al piano 20. La Spaccatura con il Custode (piano 21) arriva con i contenuti dei boss.
- **Telefono in verticale**: più lo schermo è stretto, più la telecamera guarda dall'alto (fino a quasi a piombo), così i muri non coprono i corridoi.

### Pacchetto 1 — eroi e mondi dell'account (fatto)

**Il modello.** L'eroe e il mondo sono due cose separate, entrambe dell'account. Prima un salvataggio incollava insieme eroe e dungeon, quindi l'eroe non poteva uscire dal suo mondo.

| | Cosa contiene | Dove si salva |
|---|---|---|
| **Eroe** (`HeroSession`, `HeroSave`) | lo snapshot del `Player` (classe, nome, livello, esperienza, punti, equipaggiamento, zaino, monete, slot rapidi, incantesimi), le difficoltà affrontate, il tempo di gioco, le cadute | voce `hero` del deposito |
| **Mondo** (`WorldSession`, `WorldSave`) | difficoltà, stato del generatore casuale, biomi dei 20 piani, piano attuale e piano più profondo, classe usata per il bottino, tempo di gioco | voce `world` del deposito |

- Del mondo si salva **solo quello che serve a rigenerarlo**, non i mostri: quando si riprende, il piano attuale si rigenera dallo stato del generatore. Il documento resta di poche centinaia di byte, e su Cosmos costa poche RU.
- L'eroe nel mondo usa un `Game` "tascabile" (una stanza 1×1 senza mostri) solo per riusare le azioni già scritte del gioco a turni: equipaggiare, spendere punti, imparare incantesimi, scartare.

**Il deposito** (`IVaultRepository`): un'interfaccia con quattro operazioni (elenco dell'account, lettura, scrittura, cancellazione) e quattro implementazioni come per i salvataggi: memoria, file (`saves/vault/<account>/hero-<id>.json`), SQL (tabella `VaultEntries`) e **Cosmos** (container `vault`, partizione = Id dell'account, `data` non indicizzato). Davanti c'è una coda di scrittura (`WriteBehindVaultRepository`) come per i salvataggi: il server di gioco non aspetta mai il database dentro un tick.

**`VaultService`** crea, trova, salva e cancella eroi e mondi, con un limite di 24 eroi e 24 mondi per account. A un nuovo eroe dà la Scheggia del Drago se l'account ha battuto il Custode, come nel gioco a turni.

**Migrazione dei salvataggi a turni, automatica.** La prima volta che un account apre il deposito, ogni sua partita a turni diventa:
- un **eroe** con lo **stesso Id** della partita e tutto il suo `Player`, con vita e mana pieni;
- un **mondo** con Id derivato da quello della partita (sempre lo stesso), stessa difficoltà, biomi, stato del generatore e piano (al massimo il 20);
- una partita Hardcore già persa diventa solo l'eroe, con la regola Hardcore applicata.

La migrazione è una copia: il gioco a turni su `main` continua a funzionare sui suoi salvataggi. Se si cancella un eroe migrato, la partita viene scollegata dall'account, così non ricompare.

**Entrare in un mondo.** `Create(eroe, mondo, condiviso)` e `Join(codice, eroe)` sull'hub:
- in solitaria si può aprire un mondo di qualunque difficoltà;
- in cooperativa si apre solo un mondo **Hard**; i mondi Hardcore si giocano solo da soli;
- un mondo può essere aperto una volta sola (`That world is already open.`), un eroe può essere in un mondo solo;
- il mondo si salva quando si scende di piano, ogni 60 secondi e alla chiusura; l'eroe come prima (uscita, chiusura, ogni 15 secondi se è cambiato qualcosa).

**Cadute.**
- **Normal e Hard**: chi cade perde il 25% delle monete (`HeroFell` porta quante) e si rialza all'ingresso. La tomba di Hard arriva con il pacchetto 2.
- **Hardcore**: alla prima caduta il mondo viene cancellato, all'eroe si applica la regola Hardcore e la stanza si chiude con il messaggio *"Your hero fell: this Hardcore world is gone."*

**API.** `GET /api/vault` (eroi con equipaggiamento e difficoltà affrontate, mondi con piano e bioma), `POST /api/vault/heroes`, `DELETE /api/vault/heroes/{id}`, `POST /api/vault/worlds`, `DELETE /api/vault/worlds/{id}`. Non si cancella un eroe che è in un mondo né un mondo aperto (409).

**Schermata iniziale.**
- Sotto nome, classe e livello dell'eroe ci sono le **difficoltà che ha affrontato** (Normal, Hard, Hardcore).
- **Solo → Continue** apre la scelta del mondo: i mondi dell'account (difficoltà, piano, bioma, piano più profondo, tempo di gioco, con *Enter* ed elimina) e sotto *New world* con le tre difficoltà spiegate.
- **Co-op → Host a world** apre la stessa scelta con i soli mondi Hard; il codice per unirsi resta.
- La creazione di un eroe non chiede più la difficoltà.
- Il gioco a turni non è più raggiungibile da questo branch: l'interfaccia dei turni è stata tolta da `App.tsx`, i suoi componenti restano finché il pacchetto 5 non elimina il codice dei turni.

**Compatibilità.** I salvataggi a turni non vengono modificati. I mondi aperti con le versioni precedenti di questo branch non erano salvati, quindi non c'è nulla da convertire.

### Pacchetto 2 — mercante, viaggio, tomba, boss e Spaccatura (fatto)

**Mercante.** Quando il piano ne ha uno (stesse regole di comparsa del gioco a turni), sta fermo nella sua casella e non si attraversa.
- Si apre il negozio colpendolo da vicino (1,3 caselle), come nel gioco a turni (`MerchantGreeted`); avvicinarsi mostra solo un suggerimento. Si compra, si vende, si vende la robaccia e si allarga lo zaino con `Buy`, `Sell`, `SellJunk` e `BuyUpgrade` sull'hub.
- Le regole e i prezzi sono quelli del gioco a turni, con i prezzi legati al **piano del mondo**.
- Il magazzino è unico per tutto il gruppo: ciò che compra uno sparisce per tutti.

**Pietra del viaggio.** C'è su ogni piano, accanto all'ingresso. Nella Spaccatura è nella posizione fissata da `RiftArena`.
- Il viaggio si apre colpendo la pietra da vicino (`WaystoneTouched`), non avvicinandosi: così la finestra non compare ogni volta che ci passi accanto.
- Solo l'**ospite** sceglie la destinazione (`Travel(piano)`), tra i piani già raggiunti in quel mondo. Il cambio avviene al tick successivo, con lo stesso `NewFloor` delle scale. Gli altri vedono *"Only the host can choose where the party travels."*

**Totem.** Su un piano già ripulito in quel mondo, al posto del boss c'è un totem e le scale sono aperte. Lo paga chi lo colpisce:
- il primo colpo mostra il prezzo (`TotemPrimed`);
- un secondo colpo entro 3 secondi paga e risveglia il boss.

Il prezzo cresce del 50% a ogni risveglio nel mondo (`Game.TotemPriceFor`). La Spaccatura non ha totem: il Custode c'è sempre.

**Cadute e tomba.** Le regole dipendono dalla difficoltà del mondo (`FallRule`):
- **Normal**: si perde il 25% delle monete.
- **Hard**: le monete restano, ma l'equipaggiamento finisce in una **tomba** sul punto della caduta. Fanno eccezione l'arma di scorta e le armi del drago. L'eroe si rialza con l'arma di scorta.
  - La tomba la apre **solo il proprietario**, camminandoci sopra. Gli altri la vedono con il nome del proprietario.
  - Cadere di nuovo prima di recuperarla la fa sparire.
  - Le tombe si salvano nel mondo, con il proprietario. Se si lascia il piano o il mondo, la tomba aspetta vicino all'ingresso di quel piano (almeno due caselle), come nel gioco a turni.
- **Hardcore**: come nel pacchetto 1.

**Poteri dei boss.** Ogni 3 secondi un boss sveglio usa il suo potere sul bersaglio. I valori sono quelli del gioco a turni.

| Potere | Effetto in tempo reale |
|---|---|
| Anello di lava | le caselle intorno al boss diventano lava per 3 secondi |
| Tentacoli | il bersaglio tra 2 e 5 caselle viene tirato di una casella verso il boss |
| Fioritura di cristalli | crescono cristalli intorno al boss (non si attraversano); alla volta dopo esplodono e feriscono chi è vicino |
| Evocazione | fino a 2 servitori alla volta, al massimo 4 vivi; muoiono con il boss |
| Raffica | il bersaglio entro 3 caselle viene spinto via di 3; se sbatte contro un muro prende danno |

- Il Custode cambia potere e velocità per fasi. Al cambio arriva `BossPhaseChanged` e il nuovo potere scatta subito.
- La mappa cambia durante il gioco: `TilesChanged` porta le caselle e il nuovo simbolo, e il client aggiorna il disegno.
- Stare sulla **lava** brucia ogni secondo (10% della salute massima, almeno 3), a meno di un mantello con resistenza al fuoco. Vale anche per la lava dei biomi.

**Spaccatura e vittoria.** Le scale del piano 20 portano alla Spaccatura (piano 21), con il Custode. Quando cade:
- `FloorCleared` segna il piano come ripulito nel mondo;
- `VaultConquered` segna il mondo come conquistato e apre la schermata di vittoria per tutti.

Nella scelta dei mondi, un mondo conquistato mostra *Vault conquered*, e un mondo con la tomba dell'eroe selezionato lo dice.

**Stato dell'eroe.** `LiveHeroState` ora porta anche:
- piano più profondo e conquista;
- mercante con le offerte, pietra con le destinazioni, totem con il prezzo, tombe del piano.

I prezzi di vendita dello zaino sono calcolati sul piano del mondo.

**Migrazione.** Dalle partite a turni ora passano anche piano più profondo, piani ripuliti, usi del totem, conquista e tomba.

### Gruppo più grande: più mostri e indicatori dei compagni (fatto)

Decisione: la mappa **non** si allarga in cooperativa. Gli eroi si attraversano tra loro, quindi non ci sono ingorghi. Una mappa più grande vorrebbe dire solo più strada a vuoto, e non si potrebbe adattare a chi entra a metà piano.

- **Salute**: i mostri normali hanno +30% di salute per ogni eroe oltre il primo (prima era +60%), i boss +60%. Si ricalcola dal vivo quando qualcuno entra o esce.
- **Rinforzi**: quando il gruppo arriva su un piano (scale o pietra del viaggio), ogni 2 eroi oltre il primo si aggiungono mostri pari al 35% dei mostri normali del piano.
  - Il 35% corrisponde a circa un mostro in più per stanza. Il generatore non conserva le stanze, quindi il numero si calcola sul piano intero.
  - I nuovi mostri sono copie di quelli esistenti (stesso tipo, rarità e livello), a una o due caselle dalla loro casa e mai vicino all'ingresso.
  - Chi entra a metà piano non aggiunge mostri: conta il gruppo all'arrivo. Il primo piano di un mondo appena aperto ha solo l'ospite.
- **Servitori dei boss**: ogni 2 eroi oltre il primo, +1 servitore per evocazione e +2 vivi al massimo.
- **Indicatori dei compagni**: un compagno fuori dallo schermo compare sul bordo, con una freccia verso di lui, il nome e la distanza in caselle. Diventa rosso se è caduto. Funziona anche in prima persona. Quando arriverà la mappa (pacchetto 4), i compagni ci saranno segnati.

### Pacchetto 3 — terreni, effetti degli oggetti ed esotici (fatto)

**Terreni dei biomi.** Valgono le stesse regole del gioco a turni, con il "turno" sostituito da un secondo.

| Terreno | In tempo reale |
|---|---|
| Lava | brucia ogni secondo il 10% della salute massima (minimo 3): eroi senza mantello di fuoco e mostri non ignifughi; i mostri non ignifughi non ci entrano |
| Acqua | i colpi di fulmine su un mostro in acqua fanno il doppio e contano come efficaci (armi, incantesimi e scariche) |
| Spore | chi ci sta sopra resta avvelenato 3 secondi, che si rinnovano finché non se ne va: 5% della salute massima al secondo (minimo 2) |
| Correnti | spingono eroi e mostri (non boss, non immobili) a 3 caselle al secondo nella loro direzione |
| Cristalli | non si attraversano; un colpo in mischia o una freccia li rompe, e le schegge feriscono i mostri entro 1,5 caselle |
| Ostacoli | un colpo in mischia li abbatte |

Rompere cristalli e ostacoli cambia la mappa con lo stesso `TilesChanged` dei boss.

**Effetti degli oggetti che mancavano.** Funzionano come nel gioco a turni:
- risucchio vitale, anche quello durante la Furia;
- spine;
- riflesso del blocco;
- doppio tiro: una seconda freccia leggermente angolata;
- tiro lontano: critico in più oltre 4 caselle;
- rigenerazione e prolungamento della Furia;
- furtività: i mostri si svegliano più vicino;
- imboscata più forte;
- risucchio di mana alle uccisioni.

**I 12 poteri esotici.**

| Potere | In tempo reale |
|---|---|
| Riftrender | ogni 3° colpo sullo stesso bersaglio fa il doppio e ignora la difesa |
| Oathbound | il danno subito torna come bonus sul colpo successivo |
| Headsman's Moon | un'uccisione rende subito pronto il colpo successivo |
| Worldsplitter | i mostri colpiti (non boss) restano storditi per 1 secondo |
| Rift Needle | le frecce trapassano, al 75% dal secondo bersaglio |
| Huntress' Promise | il bersaglio colpito è marchiato per 3 secondi (+50% danni); ucciderlo marchiato cura il 10% |
| Widow's Kiss | ogni colpo aggiunge veleno (fino a 5 dosi, 2 danni per dose al secondo) |
| Shadowstep | colpendo un mostro poco fuori portata, gli si balza addosso con un critico; ricarica di 3 secondi |
| Last Wall | un mostro che carica il colpo davanti all'eroe viene colpito subito (al massimo una volta al secondo per mostro) |
| Starfall Lance | l'affondo arriva una casella più lontano |
| Prism | gli incantesimi usano l'elemento debole del bersaglio |
| Echo | ogni incantesimo lanciato col bastone si ripete gratis al 50% |

Quando un potere scatta compare una scritta sopra il bersaglio. Nuovi eventi:
- `ExoticTriggered`;
- `MonsterShardHit`, `MonsterBurnedByLava`, `MonsterPricked`, `MonsterEnvenomed`;
- `HeroPoisoned`, `HeroPoisonDamaged`, `HeroLifeDrained`, `HeroManaSiphoned`;
- `CrystalBroken`, `ObstacleBroken`.

### Pacchetto 4 — mira, predizione, mappa, vista 2D e trofei (fatto)

**Mira automatica verso dove ti muovi.** Senza una mira esplicita (terza persona, telefono, 2D), tra i nemici alla portata il server sceglie il più vicino **dentro un cono di 60° nella direzione del movimento**. Da fermo conta la direzione in cui guardi. Solo se lì davanti non c'è nessuno, sceglie il più vicino in assoluto. In prima persona resta la mira del mirino.

**Predizione lato client.** Prima il proprio eroe si vedeva con lo stesso ritardo degli altri (circa 100 ms di buffer più la latenza). Ora il client muove subito il proprio eroe:
- **Movimento.** Usa la stessa velocità (`HeroSpeed` nel benvenuto) e le stesse collisioni con muri, ostacoli e cristalli del server (`prediction.ts` copia `Collision.Slide`).
- **Correzione.** Vedi "Predizione senza rimbalzo" più sotto: la prima versione tirava l'eroe indietro quando si fermava.
- **Salti.** Sopra 1,5 caselle di differenza (cambio di piano, rinascita, spinte) il client salta direttamente alla posizione giusta.
- **Mostri e oggetti fissi.** Il client non li prevede: se ti blocca un mostro, la correzione riporta l'eroe in pochi decimi di secondo.

Il server resta l'unico a decidere: la predizione cambia solo ciò che vedi.

**Mappa (tasto M o pulsante Map).**
- Si apre sopra il gioco senza fermarlo, con i biomi disegnati come nel gioco a turni.
- Mostra solo le caselle già esplorate, cioè viste dal proprio eroe: entro 7 caselle se nessun muro è in mezzo, più la stanza intera in cui ci si trova (la stessa regola della mappa a turni, `visibleTiles`). Si azzera a ogni piano.
- Segna i compagni (blu, rossi se caduti), te (giallo), scale, forzieri, mercante, pietra, totem, tombe e oggetti a terra. Tutto ciò che sta su una casella compare solo dopo che quella casella è stata esplorata.
- Il boss compare (rombo rosso con il nome) solo dopo averlo incontrato: entro 7 caselle e senza muri in mezzo. Qui non vale la regola della stanza intera, perché stanze e corridoi larghi possono formare un'unica zona molto grande. Da quel momento resta segnato dove si trova, anche se si allontana.

**Vista 2D (pulsante 2D/3D).**
- Disegna lo stesso stato dall'alto su un canvas, con le immagini del vecchio tabellone a turni: tessere dei biomi, terreni animati, eroi, mostri con barra della salute, proiettili, numeri dei danni, nomi dei compagni.
- Comandi e regole sono gli stessi; la prima persona non c'è.
- La scelta resta salvata sul dispositivo.

**Telefoni vecchi.**
- Senza WebGL il gioco parte direttamente in 2D e il 3D non viene caricato.
- In 3D un contatore misura i fotogrammi al secondo: sotto 24 per due finestre di 5 secondi di fila, propone *"Switch to 2D"*. Si può rifiutare per sempre con *"Keep 3D"*.
- Passando al 2D la scena 3D resta caricata ma ferma (`frameloop="never"`), così il ritorno al 3D è immediato e la GPU non lavora.

**Trofei nei mondi.** Gli stessi trofei del gioco a turni ora si sbloccano anche in tempo reale (`TrophyService.RecordLive`). L'avviso compare in gioco.
- *First blood* e *King slayer*: chi uccide.
- *Collector*: oggetto esotico raccolto o comprato.
- *Untouchable*: scendere le scale senza aver preso danni su quel piano. Arrivare con la pietra del viaggio non conta.
- *Unbroken*: arrivare al piano 10 senza essere mai caduti con quell'eroe.
- *Rich*: monete da uccisioni e forzieri. Si contano a blocchi, al salvataggio o al cambio di piano, per non scrivere sul database a ogni uccisione.
- *Custode* e *Weapon master*: per tutto il gruppo, alla vittoria, secondo la difficoltà del mondo.

Le cadute degli eroi ora si contano a ogni difficoltà.

**Corona del boss e barra del boss.** Come nel gioco a turni, un boss incontrato ha un indicatore con la corona sul bordo dello schermo quando è fuori vista, con il nome e la distanza (in 3D accanto agli indicatori dei compagni, in 2D disegnato sul canvas, che ora segna anche i compagni). La barra con il nome sopra il boss in 3D compare con la stessa regola: prima era sempre visibile, e siccome è un elemento HTML sopra la scena si vedeva anche attraverso i muri. "Incontrato" vale per ognuno: in cooperativa un compagno che combatte il boss lontano non lo rivela agli altri (`meetBosses` in `topDownLayout.ts`).

**Corretto nel gioco a turni (main): la corona prima di incontrare il boss.** La corona si accendeva quando il boss entrava nel rettangolo dello schermo 2D intorno all'eroe, muri compresi: bastava passare accanto alla stanza del boss, dall'altra parte di una parete. In più la memoria era legata a partita e numero di piano, quindi un piano ricostruito dalla pietra del viaggio ereditava la corona della visita precedente. Ora serve anche la linea di vista libera (`clearLine` in `vision.ts`) e la chiave è quella della disposizione del piano (`layoutKey`).

**Statistiche nel mercante.** Ogni pezzo in vendita che si può indossare mostra come cambierebbero le statistiche: valore attuale, freccia verde in su o rossa in giù, nuovo valore (vita, mana, attacco, difesa, schivata, critico, parata, resistenza). Il calcolo lo fa il server con `Player.PreviewItem`: prende una copia dell'eroe con un posto in più nello zaino, ci mette il pezzo e lo indossa. Le formule (set, modificatori di classe, limiti) restano in un solo posto e lo zaino pieno non impedisce l'anteprima. Sull'hub c'è `PreviewOffer(offerId, hand)`. Le anteprime si richiedono quando si apre il negozio e quando cambiano offerte, equipaggiamento o livello, non a ogni snapshot.

Corretto anche lo zaino: l'anteprima di un oggetto selezionato veniva richiesta di nuovo a ogni snapshot (20 volte al secondo), perché la funzione passata al componente cambiava a ogni disegno. Ora è stabile (`useCallback`).

### Giocare dal telefono in casa (rete locale)

1. avvia l'API come al solito da Visual Studio;
2. in `web/` lancia `npm run dev:lan` invece di `npm run dev`: Vite si mette in ascolto anche sull'indirizzo del PC nella rete di casa e stampa un indirizzo tipo `http://192.168.1.23:5173`;
3. dal telefono, collegato allo stesso Wi‑Fi, apri quell'indirizzo, accedi con un account e unisciti al mondo con il codice;
4. se il telefono non si collega, alla prima esecuzione consenti Node.js nelle reti private quando il firewall di Windows lo chiede.

Il proxy di Vite in sviluppo toglie l'attributo `Secure` dal cookie di accesso: l'API in locale risponde in HTTPS e il cookie verrebbe marcato `Secure`, ma il telefono apre la pagina in HTTP e lo rifiuterebbe. In produzione non cambia nulla, perché lì tutto è HTTPS.

Limiti noti, da affrontare nei prossimi passi:

- le decorazioni non hanno collisioni;
- i raggi di collisione vanno tarati sulla dimensione dei modelli.

## Riconnessione

Se la connessione cade (il telefono passa dal Wi-Fi ai dati, lo schermo si blocca, la rete ha un buco), l'eroe non esce più dal mondo.

**Lato server.**
- Ogni eroe nel mondo riceve nel `LiveWelcome` un `ResumeToken` casuale (128 bit), valido solo per il suo account.
- Quando una connessione si chiude senza che il client abbia chiamato `Leave`, la stanza parcheggia l'eroe per 30 secondi (`Room.ResumeGrace`). Il motore lo segna `IsAway`: non si muove, i mostri non lo vedono e non lo inseguono, e nessun danno lo raggiunge (colpi, terreni, veleno, poteri dei boss). Resta però nel conteggio del gruppo, quindi i mostri non si indeboliscono: staccarsi non diventa un trucco.
- Al parcheggio si salvano l'eroe e le monete in sospeso.
- Il client si ricollega con una connessione nuova e chiama `Resume(token)`. La stanza sposta l'eroe sulla nuova connessione (anche il ruolo di host) e risponde con un `LiveWelcome` completo.
- Se i 30 secondi passano, l'eroe esce come prima. Se era l'host, il mondo si chiude e gli altri ricevono "The host left". Il controllo lo fa `RoomRegistry.ExpireAway` a ogni tick.
- Se la stanza cambia piano mentre l'eroe è via, sul nuovo piano resta parcheggiato.
- Uscire con Esc chiama prima `Leave`: l'uscita è immediata, senza attesa. Chiudere la scheda invece parcheggia l'eroe per 30 secondi.
- Per accorgersi prima di una connessione morta: keep-alive a 5 secondi e timeout del client a 15 secondi (prima erano 15 e 30).

**Lato client.**
- `withAutomaticReconnect` con attese 0, 1, 2, 3, 4, 5, 5, 5 secondi: 25 secondi in tutto, meno della pazienza del server.
- Durante i tentativi compare "Connection lost · Reconnecting…". Dopo `Resume` il gioco riparte. Se il piano è lo stesso, mappa esplorata e boss incontrati restano.
- Gli altri vedono l'eroe trasparente con "reconnecting" accanto al nome.
- Se il posto non c'è più, compare il motivo sopra la scena, con il pulsante Back.

**Corretto per strada.** Quando il mondo si chiudeva (host uscito, mondo inattivo, caduta in Hardcore), il messaggio sostituiva tutta la scena. Smontare la scena 3D in quel modo generava un errore `removeChild` nella pagina. Ora il messaggio compare sopra la scena ferma, e la scena si smonta solo con Back, come quando si esce con Esc.

Verifica automatica con un proxy che taglia solo la connessione del gioco: ospite staccato 4 secondi e rientrato; host staccato 4 secondi con il mondo rimasto aperto per l'ospite; host sparito, con il mondo chiuso dopo 30 secondi; ospite staccato oltre i 30 secondi, che riceve "Your place in that world is gone".

## Pacchetto 5a — prove con gli amici

**Corsa (Shift sinistro, o il pulsante » sul telefono).** Velocità ×1,6. Mentre corri non attacchi e i mostri ti sentono da 2 caselle più lontano. Il flag viaggia nell'input (`Input(…, sprint)`), e il benvenuto porta `SprintFactor` perché la predizione usi la stessa velocità.

**Mercante e pietra del viaggio si colpiscono.** Avvicinarsi mostra solo "Strike the merchant to trade" o "Strike the waystone to travel"; il negozio e il viaggio si aprono attaccandoli da vicino. Se la pietra non ha ancora destinazioni, lo dice.

**Correnti più forti.** 3 caselle al secondo invece di 1,6. La predizione le applica anche sul client (`CurrentSpeed` nel benvenuto), così l'eroe non viene corretto di continuo mentre la corrente lo porta.

**Predizione senza rimbalzo.** La prima versione puntava alla posizione del server più un anticipo. Quando ti fermavi, l'anticipo spariva subito ma il server si muoveva ancora per un tempo di andata e ritorno: l'eroe veniva tirato indietro e poi di nuovo avanti. Ora il client tiene la storia delle proprie posizioni dell'ultimo secondo e mezzo. Ogni snapshot del server si confronta con dove il client era un tempo di andata e ritorno prima, che è il momento a cui quello snapshot corrisponde. Solo la differenza vera viene corretta, un terzo alla volta per assorbire il passo dei tick, e il disegno segue con una leggera morbidezza. Fermarsi non produce più nessuna correzione all'indietro.

La predizione conosce anche ciò che blocca l'eroe oltre ai muri: mercante, pietra del viaggio, totem (raggio `FixtureRadius` nel benvenuto) e i mostri vivi, con la stessa regola del server (si è fermati solo se ci si avvicina, ci si può sempre allontanare). Senza questo, spingendo contro la pietra il client avanzava, il server no, e ogni snapshot riportava indietro l'eroe: in prova il passo indietro massimo era di 0,15 caselle e ora è di 0,03.

**Mostri più fluidi.** Sul server la direzione di marcia dei mostri ha un po' di inerzia (`HeadingInertia` 0,6), così agli angoli non oscillano più tra la scia del percorso e la linea diretta. Sul client l'animazione di camminata resta accesa per 180 ms dopo l'ultimo passo: un mostro che si ferma e riparte non salta più tra corsa e riposo a ogni snapshot.

**Animazioni.**
- Cristalli e ostacoli rotti esplodono in schegge (azzurre o di pietra), in 3D e in 2D.
- Attacco dei mostri: oltre al cerchio rosso, un "!" sopra la testa durante la carica; al colpo, graffi rossi sull'eroe colpito. In 2D anche il cerchio di carica e lo scatto del mostro verso il bersaglio.

**Pavimento leggibile.**
- 3D: i sassi decorativi del pavimento restano, ma solo sulle caselle libere. Gli ostacoli veri sono più grandi, così si distinguono, e i mobili stanno contro il muro.
- 2D: muri più scuri, bordo d'ombra dove il pavimento tocca il muro, griglia leggera sulle caselle.

**Nebbia in 2D.** Come prima nel gioco a turni si vede solo intorno a sé: le caselle mai viste sono nere, quelle già viste ma fuori vista sono in penombra, e un'ombra circolare chiude la vista. Mostri e proiettili si vedono solo nella vista attuale.

**Prima persona.** Si esce anche con la rotella, come si entra. Con il puntatore bloccato la rotella arriva al contenitore del gioco e non alla scena, per questo prima non funzionava.

**Testi in secondi.** Rigenerazione, durata delle abilità, delle pozioni e degli effetti ora sono scritte in secondi. La rigenerazione era già al secondo: base 1 vita ogni 3 secondi e 1 mana ogni 2.

**Armi del Drago e deposito.**
- Battere il Custode (prima conquista del mondo) dà a ogni eroe del gruppo l'arma del Drago della **propria classe**, del tier della difficoltà del mondo (Normal 1, Hard 2, Hardcore 3). Se lo zaino è pieno, finisce nel deposito (`DragonReward`, `LiveRewardGranted`).
- I nuovi eroi non ricevono più le armi del Drago alla creazione. Chi le ha già le tiene.
- **Deposito dell'account**: 24 posti, dalla schermata iniziale (pulsante Deposit). Si mette un oggetto dello zaino di un eroe e lo prende un altro eroe dell'account. L'equipaggiamento indossato resta sull'eroe. Non si usa mentre l'eroe è in un mondo. Le armi legate all'account si possono depositare, ed è anche l'unico modo per proteggerle dalla morte in Hardcore.
- Salvataggio: una voce `VaultKind.Deposit` per account (`DepositSave`), nello stesso repository di eroi e mondi.

**Versione del protocollo.** Il benvenuto porta `Protocol` (`RoomHub.Protocol`, ora 2) e il client lo confronta con il suo (`liveProtocol`). Se server e pagina sono di versioni diverse, il client non entra e lo dice ("restart the server, then reload this page"). Senza questo controllo, quando il comando `Input` cambia forma (con la corsa ha preso un parametro in più) il server scarta in silenzio tutti i movimenti: l'eroe fa un passo e torna indietro, come se fosse incollato. Serve anche dopo un deploy, per le schede rimaste aperte con la versione vecchia. Un test controlla che i due numeri coincidano.

**Inquadratura.** La telecamera in terza persona torna all'angolo del gioco a turni: `(0, 9,5, 12,5)` dietro e sopra l'eroe, circa 37° sull'orizzonte, invece di `(0, 12, 9)` (circa 53°), che dava una vista quasi dall'alto. Sui telefoni in verticale resta il passaggio graduale verso la vista dall'alto, perché lo schermo stretto mostra poco davanti all'eroe.

## Pacchetto 5b — leggibilità e movimento

**Nebbia anche in 3D.** Come nel gioco a turni, le zone mai viste sono coperte dai blocchi scuri di `Shroud`, e i mostri fuori dalla vista attuale non si disegnano. La vista è la stessa del 2D e della mappa (`visibleTiles`: 7 caselle senza muri in mezzo, più la stanza intera). Il client aggiorna la nebbia solo quando scopre caselle nuove o cambia casella, non a ogni snapshot.

**Caselle visibili.** Sul pavimento esplorato tornano le linee della griglia del gioco a turni (`GridLines`), così si vede dove si può camminare.

**Angoli dei muri.** Prima l'eroe, tenendo premuta una sola direzione, si fermava contro lo spigolo di una porta se non era perfettamente allineato. Ora, se la casella davanti è libera, `Collision.Slide` con `roundsCorners` lo fa scivolare verso il centro della corsia e poi entrare. Lo fanno uguale il server e la predizione del client. Vale solo per gli eroi; i mostri seguono già il loro percorso.

**L'eroe risponde subito.** La direzione del proprio eroe ora viene dall'input e non dal server: prima l'eroe si spostava subito ma si girava solo quando arrivava lo snapshot, circa 150 ms dopo. Si gira più in fretta, e la camminata si ferma appena lasci il tasto (per gli altri resta la tolleranza di 180 ms).

**Mostri più regolari.**
- Il ciclo del server controlla l'orologio ogni 10 ms invece che ogni 50. Su Windows il timer ha una risoluzione di circa 15,6 ms: con 50 ms alcuni tick saltavano e il successivo ne faceva due insieme, e i mostri avanzavano a scatti.
- Il client disegna gli altri con 120 ms di ritardo invece di 100, e prolunga il movimento fino a 150 ms se uno snapshot arriva tardi.
- Mentre camminano, i mostri guardano dove vanno e non verso l'eroe, e quando si spingono a vicenda risultano in movimento: niente più scivolate da fermi.

## Pacchetto 5c — armi, mostri, effetti e deposito

**Zanne del Drago.** Sono un pugnale singolo a una mano: un colpo per attacco, come gli altri pugnali. Solo le asce del Drago sono doppie: colpiscono due volte e occupano tutte e due le mani (`IsTwin` e `FillsBothHands`). Senza il secondo colpo l'attacco delle zanne sale a 16 + 3 per tier, come la Lama del Drago. Le zanne già negli zaini cambiano da sole al caricamento, perché le regole e la descrizione vengono dal catalogo e non dal salvataggio.

**Ragni e boss che vibravano.** Il server muove i mostri in modo regolare (simulato tick per tick). Il tremolio veniva dal client e dalla direzione:
- l'animazione procedurale di carica scuoteva il modello avanti e indietro, e quella di colpo lo scuoteva di lato: ora la carica non scuote e il colpo è una breve spinta all'indietro;
- da vicino il mostro alternava tra "guarda dove va" e "guarda l'eroe": ora guarda dove va solo oltre un metro dalla portata (`FaceTheWayDistance`), altrimenti guarda l'eroe;
- la direzione di marcia ha un po' di inerzia (`HeadingInertia`) e si azzera quando il mostro è a portata, così non riparte di lato.

**Effetti del gioco a turni anche in tempo reale.** Tornano neve, lucciole, braci e gli altri effetti di bioma (`Weather`), la corona e l'aura rossa dei boss, l'anello di rarità dei mostri non comuni, la colonna di luce sulla propria tomba e l'aura del passaggio di livello. Usano gli stessi componenti del gioco a turni.

**L'eroe corre davvero.** In tempo reale l'eroe va a 3 caselle al secondo (12 unità), mentre la camminata `Walking_A` dei modelli KayKit, che hanno le gambe corte, copre circa 1 unità al secondo: per questo sembrava pattinare. L'eroe ora usa `Running_A`, aggiunta ad `animations.glb` da `tools/models3d/build-models.mjs`, e la velocità dell'animazione segue quella reale (`walkPace`: da 0,8× a 3×). Contro un muro o un ostacolo l'eroe smette di correre sul posto.

**Attacco immediato.** Prima l'animazione partiva solo all'arrivo dell'evento `HeroSwung` dal server (un tick più la latenza) e durava più di un secondo: il colpo si vedeva dopo che il mostro era già morto. Ora alla pressione del tasto il client fa partire subito l'attacco del proprio eroe, e ogni attacco dura 420 ms, dentro la ricarica di 0,55 s. Se poi arriva l'evento dello stesso colpo, non lo ripete.

**Equipaggiamento e deposito.** La schermata "Gear and deposit" della home mostra per l'eroe scelto:
- l'equipaggiamento indossato, con Unequip (torna nello zaino) e Store (va nel deposito);
- lo zaino, con Equip per gli oggetti indossabili e Store;
- il deposito, con Take.
Così l'eroe si prepara prima di entrare in un mondo. Endpoint nuovi: `POST api/vault/deposit/{heroId}/store-equipped/{slot}`, `/equip/{entryId}`, `/unequip/{slot}`. Non si usa mentre l'eroe è in un mondo. Se il server è più vecchio della pagina, la schermata lo dice ("restart the server, then reload this page") invece di diventare tutta nera.

## Pacchetto 5d — casse, registro, terreni e rifiniture

**Casse ad attacco.** Le casse non si aprono più passandoci sopra: sono solide come mercante, pietra e totem, e si aprono colpendole quando non c'è un mostro da mirare (`OpenChest` dentro `Attack`, stessa portata `FixtureReach`). Anche il mimic si sveglia solo quando lo colpisci. Il client le conta tra gli ostacoli della predizione e la prima volta che ti avvicini scrive "Strike the chest to open it.". `ChestReach` non esiste più.

**Registro degli eventi.** In basso a sinistra torna il registro del gioco a turni: il più recente in alto in grassetto, al massimo 7 righe, ognuna resta 30 secondi. Oltre ai messaggi di prima scrive i colpi dati e ricevuti, i critici, i mancati, le uccisioni e l'efficacia degli elementi ("weak to fire", "resists ice", "you resist ice"). Per questo `HeroHit` ora porta elemento ed efficacia come `MonsterHit`. Due messaggi uguali di fila diventano una riga sola.

**Etichette dei mostri.** Ogni mostro visibile ha di nuovo l'etichetta del gioco a turni: nome colorato per rarità con le stelle, livello, icone di fuoco e gelo e barra della vita. Il boss ha la corona e la sua etichetta compare solo dopo l'incontro.

**Boss incontrato.** Un boss conta come incontrato quando è entro 4 caselle in linea di vista, non più 7: a 7 caselle poteva essere ancora fuori dallo schermo, e l'indicatore con la corona compariva per un boss mai visto.

**Lava e acqua.** Nuovi shader:
- la lava ha una crosta scura spezzata da crepe incandescenti che pulsano, zone fuse che si muovono, bolle e un bordo scuro dove tocca il pavimento;
- l'acqua ha onde che scorrono, riflessi che si muovono sul fondo, riflessi di luce, più profondità al centro e schiuma sulle rive.

Le rive si calcolano per istanza: un attributo `banks` dice quali lati della casella toccano terra. La superficie è un piano e non più un cubo: i lati dei cubi si vedevano attraverso l'acqua trasparente come righe.

**Torce.** Nella cripta le torce sono quasi tre volte più frequenti e compaiono anche sui muri laterali (`BiomeLook.torches`). Ogni fiamma ha un alone luminoso, in tutti i biomi.

**Freccia.** La freccia va a 18 caselle al secondo invece di 14, e le proprie frecce si disegnano senza i 120 ms di ritardo dell'interpolazione: partono insieme all'animazione del tiro.

**Mostri senza scatti.** Il passaggio tra camminata e riposo dei mostri costruiti in codice (ragni, regina dei ragni e gli altri) prima era istantaneo. Un mostro a portata che si ferma e riparte di continuo cambiava posa più volte al secondo, sobbalzando e inclinandosi. Ora l'andatura sfuma in circa 150 ms (`gait`), il ritmo delle zampe segue un orologio continuo che non riparte a ogni cambio, e i mostri con molte zampe non dondolano più il corpo.

**Attacco dei compagni.** In cooperativa l'animazione d'attacco di ogni eroe ora usa la sua arma, presa dall'equipaggiamento del gruppo (`party`). Prima usava quella del giocatore locale: se tu avevi la spada, il compagno con l'arco faceva il fendente.

## Pacchetto 5e — traffico ridotto

Misurato in locale con un giocatore che si muove su un piano di 25 mostri:

| | Prima | Dopo |
|---|---|---|
| Dati per snapshot (JSON) | circa 5,7 KB | circa 0,5 KB |
| Dati al secondo, decompressi | circa 107 KB/s | circa 10 KB/s |
| Dati al secondo, sul filo (compressi) | uguale, nessuna compressione | circa 0,6 KB/s |

Come:
- **Mostri a differenze.** Ogni giocatore riceve solo i mostri cambiati dall'ultimo invio, più `Gone` con gli id spariti. Ogni 5 secondi (`KeyframeSeconds`), all'ingresso, alla ripresa e al cambio di piano arriva uno snapshot completo (`Full = true`) che rimette tutto a posto. Il server ricorda per ogni giocatore l'ultimo stato inviato (`SentMonsters`). Il client ricostruisce l'elenco completo in `expandMonsters` prima di passarlo al gioco, quindi interpolazione, scena e mappa non cambiano.
- **Niente campi vuoti.** Il protocollo SignalR non scrive più i campi `null`: gli eventi ne avevano una ventina ciascuno.
- **Due decimali.** Posizioni e direzioni hanno 2 decimali invece di 3 (un centesimo di casella).
- **Compressione WebSocket** (`permessage-deflate`). Per l'hub è attiva `DangerousEnableCompression`: "Dangerous" riguarda gli attacchi che mescolano segreti e testo dell'attaccante nello stesso messaggio, e gli snapshot non contengono segreti. Il browser la negozia da solo.
- **Server a riposo.** Senza stanze aperte il ticker controlla 4 volte al secondo invece di 100.

Il protocollo passa a 3: una pagina vecchia con un server nuovo, o il contrario, mostra il messaggio di versioni diverse.

## Pacchetto 5f — Azure Container Apps

**Perché.** Il piano gratuito di App Service ha limiti fissi: 5 WebSocket in tutto, 60 minuti di CPU e 165 MB di traffico al giorno, niente Always On. Container Apps a consumo si spegne quando nessuno gioca (minimo 0 repliche) e sotto la quota gratuita mensile non costa niente.

**Come è fatto.**
- **Immagine.** Il `Dockerfile` nella radice compila il client React (Node 22), pubblica l'API (.NET 10) e mette il client in `wwwroot`. Gira come utente non root sulla porta 8080. `.dockerignore` tiene fuori test, documenti, salvataggi locali e file con credenziali.
- **Workflow** (`.github/workflows/container.yml`). Parte a ogni push su `main` o a mano. `dev` resta il ramo di lavoro, controllato da `ci.yml`; il vecchio `deploy.yml` per App Service è stato tolto. Fa tre cose:
  1. lint e test del client e del server;
  2. immagine su GitHub Container Registry (`ghcr.io/tommicasti/vaultrift`, tag con lo SHA e `latest`);
  3. `az containerapp update` con l'immagine nuova. Questo passo usa OIDC: niente password salvate, Azure si fida dei token di GitHub per l'ambiente `container`, e gira solo se la variabile `CONTAINER_APP_NAME` è impostata.
- **App.** Una replica al massimo (le stanze vivono in memoria), minimo 0, 0,25 vCPU e 0,5 GiB. Senza Log Analytics, così non ci sono costi di log.
- **Login che sopravvivono ai riavvii.** I cookie di accesso sono cifrati con le chiavi di ASP.NET Data Protection. In un container le chiavi sparirebbero a ogni riaccensione e tutti verrebbero disconnessi. Con Cosmos le chiavi stanno nel container `accounts`, partizione `keyring` (`CosmosKeyRing`, `KeyRingRepository`), e il nome dell'applicazione è fisso (`Vaultrift`), così le chiavi valgono per qualunque server.
- **Spegnimento.** Con l'arresto le stanze si chiudono e i salvataggi in coda vengono scritti, nei 30 secondi che Container Apps concede.

**Attenzione:** due server sullo stesso Cosmos (App Service e Container Apps) non sanno l'uno dell'altro. Un eroe aperto su entrambi verrebbe sovrascritto. Quando Container Apps è pronto, la Web App va fermata.

## Pagina Players (solo per i custodi)

Nella home un account custode ha il pulsante **Players**. Apre un resoconto di tutti gli account, già tradotto, senza id:
- **account:** nome, data di iscrizione, ultima volta visto, se sta giocando adesso, ore di gioco totali;
- **trofei** sbloccati, con descrizione e data al passaggio del mouse;
- **eroi:** nome, classe, livello, monete, ore, cadute, se è in un mondo, equipaggiamento per slot con la rarità, oggetti nello zaino;
- **mondi:** difficoltà, classe, piano attuale e più profondo, bioma, conquistato, aperto adesso, ore;
- **deposito:** numero di oggetti, più l'elenco dei leggendari, esotici e platino;
- **eroi a turni** rimasti dal vecchio gioco: livello, piano, turni, monete, morti, ore, perso o conquistato.

Chi è custode lo decide la configurazione `Admin:Accounts` (in Azure la variabile `Admin__Accounts`): nomi utente separati da virgola. Per tutti gli altri l'endpoint `GET /api/admin/players` risponde 404, come se non esistesse.

Il resoconto legge soltanto: usa `VaultService.Peek`, `PeekHero` e `PeekWorld`, che non convertono i vecchi salvataggi a turni. Così aprire la pagina non cambia gli account degli altri. Per elencare gli account c'è `IAccountRepository.All()`.
