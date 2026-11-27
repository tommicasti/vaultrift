# Vaultrift su Azure

Guida passo passo, scritta mentre la configurazione veniva fatta davvero: segue le schermate del portale in italiano e segnala i punti in cui qualcosa è andato diversamente dal previsto.

Architettura:

- **Azure App Service F1** (gratuito, Linux) serve l'API e il client React dallo stesso indirizzo;
- **Azure Cosmos DB for NoSQL, livello gratuito** per salvataggi, account e trofei (fino a novembre 2026 era Azure SQL: vedi l'appendice);
- **GitHub Actions** pubblica a ogni merge su `main`;
- un **budget** con avvisi via mail come rete di sicurezza.

In locale non serve nulla di tutto questo: i salvataggi finiscono in `src/Vaultrift.Api/saves/` come file JSON.

Risorse create:

| Risorsa | Nome | Regione |
|---|---|---|
| Resource group | `rg-vaultrift` | West Europe |
| Account Cosmos DB | `cosmos-vaultrift-tommi` | **Sweden Central** |
| Database Cosmos | `vaultrift` (container `games`, `accounts` e `vault`) | Sweden Central |
| App Service plan | `plan-vaultrift` (F1, Linux) | Sweden Central |
| Web App | `vaultrift-tommi` | Sweden Central |
| Budget | `vaultrift-5eur` | account di fatturazione |

Il resource group può stare in una regione diversa dalle risorse: la sua regione indica solo dove Azure tiene le informazioni sul gruppo. Contano soltanto database e Web App, che devono stare **nella stessa regione tra loro**: ogni salvataggio attraversa quel collegamento.

---

## 1. Sottoscrizione

1. Apri `https://portal.azure.com` scrivendolo a mano, con l'account personale (non quello aziendale).
2. Se compare l'errore *"L'account utente selezionato non esiste nel tenant 'Microsoft Services'"*, l'account non ha ancora una directory Azure: vai su `https://azure.microsoft.com/it-it/free/` e crea la sottoscrizione da lì.
3. Se la prova gratuita è già stata usata in passato, il sito propone *"Sign up for a Pay-As-You-Go subscription"* oppure, nel portale, la pagina **Aggiorna** di una sottoscrizione esistente. In entrambi i casi:
   - piano di supporto **Basic - Incluso** (gratuito), **non** Developer, Standard o Professional;
   - **Aggiorna ora**.

A consumo si paga solo ciò che supera i limiti gratuiti; F1 e il livello gratuito di Cosmos DB restano gratuiti senza scadenza.

---

## 2. Budget (prima di creare qualsiasi risorsa)

1. **Gestione dei costi e fatturazione** → **Budget** → **+ Aggiungi**.
2. Nome `vaultrift-5eur`, periodo **Ogni mese**, importo **5**.
   La valuta è quella dell'account di fatturazione e non si può cambiare: nel nostro caso è **USD**.
3. Avvisi (le percentuali devono essere **tutte diverse**, anche se il tipo cambia):

   | Tipo | % |
   |---|---|
   | Costo effettivo | 50 |
   | Costo effettivo | 100 |
   | Costo stimato | 80 |

4. Destinatario: la tua mail. **Crea**.
5. In Gmail crea un filtro *Da: `azure-noreply@microsoft.com`* → **Non inviare mai a Spam**.

Il budget **avvisa**, non blocca, e i costi arrivano con qualche ora di ritardo.

---

## 3. Resource group

**Gruppi di risorse** → **+ Crea** → nome `rg-vaultrift` → **Rivedi e crea** → **Crea**.

Il nome non si può cambiare dopo: controllalo prima di confermare.

---

## 4. Azure Cosmos DB (livello gratuito)

### Perché Cosmos e non Azure SQL

L'offerta gratuita di Azure SQL dà 100.000 vCore-secondi al mese, ma il database **resta sveglio almeno un'ora dopo ogni visita** e da sveglio consuma 0,5 vCore anche senza fare nulla. Con sei amici che giocano a orari diversi le 55 ore gratuite sono finite in tre settimane e ogni giornata di gioco costava circa 2,5 $.

Il livello gratuito di Cosmos DB funziona in un altro modo: **1000 RU/s e 25 GB gratis per sempre**, senza pause e senza ore contate. Si paga solo se si chiede più throughput, e alla creazione si può vietarlo.

### Come ragiona Cosmos

- Un **account** contiene **database**, un database contiene **container**, un container contiene **documenti JSON**.
- Ogni documento ha un `id` e una **chiave di partizione** (`pk`): leggere un documento conoscendo entrambi è l'operazione più economica (1 RU per 1 KB).
- Il costo si misura in **Request Unit (RU)**. Il database `vaultrift` ha **1000 RU/s condivise** tra i due container: è esattamente il gratuito.
- Se in un secondo si chiedono più RU, Cosmos risponde 429 e l'SDK riprova da solo dopo pochi millisecondi: un turno arriva un attimo dopo, ma non si paga niente.

### Creazione

1. **Azure Cosmos DB** → **+ Crea** → **Azure Cosmos DB for NoSQL** → **Crea**. Se chiede il tipo di carico di lavoro, il portale preimposta i campi di conseguenza: da noi *Sviluppo/Test* aveva scelto **Serverless**. Qualunque tipo scegli, ricontrolla la modalità di capacità.
2. **Informazioni di base**
   - gruppo `rg-vaultrift`, nome account `cosmos-vaultrift-tommi` (minuscole, unico in tutto Azure);
   - località **Sweden Central**, la stessa della Web App;
   - zone di disponibilità **Disabilita**;
   - **Modalità di capacità: Velocità effettiva con provisioning**. **Non Serverless**: il gratuito non vale per serverless, e l'API non parte perché chiede 1000 RU/s condivise;
   - **Applica sconto livello gratuito: Applica**. Compare solo con la velocità effettiva con provisioning; si sceglie solo adesso, dopo non si può più aggiungere, e c'è un solo account gratuito per sottoscrizione;
   - **Limita la velocità effettiva totale dell'account**: casella spuntata. Così nessuno (nemmeno noi per errore) può superare le 1000 RU/s gratuite.
3. **Distribuzione globale**: ridondanza geografica, scritture in più aree e zone di disponibilità **disattivate** (costano).
4. **Rete**: **Tutte le reti**. L'app non ha un indirizzo fisso, la protezione è la chiave nella stringa di connessione.
5. **Backup**: **Periodico**, ridondanza dell'archiviazione di backup **locale**.
6. **Rivedi e crea** → controlla *Modalità di capacità: Velocità effettiva con provisioning* → **Crea** (pochi minuti). Il riepilogo **non mostra** né lo sconto né il limite: controllali nella scheda *Informazioni di base* prima di creare.
7. Account → **Impostazioni** → **Chiavi** → copia la **STRINGA DI CONNESSIONE PRIMARIA**. È una password: va solo nelle impostazioni della Web App e nella variabile d'ambiente della migrazione, mai nel codice o negli screenshot.

Non serve creare database e container a mano: all'avvio l'API crea il database `vaultrift` con 1000 RU/s condivise e i container `games`, `accounts` e `vault`, se mancano. Il terzo container condivide le stesse 1000 RU/s: non costa nulla in più.

### Come sono fatti i dati

| Container | Documento (`type`) | `id` | `pk` | Contenuto |
|---|---|---|---|---|
| `games` | `game` | Id della partita | uguale a `id` | codice, classe, nome, piano, livello, `data` (il salvataggio come testo), `stats`, `createdAt`, `updatedAt` |
| `accounts` | `account` | Id dell'account | uguale a `id` | nome, hash della password, email, data di registrazione |
| `accounts` | `name` | `name:` + nome in maiuscolo | uguale a `id` | Id dell'account: garantisce nomi unici |
| `accounts` | `owner` | `game:` + Id della partita | uguale a `id` | a quale account appartiene la partita |
| `accounts` | `trophy` | `trophy:` + trofeo | Id dell'account | data di sblocco |
| `accounts` | `counter` | `counter:` + nome | Id dell'account | valore (incrementato con una *patch* atomica) |
| `vault` | `vault` | `hero:` o `world:` + Id | Id dell'account | eroi e mondi del tempo reale: `kind`, `entryId`, `data` (il salvataggio come testo, non indicizzato), `createdAt`, `updatedAt` |

- `data` resta **testo** e non un oggetto JSON: Cosmos salva i numeri come double e lo stato del generatore casuale (`RandomState`, un intero a 64 bit) perderebbe le ultime cifre.
- `stats` copia fuori dal salvataggio morti, monete, turni, conquista e secondi di gioco, così le interrogazioni possono leggerli. `data` è escluso dall'indice per spendere meno RU a ogni salvataggio.
- Tutte le date sono in **UTC**: in Italia aggiungi un'ora (due con l'ora legale).
- Le interrogazioni pronte sono in `docs/cosmos/queries.md` (Esplora dati → container → **Nuova query SQL**).

## 5. Web App (App Service F1)

**Servizi app** → **+ Crea** → **App Web**.

- **Informazioni di base**: gruppo `rg-vaultrift`, nome `vaultrift-tommi`, **Codice**, **.NET 10 (LTS)**, **Linux**, area **uguale al database** (Sweden Central), nuovo piano `plan-vaultrift`, **Gratuito F1**.
  Con il *nome host predefinito univoco sicuro* l'indirizzo diventa lungo, per esempio `vaultrift-tommi-<casuale>.swedencentral-01.azurewebsites.net`: va bene così.
- **Istanza gestita**: spenta.
- **Database**: niente (il database c'è già).
- **Distribuzione**: distribuzione continua **disabilitata**, autenticazione di base **abilitata**.
- **Rete**: accesso pubblico attivo.
- **Monitora e proteggi**: Application Insights **No**.

Perché Linux: F1 è gratuito su entrambi, ma il primo piano a pagamento (B1) costa molto meno su Linux e il sistema operativo di un piano non si cambia. Su Linux i nomi dei file distinguono maiuscole e minuscole.

### Limiti di F1

- **60 minuti di CPU al giorno**, azzerati a mezzanotte UTC. Un turno costa circa 1-2 ms di CPU.
- **165 MB al giorno di traffico in uscita**. Un turno pesa circa 3,5 KB compresso e il client resta in cache.
- **Niente Always On**: dopo circa 20 minuti senza visite l'app si spegne e la prima richiesta la riaccende in 10-30 secondi. Le partite non si perdono.

---

## 6. Configurare l'app

### Variabili di ambiente

Web App → **Impostazioni** → **Variabili di ambiente**.

- **Impostazioni app**: `Storage__Provider` = `Cosmos` (due trattini bassi).
- **Stringhe di connessione**: nome `Cosmos`, tipo **Custom**, valore: la stringa di connessione primaria dell'account Cosmos.
- Facoltativo: `Storage__Database` cambia il nome del database (predefinito `vaultrift`).
- In fondo: **Applica** → **Conferma**.

### Configurazione → Impostazioni generali

- Credenziali di pubblicazione autenticazione di base **SCM**: attivo.
- Credenziali **FTP**: disattivo, **Stato FTP**: disattivato (non usiamo FTP).
- **Solo HTTPS**: attivo. **TLS minimo**: 1.2.
- **SSH**: attivo (console nel container per diagnosticare).

---

## 7. GitHub

1. Web App → **Panoramica** → **Scarica il profilo di pubblicazione**.
2. GitHub → repository → **Settings** → **Secrets and variables** → **Actions**:
   - **Repository secrets** → `AZURE_WEBAPP_PUBLISH_PROFILE` = contenuto completo del file;
   - **Repository variables** → `AZURE_WEBAPP_NAME` = `vaultrift-tommi`.
3. Cancella il file `.PublishSettings`. Se pensi che sia finito in mani sbagliate: Panoramica → **Reimposta profilo di pubblicazione**.

**Configura i secret prima del merge su `main`.** Il primo deploy da noi è fallito con *"No credentials found"* perché il merge era arrivato prima. Si risolve aggiungendo il secret e cliccando **Re-run failed jobs**: il re-run rilegge i secret.

Il workflow `.github/workflows/deploy.yml`:

1. builda il client React;
2. esegue tutti i test (se uno fallisce, non pubblica);
3. pubblica l'API e copia il client in `wwwroot`;
4. carica tutto sull'App Service.

**Rollback**: Actions → l'ultima esecuzione riuscita prima di quella sbagliata → **Re-run jobs**.

---

## 8. Verifica e diagnosi

- Apri l'indirizzo dell'app: la prima volta può impiegare fino a un minuto.
- Crea un eroe, premi **Menu**: deve comparire **Continue** con il codice di salvataggio.
- Errori: Web App → **Monitoraggio** → **Flusso di log**. Un salvataggio fallito appare come `Could not save game`, un salvataggio non più leggibile come `Save ... could not be loaded`.
- Cosmos → **Esplora dati**: le interrogazioni di `docs/cosmos/queries.md`. Ogni interrogazione mostra quante RU è costata.
- **Consumo di RU**: Cosmos → **Monitoraggio** → **Metriche** → *Normalized RU Consumption* (massimo). Se resta spesso al 100%, i turni rallentano: si può ridurre il peso dei salvataggi.
- **Dettaglio per operazione**: impostazione app `Logging__LogLevel__Vaultrift.Cosmos` = `Debug`, poi **Flusso di log** mostra righe come `games set cost 18.4 RU`. Toglila dopo la misura: scrive una riga per ogni salvataggio.
- **Costi**: Gestione costi → **Analisi dei costi**, filtro `rg-vaultrift`: deve restare a 0.

---

## 9. Migrazione da Azure SQL a Cosmos DB

Si fa una volta sola, con il sito fermo, così nessun salvataggio va perso a metà.

1. **Prima** il codice con Cosmos deve essere su `main` e pubblicato. Una versione vecchia dell'API con `Storage__Provider` = `Cosmos` non parte.
2. Crea l'account Cosmos (sezione 4).
3. Web App → **Arresta**.
4. Server SQL → **Rete** → **Aggiungi l'indirizzo IP client corrente** → **Salva** (il tuo IP di casa può essere cambiato).
5. Dalla cartella del repository, in PowerShell:

   ```powershell
   $env:VAULTRIFT_SQL = "<stringa di connessione SQL, con la password>"
   $env:VAULTRIFT_COSMOS = "<stringa di connessione primaria Cosmos>"
   dotnet run --project tools/Vaultrift.Migrate
   Remove-Item Env:VAULTRIFT_SQL, Env:VAULTRIFT_COSMOS
   ```

   Le stringhe stanno solo in quella finestra di PowerShell e spariscono con l'ultima riga. Il programma copia partite, account, collegamenti, trofei e contatori, rilegge ogni partita e ogni account per controllare la copia e alla fine stampa i totali. Si può rilanciare: sovrascrive con gli stessi dati.
6. Confronta i totali con quelli di SQL (Editor di query: `SELECT COUNT(*) FROM dbo.SavedGames`, e lo stesso per `dbo.Accounts`).
7. Web App → **Variabili di ambiente** come nella sezione 6 (`Storage__Provider` = `Cosmos` e stringa `Cosmos`). Lascia per ora la stringa `Vaultrift` di SQL.
8. Web App → **Avvia**. Entra con il tuo account, controlla eroi e trofei, gioca qualche turno.

**Tornare indietro**: `Storage__Provider` = `SqlServer`. Le partite giocate nel frattempo su Cosmos non ci sono su SQL.

**Dopo una settimana senza problemi**: elimina il database e il server SQL, poi la stringa di connessione `Vaultrift` dalla Web App. Da lì il costo del database è zero.

---

## Alternativa senza database

| Name | Value |
|---|---|
| `Storage__Provider` | `File` |
| `Storage__Folder` | `/home/data/saves` |

`/home` è il disco persistente dell'App Service. Non usare `/home/site/wwwroot`: viene svuotata a ogni deploy.

---

## Come ragiona il salvataggio

- Ogni partita ha un generatore `SeededRandom` il cui stato è un solo numero: salvandolo insieme alla fotografia della partita (`GameSnapshot`), al caricamento i dadi riprendono esattamente da dove si erano fermati.
- Dopo ogni azione l'API scrive la partita intera (circa 17 KB di JSON). Se la scrittura fallisce la partita continua in memoria e il turno successivo riprova.
- Un salvataggio che questa versione non sa più leggere (formato cambiato, oggetto rimosso dal catalogo) produce una risposta **409** e il client mostra un messaggio chiaro invece di un errore generico.
- Il codice di salvataggio (`XXXX-XXXX`) usa 30 caratteri senza quelli ambigui (`0 O 1 I L`).

---

## Appendice: Azure SQL (usato fino a novembre 2026)

Resta qui per riferimento finché il database non viene eliminato. Il provider `SqlServer` esiste ancora nel codice.

### Creazione del database SQL (offerta gratuita)

1. **Database SQL** → **+ Crea** → **Database SQL (offerta gratuita)**.
   Non scegliere "Database SQL": quello è a pagamento.
2. Clicca subito **Configurazione avanzata**: la pagina semplificata non mostra né la rete né il comportamento a fine limite.
3. **Informazioni di base**
   - Gruppo di risorse `rg-vaultrift`, nome database `vaultrift`.
   - **Server → Crea nuovo**:
     - nome `sql-vaultrift-tommi`;
     - località: **West Europe rifiutava nuovi server** per la nostra sottoscrizione ("La sottoscrizione non è autorizzata a creare un server nell'area selezionata"). Abbiamo usato **Sweden Central**; alternative: North Europe, Italy North, Germany West Central;
     - **Usa l'autenticazione SQL**, amministratore `vaultriftadmin`;
     - password **nuova e scritta a mano**. Attenzione al **riempimento automatico del browser**: nel nostro caso aveva inserito l'email personale come amministratore. Un utente con `@` rompe la connessione e la password personale finirebbe nella configurazione dell'app. Se i campi hanno lo sfondo azzurro, li ha riempiti il browser.
   - **Behavior when free offer limit reached**: **Continue using database for additional charges**. La scelta è **definitiva** per questo database.
   - Calcolo: *Utilizzo generico - Serverless*, 32 GB, impostato dall'offerta. Non toccarlo.
   - Backup: **ridondanza locale** (l'unica inclusa).
4. **Rete**
   - **Endpoint pubblico**;
   - **Consenti alle risorse e ai servizi di Azure di accedere a questo server: Sì** (senza, l'app non salva);
   - **Aggiungi indirizzo IP client corrente: Sì**.
   - Se ricrei il server dalla scheda Informazioni di base, **ricontrolla questa scheda**: da noi si era rimesso su *No*.
5. **Sicurezza**: Microsoft Defender per SQL **Non ora** (dopo la prova costa 15 $ al mese).
6. **Impostazioni aggiuntive**: lascia tutto (database vuoto, regole di confronto predefinite).
7. **Rivedi e crea** → controlla amministratore, regione, Overage billing e rete → **Crea** (2-5 minuti).

Non serve creare tabelle: alla prima partita l'API crea `dbo.SavedGames`.

#### Come funziona il gratuito

- **100.000 vCore-secondi** e **32 GB** al mese.
- Il database è serverless e va in **pausa automatica** quando nessuno gioca. Con l'offerta gratuita il ritardo della pausa **non è configurabile**: in *Calcolo e archiviazione* non compare.
- Da sveglio consuma almeno 0,5 vCore: 100.000 vCore-secondi sono circa **55 ore al mese di database sveglio**.
- Al risveglio la prima richiesta può impiegare fino a un minuto: la stringa di connessione ha `Connection Timeout=60` e l'API riprova per circa 70 secondi sugli errori temporanei.

Configurazione della Web App con SQL: `Storage__Provider` = `SqlServer` e stringa di connessione `Vaultrift`, tipo **SQLAzure**:

```
Server=tcp:sql-vaultrift-tommi.database.windows.net,1433;Initial Catalog=vaultrift;Persist Security Info=False;User ID=vaultriftadmin;Password=LA_TUA_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;
```

Le interrogazioni SQL sono in `docs/sql/`.
