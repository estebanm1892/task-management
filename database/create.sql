SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id INT IDENTITY(1, 1) NOT NULL,
        Name NVARCHAR(120) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        NormalizedEmail NVARCHAR(254) NOT NULL,
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );
END;

IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tasks
    (
        Id INT IDENTITY(1, 1) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        UserId INT NOT NULL,
        CreatedAt DATETIME2(7) NOT NULL,
        AdditionalInfo NVARCHAR(MAX) NULL,
        CONSTRAINT PK_Tasks PRIMARY KEY (Id),
        CONSTRAINT FK_Tasks_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_Tasks_Status CHECK (Status IN ('Pending', 'InProgress', 'Done')),
        CONSTRAINT CK_Tasks_AdditionalInfo_IsJson CHECK
            (AdditionalInfo IS NULL OR ISJSON(AdditionalInfo) = 1),
        CONSTRAINT CK_Tasks_AdditionalInfo_Priority CHECK
            (JSON_VALUE(CASE WHEN ISJSON(AdditionalInfo) = 1 THEN AdditionalInfo ELSE N'{}' END, '$.priority')
                IS NULL OR
             JSON_VALUE(CASE WHEN ISJSON(AdditionalInfo) = 1 THEN AdditionalInfo ELSE N'{}' END, '$.priority')
                IN ('Low', 'Medium', 'High'))
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_NormalizedEmail'
               AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_NormalizedEmail ON dbo.Users (NormalizedEmail);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tasks_UserId_Status_CreatedAt'
               AND object_id = OBJECT_ID(N'dbo.Tasks'))
BEGIN
    CREATE INDEX IX_Tasks_UserId_Status_CreatedAt
        ON dbo.Tasks (UserId, Status, CreatedAt DESC, Id DESC);
END;

COMMIT TRANSACTION;

DECLARE @UserId INT = 1;
DECLARE @Status NVARCHAR(20) = N'Pending';
DECLARE @Priority NVARCHAR(10) = N'High';

SELECT
    t.Id,
    t.Title,
    t.Status,
    t.UserId,
    t.CreatedAt,
    JSON_VALUE(t.AdditionalInfo, '$.priority') AS Priority
FROM dbo.Tasks AS t
WHERE t.UserId = @UserId
  AND (@Status IS NULL OR t.Status = @Status)
ORDER BY t.CreatedAt DESC, t.Id DESC;

SELECT t.Id, t.Title, JSON_VALUE(t.AdditionalInfo, '$.priority') AS Priority
FROM dbo.Tasks AS t
WHERE JSON_VALUE(t.AdditionalInfo, '$.priority') = @Priority
ORDER BY t.CreatedAt DESC, t.Id DESC;

SELECT t.Id, JSON_QUERY(t.AdditionalInfo, '$.tags') AS Tags
FROM dbo.Tasks AS t
WHERE JSON_QUERY(t.AdditionalInfo, '$.tags') IS NOT NULL;

SELECT t.Id, tags.[value] AS Tag
FROM dbo.Tasks AS t
CROSS APPLY OPENJSON(t.AdditionalInfo, '$.tags') AS tags;

DECLARE @JsonModifyDemo NVARCHAR(MAX) = N'{"priority":"Medium","tags":["backend"]}';

SELECT JSON_MODIFY(@JsonModifyDemo, '$.priority', N'High') AS UpdatedJson;
