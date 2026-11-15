-- =============================================================================
-- GroceryHub — Least-Privilege Application Database Login & User Setup
-- Run this script as 'sa' or database administrator after database creation
-- =============================================================================

USE [master];
GO

-- 1. Create SQL Server server-level login if it does not exist
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'groceryhub_app')
BEGIN
    -- NOTE: Replace $(APP_LOGIN_PASSWORD) or placeholder with a strong random secret (>= 32 chars)
    CREATE LOGIN [groceryhub_app] 
    WITH PASSWORD = N'CHANGE_ME_GroceryHub_App_Strong_Password_2026!',
         DEFAULT_DATABASE = [GroceryHub],
         CHECK_EXPIRATION = OFF,
         CHECK_POLICY = ON;
    PRINT 'Server login [groceryhub_app] created successfully.';
END
ELSE
BEGIN
    PRINT 'Server login [groceryhub_app] already exists.';
END
GO

USE [GroceryHub];
GO

-- 2. Create database user mapped to server login
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'groceryhub_app')
BEGIN
    CREATE USER [groceryhub_app] FOR LOGIN [groceryhub_app];
    PRINT 'Database user [groceryhub_app] created in [GroceryHub].';
END
ELSE
BEGIN
    PRINT 'Database user [groceryhub_app] already exists in [GroceryHub].';
END
GO

-- 3. Grant least-privilege database roles
-- Allows reading and writing data across all schemas, plus executing stored procedures / functions
ALTER ROLE [db_datareader] ADD MEMBER [groceryhub_app];
ALTER ROLE [db_datawriter] ADD MEMBER [groceryhub_app];

-- Grant EXECUTE permission on database schema objects
GRANT EXECUTE TO [groceryhub_app];

-- Grant VIEW DATABASE STATE for connection pooling and health checks
GRANT VIEW DATABASE STATE TO [groceryhub_app];

PRINT 'Granted db_datareader, db_datawriter, EXECUTE, and VIEW DATABASE STATE to [groceryhub_app].';
GO

-- 4. Verification query
SELECT 
    dp.name AS DatabaseUser,
    dp.type_desc AS UserType,
    rp.name AS DatabaseRole
FROM sys.database_role_members rm
JOIN sys.database_principals rp ON rm.role_principal_id = rp.principal_id
JOIN sys.database_principals dp ON rm.member_principal_id = dp.principal_id
WHERE dp.name = N'groceryhub_app';
GO
