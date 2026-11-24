USE [SistemaVentas];
GO

IF OBJECT_ID('dbo.Entradas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Entradas
    (
        IdEntrada int IDENTITY(1,1) NOT NULL,
        IdCompra int NOT NULL,
        Codigo varchar(50) NOT NULL,
        Estado varchar(20) NOT NULL
            CONSTRAINT DF_Entradas_Estado DEFAULT ('Emitida'),
        FechaEmision datetime2(7) NOT NULL
            CONSTRAINT DF_Entradas_FechaEmision DEFAULT (GETDATE()),

        CONSTRAINT PK_Entradas PRIMARY KEY (IdEntrada),
        CONSTRAINT UQ_Entradas_Codigo UNIQUE (Codigo),
        CONSTRAINT FK_Entradas_Compras
            FOREIGN KEY (IdCompra) REFERENCES dbo.Compras (IdCompra)
    );
END;
GO
