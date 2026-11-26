# Interrogazioni Cosmos DB

Si lanciano da **Cosmos → Esplora dati → vaultrift → container → Nuova query SQL**. Il container è indicato sopra ogni interrogazione.

Differenze rispetto alle interrogazioni SQL di `docs/sql/`:

- **niente join tra container**: per vedere il nome dell'account accanto a una partita servono due interrogazioni;
- `ORDER BY` funziona solo da solo, non insieme a `GROUP BY`;
- le date sono in **UTC**;
- il contenuto dello zaino resta dentro `data` (testo), quindi le classifiche per rarità degli oggetti non sono disponibili.

## Partite, dalla più recente (`games`)

```sql
SELECT c.heroName AS Eroe, c.classId AS Classe, c.level AS Livello, c.floor AS Piano,
       c.stats.deaths AS Morti, c.stats.coins AS Monete, c.stats.turns AS Turni,
       c.stats.conquered AS Conquistato, c.code AS Codice,
       c.createdAt AS Iniziata, c.updatedAt AS UltimaVolta
FROM c
WHERE c.type = 'game'
ORDER BY c.updatedAt DESC
```

## Ore di gioco per partita (`games`)

```sql
SELECT c.heroName AS Eroe, c.classId AS Classe, c.level AS Livello, c.floor AS Piano,
       FLOOR(c.stats.playSeconds / 3600) AS Ore,
       FLOOR((c.stats.playSeconds % 3600) / 60) AS Minuti,
       c.stats.turns AS Turni, c.code AS Codice, c.updatedAt AS UltimaVolta
FROM c
WHERE c.type = 'game'
ORDER BY c.stats.playSeconds DESC
```

## Totali per nome dell'eroe (`games`)

```sql
SELECT c.heroName AS Eroe, COUNT(1) AS Partite,
       SUM(c.stats.playSeconds) AS Secondi, SUM(c.stats.turns) AS Turni
FROM c
WHERE c.type = 'game'
GROUP BY c.heroName
```

## Chi ha giocato oggi (`games`)

```sql
SELECT c.heroName AS Eroe, c.floor AS Piano, c.level AS Livello, c.updatedAt AS UltimaVolta
FROM c
WHERE c.type = 'game' AND c.updatedAt >= '2026-11-26T00:00:00'
ORDER BY c.updatedAt DESC
```

Cambia la data a mano.

## Account, dal più recente (`accounts`)

```sql
SELECT c.userName AS Account, c.email AS Email, c.createdAt AS Registrato
FROM c
WHERE c.type = 'account'
ORDER BY c.createdAt DESC
```

## Partite di un account (`accounts`, poi `games`)

Prima trova l'Id dell'account:

```sql
SELECT c.accountId FROM c WHERE c.type = 'name' AND c.id = 'name:ZEG'
```

Il nome va in maiuscolo. Poi le sue partite, dalla più recente:

```sql
SELECT c.gameId, c.linkedAt FROM c
WHERE c.type = 'owner' AND c.accountId = '<accountId>'
ORDER BY c.linkedAt DESC
```

E nel container `games`, per ogni `gameId`:

```sql
SELECT c.heroName, c.floor, c.level, c.updatedAt FROM c WHERE c.id = '<gameId>'
```

## Trofei sbloccati, dal più recente (`accounts`)

```sql
SELECT c.accountId, c.trophyId AS Trofeo, c.unlockedAt AS Sbloccato
FROM c
WHERE c.type = 'trophy'
ORDER BY c.unlockedAt DESC
```

## Trofei e monete per account (`accounts`)

```sql
SELECT c.pk AS accountId, COUNT(1) AS Trofei
FROM c
WHERE c.type = 'trophy'
GROUP BY c.pk
```

```sql
SELECT c.accountId, c["value"] AS MoneteRaccolte
FROM c
WHERE c.type = 'counter' AND c.name = 'coins'
```
