
-- Reverse Proxy 設定資料表（SQL Server / MSSQL）
CREATE TABLE dbo.proxy_cluster (
    cluster_id NVARCHAR(128) NOT NULL PRIMARY KEY,
    cluster_name NVARCHAR(128) NULL,
    is_enabled BIT NOT NULL CONSTRAINT DF_proxy_cluster_is_enabled DEFAULT (1),
    created_at DATETIME2 NOT NULL CONSTRAINT DF_proxy_cluster_created_at DEFAULT (SYSUTCDATETIME()),
    updated_at DATETIME2 NOT NULL CONSTRAINT DF_proxy_cluster_updated_at DEFAULT (SYSUTCDATETIME())
);

CREATE TABLE dbo.proxy_destination (
    destination_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    cluster_id NVARCHAR(128) NOT NULL,
    destination_name NVARCHAR(128) NOT NULL,
    destination_address NVARCHAR(500) NOT NULL,
    is_enabled BIT NOT NULL CONSTRAINT DF_proxy_destination_is_enabled DEFAULT (1),
    created_at DATETIME2 NOT NULL CONSTRAINT DF_proxy_destination_created_at DEFAULT (SYSUTCDATETIME()),
    updated_at DATETIME2 NOT NULL CONSTRAINT DF_proxy_destination_updated_at DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_proxy_destination_cluster FOREIGN KEY (cluster_id) REFERENCES dbo.proxy_cluster(cluster_id)
);

CREATE TABLE dbo.proxy_route (
    route_name NVARCHAR(128) NOT NULL PRIMARY KEY,
    cluster_id NVARCHAR(128) NOT NULL,
    route_order INT NOT NULL CONSTRAINT DF_proxy_route_order DEFAULT (100),
    match_path NVARCHAR(256) NULL,
    match_hosts NVARCHAR(500) NULL,
    is_enabled BIT NOT NULL CONSTRAINT DF_proxy_route_is_enabled DEFAULT (1),
    created_at DATETIME2 NOT NULL CONSTRAINT DF_proxy_route_created_at DEFAULT (SYSUTCDATETIME()),
    updated_at DATETIME2 NOT NULL CONSTRAINT DF_proxy_route_updated_at DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_proxy_route_cluster FOREIGN KEY (cluster_id) REFERENCES dbo.proxy_cluster(cluster_id)
);

-- 範例資料：將原本 appsettings.json 中的 reverse proxy 設定搬到 SQL
INSERT INTO dbo.proxy_cluster (cluster_id, cluster_name, is_enabled)
VALUES
    (N'target-web1', N'target-web1', 1),
    (N'target-web2', N'target-web2', 1);

INSERT INTO dbo.proxy_destination (cluster_id, destination_name, destination_address, is_enabled)
VALUES
    (N'target-web1', N'destination1', N'http://sso-poc-target-web1/', 1),
    (N'target-web2', N'destination1', N'http://sso-poc-target-web2/', 1);

INSERT INTO dbo.proxy_route (route_name, cluster_id, route_order, match_path, match_hosts, is_enabled)
VALUES
    (N'test1-route', N'target-web1', 100, N'/{**catch-all}', N'test1.gd.com', 1),
    (N'test2-route', N'target-web2', 100, N'/{**catch-all}', N'test2.gd.com', 1);

CREATE TRIGGER dbo.trg_proxy_cluster_updated
ON dbo.proxy_cluster
AFTER UPDATE
AS
BEGIN
    UPDATE c
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.proxy_cluster c
    INNER JOIN inserted i ON i.cluster_id = c.cluster_id;
END;

CREATE TRIGGER dbo.trg_proxy_destination_updated
ON dbo.proxy_destination
AFTER UPDATE
AS
BEGIN
    UPDATE d
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.proxy_destination d
    INNER JOIN inserted i ON i.destination_id = d.destination_id;
END;

CREATE TRIGGER dbo.trg_proxy_route_updated
ON dbo.proxy_route
AFTER UPDATE
AS
BEGIN
    UPDATE r
    SET updated_at = SYSUTCDATETIME()
    FROM dbo.proxy_route r
    INNER JOIN inserted i ON i.route_name = r.route_name;
END;

-- CRUD Stored Procedures（完整管理介面使用）
CREATE OR ALTER PROCEDURE dbo.sp_proxy_cluster_upsert
    @cluster_id NVARCHAR(128),
    @cluster_name NVARCHAR(128) = NULL,
    @is_enabled BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.proxy_cluster WHERE cluster_id = @cluster_id)
    BEGIN
        UPDATE dbo.proxy_cluster
        SET cluster_name = @cluster_name,
            is_enabled = @is_enabled,
            updated_at = SYSUTCDATETIME()
        WHERE cluster_id = @cluster_id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.proxy_cluster (cluster_id, cluster_name, is_enabled)
        VALUES (@cluster_id, @cluster_name, @is_enabled);
    END
END;

CREATE OR ALTER PROCEDURE dbo.sp_proxy_cluster_delete
    @cluster_id NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.proxy_route WHERE cluster_id = @cluster_id;
    DELETE FROM dbo.proxy_destination WHERE cluster_id = @cluster_id;
    DELETE FROM dbo.proxy_cluster WHERE cluster_id = @cluster_id;
END;

CREATE OR ALTER PROCEDURE dbo.sp_proxy_destination_upsert
    @cluster_id NVARCHAR(128),
    @destination_name NVARCHAR(128),
    @destination_address NVARCHAR(500),
    @is_enabled BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.proxy_destination WHERE cluster_id = @cluster_id AND destination_name = @destination_name)
    BEGIN
        UPDATE dbo.proxy_destination
        SET destination_address = @destination_address,
            is_enabled = @is_enabled,
            updated_at = SYSUTCDATETIME()
        WHERE cluster_id = @cluster_id AND destination_name = @destination_name;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.proxy_destination (cluster_id, destination_name, destination_address, is_enabled)
        VALUES (@cluster_id, @destination_name, @destination_address, @is_enabled);
    END
END;

CREATE OR ALTER PROCEDURE dbo.sp_proxy_destination_delete
    @cluster_id NVARCHAR(128),
    @destination_name NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.proxy_destination
    WHERE cluster_id = @cluster_id AND destination_name = @destination_name;
END;

CREATE OR ALTER PROCEDURE dbo.sp_proxy_route_upsert
    @route_name NVARCHAR(128),
    @cluster_id NVARCHAR(128),
    @route_order INT = 100,
    @match_path NVARCHAR(256) = NULL,
    @match_hosts NVARCHAR(500) = NULL,
    @is_enabled BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.proxy_route WHERE route_name = @route_name)
    BEGIN
        UPDATE dbo.proxy_route
        SET cluster_id = @cluster_id,
            route_order = @route_order,
            match_path = @match_path,
            match_hosts = @match_hosts,
            is_enabled = @is_enabled,
            updated_at = SYSUTCDATETIME()
        WHERE route_name = @route_name;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.proxy_route (route_name, cluster_id, route_order, match_path, match_hosts, is_enabled)
        VALUES (@route_name, @cluster_id, @route_order, @match_path, @match_hosts, @is_enabled);
    END
END;

CREATE OR ALTER PROCEDURE dbo.sp_proxy_route_delete
    @route_name NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.proxy_route WHERE route_name = @route_name;
END;

-- CRUD 查詢範例：
-- SELECT cluster_id, cluster_name, is_enabled FROM dbo.proxy_cluster ORDER BY cluster_name;
-- SELECT route_name, cluster_id, route_order, match_path, match_hosts FROM dbo.proxy_route WHERE is_enabled = 1 ORDER BY route_order;
-- SELECT cluster_id, destination_name, destination_address FROM dbo.proxy_destination WHERE is_enabled = 1 ORDER BY cluster_id, destination_name;