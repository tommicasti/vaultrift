SELECT
    s.HeroName AS Eroe,
    s.ClassId AS Classe,
    s.Level AS Livello,
    s.Floor AS Piano,
    CAST(JSON_VALUE(s.Data, '$.Game.Deaths') AS INT) AS Morti,
    CAST(JSON_VALUE(s.Data, '$.Game.Player.Coins') AS INT) AS Monete,
    CAST(JSON_VALUE(s.Data, '$.Game.TurnNumber') AS INT) AS Turni,
    JSON_VALUE(s.Data, '$.Game.IsConquered') AS Conquistato,
    s.Code AS Codice,
    CONVERT(VARCHAR(16), s.CreatedAt AT TIME ZONE 'Central European Standard Time', 120) AS Iniziata,
    CONVERT(VARCHAR(16), s.UpdatedAt AT TIME ZONE 'Central European Standard Time', 120) AS UltimaVolta
FROM dbo.SavedGames s
ORDER BY s.UpdatedAt DESC;

WITH Owned AS (
    SELECT s.Code, s.HeroName, s.UpdatedAt, CAST(N'Zaino' AS NVARCHAR(16)) AS Dove, e.ItemId, e.Element, e.Quantity
    FROM dbo.SavedGames s
    CROSS APPLY OPENJSON(s.Data, '$.Game.Player.Inventory.Entries')
        WITH (ItemId NVARCHAR(64) '$.Item.Id', Element NVARCHAR(16) '$.Item.Element', Quantity INT '$.Quantity') e
    UNION ALL
    SELECT s.Code, s.HeroName, s.UpdatedAt, CAST(q.[key] AS NVARCHAR(16)), JSON_VALUE(q.value, '$.Item.Id'), JSON_VALUE(q.value, '$.Item.Element'), 1
    FROM dbo.SavedGames s
    CROSS APPLY OPENJSON(s.Data, '$.Game.Player.Equipment') q
)
SELECT
    o.HeroName AS Eroe,
    o.Code AS Codice,
    o.Dove,
    COALESCE(c.Name, o.ItemId) AS Oggetto,
    c.Rarity AS Rarita,
    c.Kind AS Tipo,
    o.Element AS Elemento,
    o.Quantity AS Quantita
FROM Owned o
LEFT JOIN dbo.ItemCatalog c ON c.Id = o.ItemId
ORDER BY
    o.UpdatedAt DESC,
    o.Code,
    CASE c.Rarity WHEN 'Exotic' THEN 0 WHEN 'Legendary' THEN 1 WHEN 'Epic' THEN 2 WHEN 'Rare' THEN 3 ELSE 4 END,
    COALESCE(c.Name, o.ItemId);

WITH Owned AS (
    SELECT s.Code, s.HeroName, e.ItemId
    FROM dbo.SavedGames s
    CROSS APPLY OPENJSON(s.Data, '$.Game.Player.Inventory.Entries') WITH (ItemId NVARCHAR(64) '$.Item.Id') e
    UNION ALL
    SELECT s.Code, s.HeroName, JSON_VALUE(q.value, '$.Item.Id')
    FROM dbo.SavedGames s
    CROSS APPLY OPENJSON(s.Data, '$.Game.Player.Equipment') q
)
SELECT
    o.HeroName AS Eroe,
    o.Code AS Codice,
    SUM(CASE WHEN c.Rarity = 'Exotic' THEN 1 ELSE 0 END) AS Esotici,
    SUM(CASE WHEN c.Rarity = 'Legendary' THEN 1 ELSE 0 END) AS Leggendari,
    SUM(CASE WHEN c.Rarity = 'Epic' THEN 1 ELSE 0 END) AS Epici,
    SUM(CASE WHEN c.Rarity = 'Rare' THEN 1 ELSE 0 END) AS Rari,
    SUM(CASE WHEN c.Rarity = 'Common' THEN 1 ELSE 0 END) AS Comuni
FROM Owned o
JOIN dbo.ItemCatalog c ON c.Id = o.ItemId
WHERE c.Kind <> 'Potion'
GROUP BY o.HeroName, o.Code
ORDER BY Leggendari DESC, Epici DESC, Rari DESC;

SELECT
    s.HeroName AS Eroe,
    s.ClassId AS Classe,
    s.Level AS Livello,
    s.Floor AS Piano,
    CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT) / 3600 AS Ore,
    CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT) % 3600 / 60 AS Minuti,
    CAST(JSON_VALUE(s.Data, '$.Game.TurnNumber') AS INT) AS Turni,
    s.Code AS Codice,
    CONVERT(VARCHAR(16), s.UpdatedAt AT TIME ZONE 'Central European Standard Time', 120) AS UltimaVolta
FROM dbo.SavedGames s
ORDER BY CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT) DESC;

SELECT
    s.HeroName AS Eroe,
    COUNT(*) AS Partite,
    SUM(CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT)) / 3600 AS Ore,
    SUM(CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT)) % 3600 / 60 AS Minuti,
    SUM(CAST(JSON_VALUE(s.Data, '$.Game.TurnNumber') AS INT)) AS Turni
FROM dbo.SavedGames s
GROUP BY s.HeroName
ORDER BY SUM(CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT)) DESC;

SELECT
    a.UserName AS Account,
    CONVERT(VARCHAR(16), a.CreatedAt AT TIME ZONE 'Central European Standard Time', 120) AS Registrato,
    COUNT(s.Id) AS Partite,
    SUM(CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT)) / 3600 AS Ore,
    SUM(CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT)) % 3600 / 60 AS Minuti,
    MAX(s.Level) AS LivelloMassimo,
    CONVERT(VARCHAR(16), MAX(s.UpdatedAt) AT TIME ZONE 'Central European Standard Time', 120) AS UltimaVolta
FROM dbo.Accounts a
LEFT JOIN dbo.AccountGames g ON g.AccountId = a.Id
LEFT JOIN dbo.SavedGames s ON s.Id = g.GameId
GROUP BY a.UserName, a.CreatedAt
ORDER BY SUM(CAST(COALESCE(JSON_VALUE(s.Data, '$.PlaySeconds'), '0') AS BIGINT)) DESC;

SELECT
    a.UserName AS Account,
    t.TrophyId AS Trofeo,
    CONVERT(VARCHAR(16), t.UnlockedAt AT TIME ZONE 'Central European Standard Time', 120) AS Sbloccato
FROM dbo.AccountTrophies t
JOIN dbo.Accounts a ON a.Id = t.AccountId
ORDER BY t.UnlockedAt DESC;

SELECT
    a.UserName AS Account,
    COUNT(t.TrophyId) AS Trofei,
    COALESCE(MAX(CASE WHEN c.Name = 'coins' THEN c.Value END), 0) AS MoneteRaccolte
FROM dbo.Accounts a
LEFT JOIN dbo.AccountTrophies t ON t.AccountId = a.Id
LEFT JOIN dbo.AccountCounters c ON c.AccountId = a.Id AND c.Name = 'coins'
GROUP BY a.UserName
ORDER BY COUNT(t.TrophyId) DESC;
