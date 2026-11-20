# Vaultrift su Azure

Guida passo passo, scritta mentre la configurazione veniva fatta davvero: segue le schermate del portale in italiano e segnala i punti in cui qualcosa è andato diversamente dal previsto.

Architettura:

- **Azure App Service F1** (gratuito, Linux) serve l'API e il client React dallo stesso indirizzo;
- **Azure SQL Database, offerta gratuita** per i salvataggi;
- **GitHub Actions** pubblica a ogni merge su `main`;
- un **budget** con avvisi via mail come rete di sicurezza.

In locale non serve nulla di tutto questo: i salvataggi finiscono in `src/Vaultrift.Api/saves/` come file JSON.

Risorse create:

| Risorsa | Nome | Regione |
|---|---|---|
| Resource group | `rg-vaultrift` | West Europe |
| Server SQL | `sql-vaultrift-tommi` | **Sweden Central** |
| Database | `vaultrift` | Sweden Central |
| App Service plan | `plan-vaultrift` (F1, Linux) | Sweden Central |
| Web App | `vaultrift-tommi` | Sweden Central |
| Budget | `vaultrift-5eur` | account di fatturazione |

Il resource group può stare in una regione diversa dalle risorse: la sua regione indica solo dove Azure tiene le informazioni sul gruppo. Contano soltanto database e Web App, che devono stare **nella stessa regione tra loro**.

---

## 1. Sottoscrizione

1. Apri `https://portal.azure.com` scrivendolo a mano, con l'account personale (non quello aziendale).
2. Se compare l'errore *"L'account utente selezionato non esiste nel tenant 'Microsoft Services'"*, l'account non ha ancora una directory Azure: vai su `https://azure.microsoft.com/it-it/free/` e crea la sottoscrizione da lì.
3. Se la prova gratuita è già stata usata in passato, il sito propone *"Sign up for a Pay-As-You-Go subscription"* oppure, nel portale, la pagina **Aggiorna** di una sottoscrizione esistente. In entrambi i casi:
   - piano di supporto **Basic - Incluso** (gratuito), **non** Developer, Standard o Professional;
   - **Aggiorna ora**.

A consumo si paga solo ciò che supera i limiti gratuiti; F1 e il database gratuito restano gratuiti senza scadenza.

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

## 4. Database Azure SQL (offerta gratuita)

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

### Come funziona il gratuito

- **100.000 vCore-secondi** e **32 GB** al mese.
- Il database è serverless e va in **pausa automatica** quando nessuno gioca. Con l'offerta gratuita il ritardo della pausa **non è configurabile**: in *Calcolo e archiviazione* non compare.
- Da sveglio consuma almeno 0,5 vCore: 100.000 vCore-secondi sono circa **55 ore al mese di database sveglio**.
- Al risveglio la prima richiesta può impiegare fino a un minuto: la stringa di connessione ha `Connection Timeout=60` e l'API riprova per circa 70 secondi sugli errori temporanei.

---

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

- **Impostazioni app**: `Storage__Provider` = `SqlServer` (due trattini bassi).
- **Stringhe di connessione**: nome `Vaultrift`, tipo **SQLAzure**, valore:

  ```
  Server=tcp:sql-vaultrift-tommi.database.windows.net,1433;Initial Catalog=vaultrift;Persist Security Info=False;User ID=vaultriftadmin;Password=LA_TUA_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;
  ```

  Se la password contiene `;`, scrivila tra virgolette: `Password="ab;cd";`.
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
- Database → **Editor di query**, accesso con `vaultriftadmin`:
  - `docs/sql/item-catalog.sql`: crea `dbo.ItemCatalog` con nome e rarità degli oggetti. Va rieseguito quando il catalogo cambia.
  - `docs/sql/reports.sql`: panoramica dei giocatori, oggetti posseduti con rarità, classifica per rarità.
- **Costi**: Gestione costi → **Analisi dei costi**, filtro `rg-vaultrift`: deve restare a 0.

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
