IF SUSER_ID(N'sso') IS NULL
    CREATE LOGIN [sso] WITH PASSWORD = N'$(SSO_SQL_PASSWORD)', CHECK_POLICY = OFF;
ELSE
    ALTER LOGIN [sso] WITH PASSWORD = N'$(SSO_SQL_PASSWORD)', CHECK_POLICY = OFF;
GO

IF DB_ID(N'sso') IS NULL
    CREATE DATABASE [sso];
GO

USE [sso];
GO

IF USER_ID(N'sso') IS NULL
    CREATE USER [sso] FOR LOGIN [sso];
ELSE
    ALTER USER [sso] WITH LOGIN = [sso];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS role_members
    INNER JOIN sys.database_principals AS roles
        ON roles.principal_id = role_members.role_principal_id
    INNER JOIN sys.database_principals AS users
        ON users.principal_id = role_members.member_principal_id
    WHERE roles.name = N'db_owner'
      AND users.name = N'sso'
)
    ALTER ROLE [db_owner] ADD MEMBER [sso];
GO
