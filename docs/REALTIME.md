# Vaultrift in tempo reale

Questo documento raccoglie le decisioni prese per la versione in tempo reale e multigiocatore. Si sviluppa sul branch `feature/realtime`; `dev` e `main` restano il gioco a turni pubblicato finché la nuova versione non è pronta.

Va aggiornato a ogni traguardo: quando una decisione cambia, si corregge qui, non solo nel codice.

---

## 1. Il modello: mondi alla Valheim

- Ogni giocatore può creare uno o più **mondi**. Il mondo appartiene a chi lo crea (l'**ospite**).
- Ogni mondo ha un **codice d'invito**: chi lo riceve può entrare nel mondo con il proprio eroe.
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

Nessun eroe e nessuna partita del gioco a turni vanno persi. Al traguardo 3 una migrazione converte ogni salvataggio esistente:

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

- **Mondi con codice**: chi crea un mondo (`Create`) ne diventa l'ospite e riceve un codice di 6 caratteri (stesso alfabeto dei codici di salvataggio, senza lettere ambigue). Gli altri entrano con `Join(codice, classe, nome)`, fino a 6 eroi. Se l'ospite esce il mondo si chiude per tutti; se esce un altro, il suo eroe lascia il piano.
- **Scaling**: la salute dei mostri cresce del 60% per ogni eroe oltre il primo (`LiveRules.ExtraHeroHealth`); quando qualcuno esce torna giù mantenendo le ferite in proporzione. I mostri inseguono l'eroe più vicino.
- **Bottino condiviso** come deciso: un tiro per ogni eroe presente, a terra per tutti, lo prende chi arriva prima.
- **Snapshot per giocatore**: la parte comune (eroi, mostri, eventi, oggetti a terra) si calcola una volta per tick; ogni giocatore riceve in più solo il proprio stato dell'eroe e, quando cambia, l'elenco del gruppo con l'equipaggiamento di ciascuno, per disegnare le armi giuste sugli altri eroi.
- **Mira**: con il mouse l'eroe guarda verso il puntatore anche mentre cammina in un'altra direzione (input `aimX`, `aimY`). A terra un ventaglio chiaro mostra l'area del colpo (portata e arco di 120°, mandati dal server) e un anello bianco segna il mostro che verrebbe colpito. Senza mira (telefono) il colpo si gira da solo verso il mostro più vicino a portata.
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

### Giocare dal telefono in casa (rete locale)

1. avvia l'API come al solito da Visual Studio;
2. in `web/` lancia `npm run dev:lan` invece di `npm run dev`: Vite si mette in ascolto anche sull'indirizzo del PC nella rete di casa e stampa un indirizzo tipo `http://192.168.1.23:5173`;
3. dal telefono, collegato allo stesso Wi‑Fi, apri quell'indirizzo, accedi con un account e unisciti al mondo con il codice;
4. se il telefono non si collega, alla prima esecuzione consenti Node.js nelle reti private quando il firewall di Windows lo chiede.

Il proxy di Vite in sviluppo toglie l'attributo `Secure` dal cookie di accesso: l'API in locale risponde in HTTPS e il cookie verrebbe marcato `Secure`, ma il telefono apre la pagina in HTTP e lo rifiuterebbe. In produzione non cambia nulla, perché lì tutto è HTTPS.

Limiti noti, da affrontare nei prossimi passi:

- il proprio eroe è mostrato con lo stesso ritardo degli altri (circa 100 ms): la predizione lato client è il prossimo miglioramento della sensazione di gioco;
- i boss attaccano come gli altri mostri (carica e colpo in mischia), senza mosse proprie;
- mancano forzieri, scale (e quindi i piani successivi), tomba, mercante, pietra del viaggio;
- mancano prima persona con mira del mouse, mappa e vista 2D;
- il prototipo non salva: chiudendo il mondo gli eroi si perdono, e chi entra parte con un eroe nuovo (il salvataggio arriva con eroe e mondo, traguardo 3);
- il cambio di piano deciso dall'ospite arriverà insieme alle scale;
- le decorazioni non hanno collisioni;
- i raggi di collisione vanno tarati sulla dimensione dei modelli.
