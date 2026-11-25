USE [SistemaVentas];
GO

-- Estado de la compra para anulaciones y reembolsos
ALTER TABLE dbo.Compras
ADD Estado varchar(20) NOT NULL
        CONSTRAINT DF_Compras_Estado DEFAULT ('Pagada');
GO

-- Entradas digitales generadas después del pago
CREATE TABLE dbo.Entradas
(
    IdEntrada int IDENTITY(1,1) NOT NULL,
    IdCompra int NOT NULL,
    Codigo varchar(50) NOT NULL,
    Estado varchar(20) NOT NULL
        CONSTRAINT DF_Entradas_Estado DEFAULT ('Emitida'),
    FechaEmision datetime2(7) NOT NULL
        CONSTRAINT DF_Entradas_FechaEmision DEFAULT (GETDATE()),
    FechaUso datetime2(7) NULL,

    CONSTRAINT PK_Entradas PRIMARY KEY (IdEntrada),
    CONSTRAINT UQ_Entradas_Codigo UNIQUE (Codigo),
    CONSTRAINT FK_Entradas_Compras
        FOREIGN KEY (IdCompra) REFERENCES dbo.Compras (IdCompra),
    CONSTRAINT CK_Entradas_Estado
        CHECK (Estado IN ('Emitida', 'Usada', 'Anulada'))
);
GO

-- Solicitudes de anulación y reembolso simulado
CREATE TABLE dbo.SolicitudesAnulacion
(
    IdSolicitud int IDENTITY(1,1) NOT NULL,
    IdCompra int NOT NULL,
    Motivo varchar(500) NOT NULL,
    Estado varchar(20) NOT NULL
        CONSTRAINT DF_SolicitudesAnulacion_Estado DEFAULT ('Solicitada'),
    FechaSolicitud datetime2(7) NOT NULL
        CONSTRAINT DF_SolicitudesAnulacion_Fecha DEFAULT (GETDATE()),
    FechaResolucion datetime2(7) NULL,
    MontoReembolso decimal(10,2) NULL,

    CONSTRAINT PK_SolicitudesAnulacion PRIMARY KEY (IdSolicitud),
    CONSTRAINT FK_SolicitudesAnulacion_Compras
        FOREIGN KEY (IdCompra) REFERENCES dbo.Compras (IdCompra),
    CONSTRAINT CK_SolicitudesAnulacion_Estado
        CHECK (Estado IN ('Solicitada', 'Aprobada', 'Rechazada')),
    CONSTRAINT CK_SolicitudesAnulacion_Monto
        CHECK (MontoReembolso IS NULL OR MontoReembolso >= 0)
);
GO
