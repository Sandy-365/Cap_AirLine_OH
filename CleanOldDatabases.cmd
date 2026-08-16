@echo off
:: ============================================================
:: SkyPass Airlines - Drop Old Databases Cleanup Script
:: Run this if old databases appear again in SSMS
:: ============================================================
echo.
echo [SkyPass] Dropping old/unused Airline databases...
echo.

sqlcmd -S "localhost,1433" -U Sandeep -P "Sandeep@123" -No -Q "
USE master;
DECLARE @dbs TABLE (name SYSNAME);
INSERT INTO @dbs VALUES 
    ('Airline_AdminDB'),('Airline_AgentDB'),('Airline_BaggageDB'),
    ('Airline_BookingDB'),('Airline_CheckInDB'),('Airline_FlightDB'),
    ('Airline_IdentityDB'),('Airline_NotificationDB'),('Airline_RewardDB'),('Airline_StaffDB');
DECLARE @name SYSNAME, @sql NVARCHAR(500);
DECLARE cur CURSOR FOR SELECT name FROM @dbs;
OPEN cur; FETCH NEXT FROM cur INTO @name;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @name)
    BEGIN
        SET @sql = 'ALTER DATABASE [' + @name + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [' + @name + '];';
        EXEC sp_executesql @sql;
        PRINT 'Dropped: ' + @name;
    END
    FETCH NEXT FROM cur INTO @name;
END
CLOSE cur; DEALLOCATE cur;
SELECT name AS [Remaining DBs] FROM sys.databases WHERE name LIKE 'Airline_%%' ORDER BY name;
"

echo.
echo [SkyPass] Done. Only 4 databases should remain above.
echo   - Airline_BackOfficeDB
echo   - Airline_FlightOpsDB
echo   - Airline_PassengerDB
echo   - Airline_PaymentDB
echo.
pause
