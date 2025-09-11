-- Create Event Sessions table
CREATE TABLE IF NOT EXISTS "EventSessions" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL,
    "SessionIdentifier" varchar(10) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Date" date NOT NULL,
    "StartTime" time NOT NULL,
    "EndTime" time NOT NULL,
    "Capacity" integer NOT NULL,
    "IsRequired" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_EventSessions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventSessions_Events_EventId" FOREIGN KEY ("EventId") 
        REFERENCES "Events" ("Id") ON DELETE CASCADE
);

-- Create Event Ticket Types table
CREATE TABLE IF NOT EXISTS "EventTicketTypes" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL,
    "Name" varchar(200) NOT NULL,
    "Description" text,
    "Type" varchar(50) NOT NULL,
    "MinPrice" decimal(10,2) NOT NULL,
    "MaxPrice" decimal(10,2) NOT NULL,
    "QuantityAvailable" integer,
    "QuantitySold" integer NOT NULL DEFAULT 0,
    "SalesStartDate" timestamp with time zone,
    "SalesEndDate" timestamp with time zone,
    "IsRsvpMode" boolean NOT NULL DEFAULT FALSE,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_EventTicketTypes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventTicketTypes_Events_EventId" FOREIGN KEY ("EventId") 
        REFERENCES "Events" ("Id") ON DELETE CASCADE
);

-- Create junction table for ticket types and sessions
CREATE TABLE IF NOT EXISTS "EventTicketTypeSessions" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "TicketTypeId" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_EventTicketTypeSessions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventTicketTypeSessions_EventTicketTypes_TicketTypeId" 
        FOREIGN KEY ("TicketTypeId") REFERENCES "EventTicketTypes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventTicketTypeSessions_EventSessions_SessionId" 
        FOREIGN KEY ("SessionId") REFERENCES "EventSessions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_EventTicketTypeSessions_TicketTypeId_SessionId" 
        UNIQUE ("TicketTypeId", "SessionId")
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_EventSessions_EventId" ON "EventSessions" ("EventId");
CREATE INDEX IF NOT EXISTS "IX_EventSessions_SessionIdentifier" ON "EventSessions" ("SessionIdentifier");
CREATE INDEX IF NOT EXISTS "IX_EventTicketTypes_EventId" ON "EventTicketTypes" ("EventId");
CREATE INDEX IF NOT EXISTS "IX_EventTicketTypeSessions_TicketTypeId" ON "EventTicketTypeSessions" ("TicketTypeId");
CREATE INDEX IF NOT EXISTS "IX_EventTicketTypeSessions_SessionId" ON "EventTicketTypeSessions" ("SessionId");

-- Mark the migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250825025039_AddEventSessionMatrix', '9.0.1') 
ON CONFLICT DO NOTHING;