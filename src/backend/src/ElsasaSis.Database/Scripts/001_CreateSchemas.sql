IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'auth')
    EXEC(N'CREATE SCHEMA [auth]');

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'outbox')
    EXEC(N'CREATE SCHEMA [outbox]');

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'opr')
    EXEC(N'CREATE SCHEMA [opr]');

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'std')
    EXEC(N'CREATE SCHEMA [std]');

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'tch')
    EXEC(N'CREATE SCHEMA [tch]');

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'acd')
    EXEC(N'CREATE SCHEMA [acd]');

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'fin')
    EXEC(N'CREATE SCHEMA [fin]');

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'dev')
    EXEC(N'CREATE SCHEMA [dev]');
