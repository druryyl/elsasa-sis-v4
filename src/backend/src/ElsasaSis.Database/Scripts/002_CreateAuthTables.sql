CREATE TABLE [auth].[users] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [username] VARCHAR(50) NOT NULL,
    [password_hash] VARCHAR(255) NOT NULL,
    [email] VARCHAR(100) NULL,
    [display_name] NVARCHAR(100) NOT NULL,
    [is_active] BIT NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_auth_users] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [UQ_auth_users_username] UNIQUE ([username])
);

CREATE TABLE [auth].[user_roles] (
    [user_id] UNIQUEIDENTIFIER NOT NULL,
    [role_code] VARCHAR(30) NOT NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_auth_user_roles] PRIMARY KEY CLUSTERED ([user_id], [role_code]),
    CONSTRAINT [FK_auth_user_roles_users] FOREIGN KEY ([user_id]) REFERENCES [auth].[users] ([id])
);

CREATE TABLE [auth].[refresh_tokens] (
    [id] UNIQUEIDENTIFIER NOT NULL,
    [user_id] UNIQUEIDENTIFIER NOT NULL,
    [token_hash] VARCHAR(255) NOT NULL,
    [expires_at] DATETIME2(3) NOT NULL,
    [revoked_at] DATETIME2(3) NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_auth_refresh_tokens] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [FK_auth_refresh_tokens_users] FOREIGN KEY ([user_id]) REFERENCES [auth].[users] ([id])
);

CREATE NONCLUSTERED INDEX [IX_auth_refresh_tokens_token_hash]
    ON [auth].[refresh_tokens] ([token_hash]);

CREATE NONCLUSTERED INDEX [IX_auth_refresh_tokens_user_id]
    ON [auth].[refresh_tokens] ([user_id]);

CREATE TABLE [auth].[user_actors] (
    [user_id] UNIQUEIDENTIFIER NOT NULL,
    [teacher_id] UNIQUEIDENTIFIER NULL,
    [student_id] UNIQUEIDENTIFIER NULL,
    [guardian_id] UNIQUEIDENTIFIER NULL,
    [created_at] DATETIME2(3) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_auth_user_actors] PRIMARY KEY CLUSTERED ([user_id]),
    CONSTRAINT [FK_auth_user_actors_users] FOREIGN KEY ([user_id]) REFERENCES [auth].[users] ([id])
);
