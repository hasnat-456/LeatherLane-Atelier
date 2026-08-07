IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Blogs] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Slug] nvarchar(max) NOT NULL,
    [Excerpt] nvarchar(max) NULL,
    [Content] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Image] nvarchar(max) NULL,
    [Author] nvarchar(max) NULL,
    [Tags] nvarchar(max) NOT NULL,
    [Views] int NOT NULL,
    [IsFeatured] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Blogs] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Slug] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [OriginalPrice] decimal(18,2) NULL,
    [Category] nvarchar(max) NOT NULL,
    [Subcategory] nvarchar(max) NULL,
    [Images] nvarchar(max) NOT NULL,
    [Thumbnail] nvarchar(max) NULL,
    [LeatherType] nvarchar(max) NULL,
    [Colors] nvarchar(max) NOT NULL,
    [Sizes] nvarchar(max) NOT NULL,
    [Material] nvarchar(max) NULL,
    [Gender] nvarchar(max) NULL,
    [Stock] int NOT NULL,
    [Rating] float NOT NULL,
    [NumReviews] int NOT NULL,
    [Sku] nvarchar(max) NULL,
    [Weight] nvarchar(max) NULL,
    [Dimensions] nvarchar(max) NULL,
    [CareInstructions] nvarchar(max) NULL,
    [IsNew] bit NOT NULL,
    [IsBestseller] bit NOT NULL,
    [Discount] decimal(18,2) NULL,
    [Available] bit NOT NULL,
    [Features] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [Password] nvarchar(max) NOT NULL,
    [IsVerified] bit NOT NULL,
    [VerificationOTP] nvarchar(max) NULL,
    [VerificationOTPExpires] datetime2 NULL,
    [ResetPasswordToken] nvarchar(max) NULL,
    [ResetPasswordExpiry] datetime2 NULL,
    [Role] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [ProductSpecification] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Value] nvarchar(max) NOT NULL,
    [ProductId] int NOT NULL,
    CONSTRAINT [PK_ProductSpecification] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductSpecification_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CartItems] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CartItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CartItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Reviews] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Rating] int NOT NULL,
    [Title] nvarchar(max) NULL,
    [Comment] nvarchar(max) NOT NULL,
    [Helpful] int NOT NULL,
    [Approved] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Transactions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ShippingName] nvarchar(max) NULL,
    [ShippingPhone] nvarchar(max) NULL,
    [ShippingAddress] nvarchar(max) NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [PaymentPhoneNumber] nvarchar(max) NULL,
    [PaymentCardLast4] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Transactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TransactionItem] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [TransactionId] int NOT NULL,
    [HasBeenExchanged] bit NOT NULL,
    [ExchangeRequestId] int NULL,
    [ExchangeCompleted] bit NOT NULL,
    [ExchangeDate] datetime2 NULL,
    CONSTRAINT [PK_TransactionItem] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransactionItem_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ExchangeRequests] (
    [ExchangeId] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [OrderItemId] int NOT NULL,
    [OriginalProductId] int NOT NULL,
    [ReplacementProductId] int NULL,
    [Reason] nvarchar(max) NOT NULL,
    [OtherReason] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [ApprovalDate] datetime2 NULL,
    [CompletionDate] datetime2 NULL,
    [RejectedReason] nvarchar(max) NULL,
    [AdminRemarks] nvarchar(max) NULL,
    [ExchangeDeadline] datetime2 NOT NULL,
    [PickupDate] datetime2 NULL,
    [InspectionDate] datetime2 NULL,
    [ReplacementShipmentDate] datetime2 NULL,
    [TrackingNumber] nvarchar(max) NULL,
    [CourierName] nvarchar(max) NULL,
    [PriceDifference] decimal(18,2) NOT NULL,
    [CustomerConfirmed] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ExchangeRequests] PRIMARY KEY ([ExchangeId]),
    CONSTRAINT [FK_ExchangeRequests_Products_OriginalProductId] FOREIGN KEY ([OriginalProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ExchangeRequests_Products_ReplacementProductId] FOREIGN KEY ([ReplacementProductId]) REFERENCES [Products] ([Id]),
    CONSTRAINT [FK_ExchangeRequests_TransactionItem_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [TransactionItem] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ExchangeRequests_Transactions_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Transactions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ExchangeRequests_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ExchangeImages] (
    [ImageId] int NOT NULL IDENTITY,
    [ExchangeId] int NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [ImageType] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ExchangeImages] PRIMARY KEY ([ImageId]),
    CONSTRAINT [FK_ExchangeImages_ExchangeRequests_ExchangeId] FOREIGN KEY ([ExchangeId]) REFERENCES [ExchangeRequests] ([ExchangeId]) ON DELETE CASCADE
);

CREATE TABLE [ExchangeStatusHistory] (
    [HistoryId] int NOT NULL IDENTITY,
    [ExchangeId] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ChangedBy] nvarchar(max) NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ExchangeStatusHistory] PRIMARY KEY ([HistoryId]),
    CONSTRAINT [FK_ExchangeStatusHistory_ExchangeRequests_ExchangeId] FOREIGN KEY ([ExchangeId]) REFERENCES [ExchangeRequests] ([ExchangeId]) ON DELETE CASCADE
);

CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);

CREATE INDEX [IX_CartItems_UserId] ON [CartItems] ([UserId]);

CREATE INDEX [IX_ExchangeImages_ExchangeId] ON [ExchangeImages] ([ExchangeId]);

CREATE INDEX [IX_ExchangeRequests_CustomerId] ON [ExchangeRequests] ([CustomerId]);

CREATE INDEX [IX_ExchangeRequests_OrderId] ON [ExchangeRequests] ([OrderId]);

CREATE INDEX [IX_ExchangeRequests_OrderItemId] ON [ExchangeRequests] ([OrderItemId]);

CREATE INDEX [IX_ExchangeRequests_OriginalProductId] ON [ExchangeRequests] ([OriginalProductId]);

CREATE INDEX [IX_ExchangeRequests_ReplacementProductId] ON [ExchangeRequests] ([ReplacementProductId]);

CREATE INDEX [IX_ExchangeStatusHistory_ExchangeId] ON [ExchangeStatusHistory] ([ExchangeId]);

CREATE INDEX [IX_ProductSpecification_ProductId] ON [ProductSpecification] ([ProductId]);

CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);

CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);

CREATE INDEX [IX_TransactionItem_TransactionId] ON [TransactionItem] ([TransactionId]);

CREATE INDEX [IX_Transactions_UserId] ON [Transactions] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260719053958_AddExchangePolicyModule', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [ExchangeRequests] DROP CONSTRAINT [FK_ExchangeRequests_Products_OriginalProductId];

ALTER TABLE [ExchangeRequests] DROP CONSTRAINT [FK_ExchangeRequests_TransactionItem_OrderItemId];

ALTER TABLE [ExchangeRequests] DROP CONSTRAINT [FK_ExchangeRequests_Transactions_OrderId];

ALTER TABLE [ExchangeRequests] DROP CONSTRAINT [FK_ExchangeRequests_Users_CustomerId];

ALTER TABLE [TransactionItem] ADD [HasBeenReturned] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [TransactionItem] ADD [ReturnCompleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [TransactionItem] ADD [ReturnDate] datetime2 NULL;

ALTER TABLE [TransactionItem] ADD [ReturnRequestId] int NULL;

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Title] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [ActionUrl] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [ReturnRequests] (
    [ReturnId] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [OrderItemId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [OtherReason] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [ApprovalDate] datetime2 NULL,
    [CompletionDate] datetime2 NULL,
    [RejectedReason] nvarchar(max) NULL,
    [AdminRemarks] nvarchar(max) NULL,
    [PickupDate] datetime2 NULL,
    [InspectionDate] datetime2 NULL,
    [TrackingNumber] nvarchar(max) NULL,
    [CourierName] nvarchar(max) NULL,
    [RefundAmount] decimal(18,2) NOT NULL,
    [CustomerConfirmed] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ReturnRequests] PRIMARY KEY ([ReturnId]),
    CONSTRAINT [FK_ReturnRequests_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReturnRequests_TransactionItem_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [TransactionItem] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReturnRequests_Transactions_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Transactions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReturnRequests_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TimelineEvents] (
    [EventId] int NOT NULL IDENTITY,
    [ReferenceId] int NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [EventDateTime] datetime2 NOT NULL,
    [CourierName] nvarchar(max) NULL,
    [TrackingNumber] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [IsCurrent] bit NOT NULL,
    [IsCompleted] bit NOT NULL,
    CONSTRAINT [PK_TimelineEvents] PRIMARY KEY ([EventId])
);

CREATE TABLE [ReturnImages] (
    [ImageId] int NOT NULL IDENTITY,
    [ReturnId] int NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [ImageType] nvarchar(max) NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ReturnImages] PRIMARY KEY ([ImageId]),
    CONSTRAINT [FK_ReturnImages_ReturnRequests_ReturnId] FOREIGN KEY ([ReturnId]) REFERENCES [ReturnRequests] ([ReturnId]) ON DELETE CASCADE
);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

CREATE INDEX [IX_ReturnImages_ReturnId] ON [ReturnImages] ([ReturnId]);

CREATE INDEX [IX_ReturnRequests_CustomerId] ON [ReturnRequests] ([CustomerId]);

CREATE INDEX [IX_ReturnRequests_OrderId] ON [ReturnRequests] ([OrderId]);

CREATE INDEX [IX_ReturnRequests_OrderItemId] ON [ReturnRequests] ([OrderItemId]);

CREATE INDEX [IX_ReturnRequests_ProductId] ON [ReturnRequests] ([ProductId]);

ALTER TABLE [ExchangeRequests] ADD CONSTRAINT [FK_ExchangeRequests_Products_OriginalProductId] FOREIGN KEY ([OriginalProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [ExchangeRequests] ADD CONSTRAINT [FK_ExchangeRequests_TransactionItem_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [TransactionItem] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [ExchangeRequests] ADD CONSTRAINT [FK_ExchangeRequests_Transactions_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Transactions] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [ExchangeRequests] ADD CONSTRAINT [FK_ExchangeRequests_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260719115246_AddNotificationsTable2', N'10.0.9');

COMMIT;
GO

