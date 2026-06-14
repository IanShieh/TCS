# DB 變更待執行指令(2026-06-14)

對應分支 `feat/other-license-codes-and-nullable-hours`、設計文件
`docs/superpowers/specs/2026-06-14-other-license-codes-and-nullable-hours-design.md`。

此專案為 DB-first(`InitialCreate.Up` 為空,DB 已存在)。以下指令請於目標資料庫
(`DB_25_0507`)手動執行。**程式合併上線前務必先完成這兩項變更**,否則:

- 新增「其他」會在 INSERT 時因舊 FK(`TCSTA.TA002 → TCSMA.MA001`)失敗。
- TA004 為 null 的單頭會因欄位 `NOT NULL` 而寫入失敗。

建議在交易中執行並先備份。

```sql
USE DB_25_0507;
GO

-- 1) 移除 TCSTA.TA002 → TCSMA.MA001 的外鍵
--    (FK 名稱可能因環境而異,以下動態查出後 DROP)
DECLARE @fk sysname;
SELECT @fk = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c
  ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'TCSTA'
  AND c.name = 'TA002';

IF @fk IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'ALTER TABLE TCSTA DROP CONSTRAINT ' + QUOTENAME(@fk) + N';';
    EXEC sp_executesql @sql;
    PRINT 'Dropped FK: ' + @fk;
END
ELSE
    PRINT 'No FK on TCSTA.TA002 found (already removed).';
GO

-- 2) TA004(Hours)改為可為 null,與 MA004 一致
ALTER TABLE TCSTA ALTER COLUMN TA004 int NULL;
GO
```

## 驗證

```sql
-- FK 應已不存在
SELECT fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'TCSTA' AND c.name = 'TA002';

-- TA004 應為 nullable(is_nullable = 1)
SELECT name, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('TCSTA') AND name = 'TA004';
```
