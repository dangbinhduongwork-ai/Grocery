-- =============================================================================
-- GroceryHub — READ_COMMITTED_SNAPSHOT (RCSI) Status Verification & Enablement
-- Verifies non-blocking reader concurrency on the GroceryHub database
-- =============================================================================

USE [master];
GO

SELECT 
    name AS DatabaseName,
    is_read_committed_snapshot_on AS IsRcsiOn,
    snapshot_isolation_state_desc AS SnapshotIsolationState
FROM sys.databases
WHERE name = N'GroceryHub';
GO

-- If is_read_committed_snapshot_on = 0, execute the following to enable:
/*
ALTER DATABASE [GroceryHub] 
SET READ_COMMITTED_SNAPSHOT ON 
WITH ROLLBACK IMMEDIATE;
GO

SELECT 
    name AS DatabaseName,
    is_read_committed_snapshot_on AS IsRcsiOn
FROM sys.databases
WHERE name = N'GroceryHub';
GO
*/
