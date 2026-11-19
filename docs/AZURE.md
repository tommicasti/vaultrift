# Vaultrift su Azure

Guida passo passo per pubblicare Vaultrift con:

- **Azure App Service F1** (gratuito) per API e client web, sullo stesso indirizzo;
- **Azure SQL Database, offerta gratuita** per i salvataggi;
- **GitHub Actions** che pubblica a ogni merge su `main`;
- un **budget da 5 €** che ti avvisa via mail se qualcosa comincia a costare.

In locale non serve nulla di tutto questo: i salvataggi finiscono in `src/Vaultrift.Api/saves/` come file JSON.

---

## 0. Prima di iniziare

- Un account Azure con una sottoscrizione attiva (anche "Pay-As-You-Go": con la configurazione qui sotto il costo atteso è 0 €).
- Il repository `tommicasti/vaultrift` su GitHub, con i permessi di amministratore.
- Scegli **una regione** e usala per tutto, ad esempio *Italy North* o *West Europe*. Database e app nella stessa regione = latenza minima e niente traffico tra regioni.

Nomi usati nella guida (cambiali pure, ma tienili coerenti):

| Risorsa | Nome |
|---|---|
| Resource group | `rg-vaultrift` |
| Server SQL | `sql-vaultrift-<tuo-suffisso>` (deve essere unico al mondo) |
| Database | `vaultrift` |
| App Service plan | `plan-vaultrift` |
| Web App | `vaultrift-<tuo-suffisso>` (diventa `https://vaultrift-<suffisso>.azurewebsites.net`) |

---

## 1. Resource group

1. Portale Azure → cerca **Resource groups** → **Create**.
2. Subscription: la tua. Name: `rg-vaultrift`. Region: quella scelta.
3. **Review + create** → **Create**.

Il resource group è la "cartella" che contiene tutto: se un giorno vuoi cancellare il progetto, cancelli lui e sparisce tutto.

---

## 2. Database Azure SQL (offerta gratuita)

1. Cerca **SQL databases** → **Create**.
2. In alto compare il banner *"Want to try Azure SQL Database for free?"* → **Apply offer**. Se non lo vedi, hai già usato le 10 basi dati gratuite della sottoscrizione.
3. **Basics**
   - Resource group: `rg-vaultrift`
   - Database name: `vaultrift`
   - Server: **Create new**
     - Server name: `sql-vaultrift-<suffisso>`
     - Location: la regione scelta
     - Authentication method: **Use SQL authentication**
     - Server admin login: ad esempio `vaultriftadmin`
     - Password: una password robusta. **Salvala**, ti serve al punto 4.
   - **Behavior when free limit reached**: **Continue using database for additional charges**.
     Così il gioco non si blocca mai a fine mese. Il rischio di spesa è coperto dal budget del punto 6, e con il nostro carico non dovresti nemmeno avvicinarti al limite.
4. **Networking**
   - Connectivity method: **Public endpoint**
   - **Allow Azure services and resources to access this server**: **Yes** (serve all'App Service)
   - **Add current client IP address**: **Yes** (ti permette di guardare il database da casa con SSMS o Azure Data Studio)
5. **Review + create** → **Create**. Ci vogliono un paio di minuti.

Non devi creare tabelle: alla prima partita l'API crea da sola la tabella `dbo.SavedGames`.

### Come funziona il "gratuito"

- Ogni mese hai **100.000 vCore-secondi** e **32 GB**.
- Il database è *serverless*: consuma solo mentre lavora. Dopo un certo tempo senza richieste (il *auto-pause delay*, lo trovi in database → **Compute + storage**: portalo al minimo, **15 minuti**) si mette in pausa e non consuma niente.
- Quando è in pausa, la prima richiesta lo sveglia: ci mette **fino a un minuto**. L'API riprova da sola per circa 70 secondi, quindi il primo giocatore della giornata vede solo un caricamento lungo.
- A spanne: il database acceso consuma almeno 0,5 vCore, quindi 100.000 vCore-s ≈ **55 ore al mese di database sveglio**. Ogni sessione di gioco tiene sveglio il database per la sua durata più il ritardo di pausa. Con qualche amico che gioca la sera ci stai dentro; se superi, il costo è circa 0,000145 $ per vCore-secondo (un'ora extra a 0,5 vCore ≈ 0,26 $).

---

## 3. Web App (App Service F1)

1. Cerca **App Services** → **Create** → **Web App**.
2. **Basics**
   - Resource group: `rg-vaultrift`
   - Name: `vaultrift-<suffisso>`
   - Publish: **Code**
   - Runtime stack: **.NET 10 (LTS)**
   - Operating System: **Linux**
   - Region: la stessa del database
   - Linux Plan: **Create new** → `plan-vaultrift`
   - Pricing plan: **Free F1**
3. **Deployment**
   - Continuous deployment: **Disable** (lo fa il nostro workflow)
   - **Basic authentication: Enable**. Serve per il *publish profile* che usa GitHub.
4. **Review + create** → **Create**.

### Limiti di F1, in pratica

- **60 minuti di CPU al giorno**, si azzerano a mezzanotte UTC (l'1 di notte in Italia, le 2 con l'ora legale). È tempo di *processore*, non di gioco: un turno costa circa 1-2 ms di CPU, quindi un'ora di gioco ne consuma pochi secondi. Il limite si raggiunge solo con decine di persone in contemporanea.
- **165 MB al giorno di traffico in uscita**. Un turno pesa circa 3,5 KB compresso; il client (JS, CSS, sprite) si scarica una volta e poi resta in cache nel browser. Siamo larghi.
- **Niente "Always On"**: dopo circa 20 minuti senza visite l'app si spegne e la prima richiesta la riaccende in 10-30 secondi. Le partite non si perdono: sono nel database.
- Se un giorno sfori, l'app risponde con errore 403 fino alla mezzanotte UTC. Niente costi.

---

## 4. Configurare l'app

Web App → **Settings** → **Environment variables**.

### Scheda "App settings"

| Name | Value |
|---|---|
| `Storage__Provider` | `SqlServer` |

Il doppio trattino basso `__` è il modo in cui le variabili d'ambiente rappresentano la sezione `Storage:Provider` di `appsettings.json`.

### Scheda "Connection strings"

1. **Add**.
2. Name: `Vaultrift`. Type: **SQLAzure**.
3. Value: vai sul database `vaultrift` → **Settings** → **Connection strings** → scheda **ADO.NET (SQL authentication)**, copia la stringa, sostituisci `{your_password}` con la password del punto 2 e porta `Connection Timeout` a **60**:

   ```
   Server=tcp:sql-vaultrift-<suffisso>.database.windows.net,1433;Initial Catalog=vaultrift;Persist Security Info=False;User ID=vaultriftadmin;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;
   ```

4. **Apply** → **Confirm**. L'app si riavvia.

Il timeout a 60 secondi dà al database il tempo di svegliarsi dalla pausa.

### Impostazioni generali

**Settings** → **Configuration** → **General settings**:

- **HTTPS Only**: On
- **SCM Basic Auth Publishing Credentials**: On (se non l'hai attivato al punto 3)

---

## 5. Collegare GitHub

1. Web App → **Overview** → **Download publish profile**. Scarica un file `.PublishSettings`: contiene credenziali, **non committarlo**.
2. GitHub → repository → **Settings** → **Secrets and variables** → **Actions**.
3. Scheda **Secrets** → **New repository secret**
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Secret: incolla tutto il contenuto del file.
4. Scheda **Variables** → **New repository variable**
   - Name: `AZURE_WEBAPP_NAME`
   - Value: `vaultrift-<suffisso>`
5. Cancella il file `.PublishSettings` dal PC.

Da qui in poi, ogni push su `main` (cioè ogni PR da `dev` che unisci) lancia `.github/workflows/deploy.yml`:

1. builda il client React;
2. esegue tutti i test (se un test fallisce, non si pubblica niente);
3. pubblica l'API;
4. copia il client dentro `wwwroot`;
5. carica tutto sull'App Service.

Puoi lanciarlo anche a mano: **Actions** → **Deploy to Azure** → **Run workflow**.

---

## 6. Budget da 5 €

1. Cerca **Cost Management** → **Budgets** → **Add**.
2. Scope: la tua sottoscrizione.
3. Name: `vaultrift-5eur`. Reset period: **Monthly**. Amount: **5**.
4. **Alert conditions**:
   - Type *Actual*, 50 %
   - Type *Actual*, 100 %
   - Type *Forecasted*, 100 %
5. Alert recipients: la tua email.
6. **Create**.

Il budget **non blocca** la spesa: ti avvisa. I dati di costo arrivano con qualche ora di ritardo, quindi è una rete di sicurezza, non un interruttore. Se arriva una mail, guarda subito *Cost analysis* filtrando per `rg-vaultrift`.

---

## 7. Primo avvio e verifica

1. Unisci la PR su `main` e aspetta che il workflow diventi verde.
2. Apri `https://vaultrift-<suffisso>.azurewebsites.net`. La prima volta può metterci fino a un minuto (app e database si stanno svegliando).
3. Crea un eroe, fai qualche passo, premi **Menu**: deve comparire la scheda **Continue** con il codice di salvataggio.
4. Apri il sito dal telefono, inserisci il codice in **Load**: deve ripartire la stessa partita.
5. Se qualcosa non va: Web App → **Monitoring** → **Log stream**. Un errore di salvataggio compare come `Could not save game ...` con il dettaglio di SQL.

Per vedere i salvataggi: database → **Query editor (preview)** → login con l'utente admin →

```sql
SELECT Code, HeroName, ClassId, Floor, Level, UpdatedAt
FROM dbo.SavedGames
ORDER BY UpdatedAt DESC;
```

---

## Alternativa senza database

Se un giorno vuoi togliere Azure SQL, imposta solo:

| Name | Value |
|---|---|
| `Storage__Provider` | `File` |
| `Storage__Folder` | `/home/data/saves` |

`/home` è il disco persistente dell'App Service (1 GB su F1). Non usare la cartella dell'app (`/home/site/wwwroot`): viene svuotata a ogni deploy.

---

## Come ragiona il salvataggio (per chi legge il codice)

- Ogni partita ha un **seme** e un generatore `SeededRandom` il cui stato è un solo numero. Salvando lo stato insieme alla fotografia della partita (`GameSnapshot`), al caricamento i dadi riprendono esattamente da dove si erano fermati.
- Dopo ogni azione l'API scrive la partita intera (circa 17 KB di JSON) nella riga della tabella. Se la scrittura fallisce la partita continua in memoria e il turno successivo riprova.
- Le partite attive restano in memoria; si rileggono dal database solo dopo un riavvio dell'app o da un altro dispositivo.
- Il **codice di salvataggio** (`XXXX-XXXX`) usa 30 caratteri senza quelli ambigui (`0 O 1 I L`): circa 650 miliardi di combinazioni, abbastanza perché nessuno indovini il codice di un altro.
