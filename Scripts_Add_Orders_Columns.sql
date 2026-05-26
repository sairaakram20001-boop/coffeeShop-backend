USE CoffeeShopDB;
GO

IF COL_LENGTH('dbo.Orders', 'FirstName') IS NULL
    ALTER TABLE dbo.Orders ADD FirstName NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Orders', 'LastName') IS NULL
    ALTER TABLE dbo.Orders ADD LastName NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Orders', 'AddressLine2') IS NULL
    ALTER TABLE dbo.Orders ADD AddressLine2 NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Orders', 'FullAddress') IS NULL
    ALTER TABLE dbo.Orders ADD FullAddress NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Orders', 'City') IS NULL
    ALTER TABLE dbo.Orders ADD City NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.Orders', 'ConfirmedAt') IS NULL
    ALTER TABLE dbo.Orders ADD ConfirmedAt DATETIME2 NULL;
GO

SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Orders'
  AND COLUMN_NAME IN ('FirstName','LastName','AddressLine2','FullAddress','City','ConfirmedAt');
GO
