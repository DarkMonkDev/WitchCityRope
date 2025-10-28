# Event Registration Cutoff Time - Implementation Specification

**Date**: 2025-10-28
**Feature**: Prevent ticket/RSVP purchases and cancellations after event start time
**Approved Approach**: Option 1 (Site-Wide Timezone) with Admin Settings UI
**Status**: Ready for Implementation

---

## Business Requirements

### Primary Goal
Prevent the following actions after an event's start time (with configurable buffer):
1. **Ticket Purchase** - Cannot buy tickets
2. **Ticket Cancellation** - Cannot cancel/refund tickets
3. **RSVP Creation** - Cannot create RSVP
4. **RSVP Cancellation** - Cannot cancel RSVP

### Cutoff Rule
**Configurable buffer** before event start time (default: 0 minutes)
- Admin can configure buffer time via Admin Settings page
- Example: If buffer set to 30 minutes, cutoff is 30 minutes before event starts

### Timezone Handling
**Site-wide timezone configuration**
- All events assumed to occur in configured timezone (default: America/New_York)
- Admin can change via Admin Settings page
- Only US timezones supported (4 options)

---

## Architecture Overview

### Storage Strategy
**Database storage** (NOT appsettings.json) - survives Docker image updates

**Settings Table**:
```
Table: Settings
Columns:
  - Id (Guid, PK)
  - Key (string, unique) - e.g., "EventTimeZone", "PreStartBufferMinutes"
  - Value (string) - e.g., "America/New_York", "30"
  - Description (string) - Human-readable description
  - CreatedAt (DateTime)
  - UpdatedAt (DateTime)
```

### Default Values (Seed Data)
```csharp
new Setting { Key = "EventTimeZone", Value = "America/New_York", Description = "Timezone where events occur" }
new Setting { Key = "PreStartBufferMinutes", Value = "0", Description = "Minutes before event start to close registration" }
```

**CRITICAL**: Default buffer is **0 minutes**, not 30!

---

## Implementation Tasks

### 1. Database Schema & Migration

#### A. Create Settings Entity

**File**: `/apps/api/Models/Setting.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Application-wide settings stored in database
/// Key-value pairs for configuration that survives deployments
/// </summary>
public class Setting
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Setting key (unique identifier)
    /// Examples: "EventTimeZone", "PreStartBufferMinutes"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value (stored as string, parsed as needed)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this setting controls
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// When setting was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When setting was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }
}
```

#### B. Add to DbContext

**File**: `/apps/api/Data/ApplicationDbContext.cs`

Add DbSet:
```csharp
public DbSet<Setting> Settings => Set<Setting>();
```

Add entity configuration in `OnModelCreating`:
```csharp
// Settings table configuration
entity = modelBuilder.Entity<Setting>();
entity.ToTable("Settings");
entity.HasKey(s => s.Id);
entity.HasIndex(s => s.Key).IsUnique(); // Unique constraint on Key
entity.Property(s => s.Key)
      .IsRequired()
      .HasMaxLength(100);
entity.Property(s => s.Value)
      .IsRequired()
      .HasMaxLength(500);
entity.Property(s => s.Description)
      .HasMaxLength(500);
entity.Property(s => s.CreatedAt)
      .IsRequired()
      .HasColumnType("timestamptz");
entity.Property(s => s.UpdatedAt)
      .IsRequired()
      .HasColumnType("timestamptz");
```

#### C. Create Migration

**Command**:
```bash
cd /apps/api
dotnet ef migrations add AddSettingsTable --context ApplicationDbContext
```

**Migration File** (will be auto-generated, verify it contains):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Settings",
        columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Settings", x => x.Id);
        });

    migrationBuilder.CreateIndex(
        name: "IX_Settings_Key",
        table: "Settings",
        column: "Key",
        unique: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "Settings");
}
```

#### D. Add Seed Data

**File**: `/apps/api/Services/SeedDataService.cs`

Add to `SeedAsync()` method (call AFTER EnsureDatabaseCreated):
```csharp
private async Task SeedSettingsAsync()
{
    _logger.LogInformation("Checking Settings seed data...");

    // Check if settings already exist
    if (await _context.Settings.AnyAsync())
    {
        _logger.LogInformation("Settings already seeded, skipping...");
        return;
    }

    var now = DateTime.UtcNow;
    var settings = new List<Setting>
    {
        new Setting
        {
            Id = Guid.NewGuid(),
            Key = "EventTimeZone",
            Value = "America/New_York",
            Description = "IANA timezone ID where events occur (Eastern Time)",
            CreatedAt = now,
            UpdatedAt = now
        },
        new Setting
        {
            Id = Guid.NewGuid(),
            Key = "PreStartBufferMinutes",
            Value = "0",
            Description = "Minutes before event start to close ticket/RSVP registration and cancellation",
            CreatedAt = now,
            UpdatedAt = now
        }
    };

    await _context.Settings.AddRangeAsync(settings);
    await _context.SaveChangesAsync();

    _logger.LogInformation("✅ Settings seeded: EventTimeZone=America/New_York, PreStartBufferMinutes=0");
}
```

Call from `SeedAsync()`:
```csharp
public async Task SeedAsync()
{
    await EnsureDatabaseCreatedAsync();
    await SeedRolesAsync();
    await SeedUsersAsync();
    await SeedEventsAsync();
    await SeedSettingsAsync(); // ADD THIS LINE
    // ... other seed methods
}
```

---

### 2. Backend API - Settings Service

**File**: `/apps/api/Features/Settings/Services/SettingsService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Settings.Services;

public interface ISettingsService
{
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string Error)> UpdateMultipleSettingsAsync(
        Dictionary<string, string> updates,
        CancellationToken cancellationToken = default);
}

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ApplicationDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.Settings
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync(cancellationToken);

        return setting?.Value;
    }

    public async Task<bool> UpdateSettingAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var setting = await _context.Settings
            .Where(s => s.Key == key)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting == null)
        {
            _logger.LogWarning("Attempted to update non-existent setting: {Key}", key);
            return false;
        }

        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated setting {Key} to {Value}", key, value);

        return true;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.Settings
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return settings;
    }

    public async Task<(bool Success, string Error)> UpdateMultipleSettingsAsync(
        Dictionary<string, string> updates,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = updates.Keys.ToList();
            var settings = await _context.Settings
                .Where(s => keys.Contains(s.Key))
                .ToListAsync(cancellationToken);

            if (settings.Count != updates.Count)
            {
                var missingKeys = keys.Except(settings.Select(s => s.Key)).ToList();
                return (false, $"Settings not found: {string.Join(", ", missingKeys)}");
            }

            var now = DateTime.UtcNow;
            foreach (var setting in settings)
            {
                setting.Value = updates[setting.Key];
                setting.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated {Count} settings", settings.Count);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return (false, "Failed to update settings");
        }
    }
}
```

---

### 3. Backend API - TimeZone Service

**File**: `/apps/api/Features/Settings/Services/TimeZoneService.cs`

```csharp
using System.Globalization;

namespace WitchCityRope.Api.Features.Settings.Services;

public interface ITimeZoneService
{
    Task<TimeZoneInfo> GetEventTimeZoneAsync(CancellationToken cancellationToken = default);
    Task<int> GetPreStartBufferMinutesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsRegistrationOpenAsync(DateTime eventStartDateUtc, CancellationToken cancellationToken = default);
    Task<DateTimeOffset> ConvertToEventTimeAsync(DateTime utcDateTime, CancellationToken cancellationToken = default);
}

public class TimeZoneService : ITimeZoneService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TimeZoneService> _logger;

    // Cache timezone to avoid repeated database lookups
    private TimeZoneInfo? _cachedTimeZone;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private const int CACHE_MINUTES = 60;

    public TimeZoneService(ISettingsService settingsService, ILogger<TimeZoneService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<TimeZoneInfo> GetEventTimeZoneAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cachedTimeZone != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedTimeZone;
        }

        var timeZoneId = await _settingsService.GetSettingAsync("EventTimeZone", cancellationToken);

        if (string.IsNullOrEmpty(timeZoneId))
        {
            _logger.LogWarning("EventTimeZone setting not found, defaulting to America/New_York");
            timeZoneId = "America/New_York";
        }

        try
        {
            _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _cacheExpiry = DateTime.UtcNow.AddMinutes(CACHE_MINUTES);
            return _cachedTimeZone;
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogError(ex, "Invalid timezone ID: {TimeZoneId}, falling back to America/New_York", timeZoneId);
            _cachedTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            _cacheExpiry = DateTime.UtcNow.AddMinutes(CACHE_MINUTES);
            return _cachedTimeZone;
        }
    }

    public async Task<int> GetPreStartBufferMinutesAsync(CancellationToken cancellationToken = default)
    {
        var bufferValue = await _settingsService.GetSettingAsync("PreStartBufferMinutes", cancellationToken);

        if (string.IsNullOrEmpty(bufferValue) || !int.TryParse(bufferValue, out int bufferMinutes))
        {
            _logger.LogWarning("PreStartBufferMinutes setting invalid or missing, defaulting to 0");
            return 0;
        }

        return bufferMinutes;
    }

    public async Task<bool> IsRegistrationOpenAsync(
        DateTime eventStartDateUtc,
        CancellationToken cancellationToken = default)
    {
        var bufferMinutes = await GetPreStartBufferMinutesAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var cutoffTime = eventStartDateUtc.AddMinutes(-bufferMinutes);

        var isOpen = now < cutoffTime;

        if (!isOpen)
        {
            _logger.LogInformation(
                "Registration closed: Current time {Now} is past cutoff {Cutoff} (buffer: {Buffer} min)",
                now, cutoffTime, bufferMinutes);
        }

        return isOpen;
    }

    public async Task<DateTimeOffset> ConvertToEventTimeAsync(
        DateTime utcDateTime,
        CancellationToken cancellationToken = default)
    {
        var timeZone = await GetEventTimeZoneAsync(cancellationToken);
        return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
    }
}
```

---

### 4. Backend API - Settings Endpoints

**File**: `/apps/api/Features/Settings/Endpoints/SettingsEndpoints.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Api.Features.Settings.Models;
using WitchCityRope.Api.Features.Settings.Services;

namespace WitchCityRope.Api.Features.Settings.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // Get all settings (admin only)
        app.MapGet("/api/admin/settings", async (
            ISettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            var settings = await settingsService.GetAllSettingsAsync(cancellationToken);
            return Results.Ok(settings);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetAdminSettings")
        .WithSummary("Get all application settings")
        .WithDescription("Returns all configurable settings (admin only)")
        .WithTags("Settings")
        .Produces<Dictionary<string, string>>(200)
        .Produces(401)
        .Produces(403);

        // Update settings (admin only)
        app.MapPut("/api/admin/settings", async (
            UpdateSettingsRequest request,
            ISettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            // Validate timezone if provided
            if (request.Settings.ContainsKey("EventTimeZone"))
            {
                var timeZoneId = request.Settings["EventTimeZone"];
                try
                {
                    TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    return Results.Problem(
                        title: "Invalid Timezone",
                        detail: $"Timezone '{timeZoneId}' is not valid",
                        statusCode: 400);
                }
            }

            // Validate buffer minutes if provided
            if (request.Settings.ContainsKey("PreStartBufferMinutes"))
            {
                var bufferValue = request.Settings["PreStartBufferMinutes"];
                if (!int.TryParse(bufferValue, out int bufferMinutes) || bufferMinutes < 0)
                {
                    return Results.Problem(
                        title: "Invalid Buffer Minutes",
                        detail: "PreStartBufferMinutes must be a non-negative integer",
                        statusCode: 400);
                }
            }

            var (success, error) = await settingsService.UpdateMultipleSettingsAsync(
                request.Settings,
                cancellationToken);

            if (!success)
            {
                return Results.Problem(
                    title: "Update Failed",
                    detail: error,
                    statusCode: 500);
            }

            return Results.Ok(new { Message = "Settings updated successfully" });
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("UpdateAdminSettings")
        .WithSummary("Update application settings")
        .WithDescription("Updates one or more application settings (admin only)")
        .WithTags("Settings")
        .Produces<object>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);
    }
}
```

**File**: `/apps/api/Features/Settings/Models/UpdateSettingsRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Settings.Models;

/// <summary>
/// Request model for updating multiple settings
/// </summary>
public class UpdateSettingsRequest
{
    /// <summary>
    /// Dictionary of setting key-value pairs to update
    /// </summary>
    [Required]
    public Dictionary<string, string> Settings { get; set; } = new();
}
```

**Register endpoints in Program.cs**:
```csharp
using WitchCityRope.Api.Features.Settings.Endpoints;
using WitchCityRope.Api.Features.Settings.Services;

// Register services
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ITimeZoneService, TimeZoneService>();

// Map endpoints
app.MapSettingsEndpoints();
```

---

### 5. Update Event Business Logic

**File**: `/apps/api/Features/Events/Services/EventService.cs`

Update methods to use TimeZoneService:

```csharp
// Inject ITimeZoneService in constructor
private readonly ITimeZoneService _timeZoneService;

public EventService(
    ApplicationDbContext context,
    ILogger<EventService> logger,
    ITimeZoneService timeZoneService)
{
    _context = context;
    _logger = logger;
    _timeZoneService = timeZoneService;
}

// Example: Update ticket purchase logic
public async Task<(bool Success, string? TicketId, string Error)> PurchaseTicketAsync(
    string sessionId,
    string userId,
    string ticketTypeId,
    CancellationToken cancellationToken = default)
{
    var session = await _context.Sessions
        .Include(s => s.Event)
        .Include(s => s.TicketTypes)
        .FirstOrDefaultAsync(s => s.Id.ToString() == sessionId, cancellationToken);

    if (session == null)
        return (false, null, "Session not found");

    // CHECK REGISTRATION CUTOFF
    if (!await _timeZoneService.IsRegistrationOpenAsync(session.StartTime, cancellationToken))
    {
        return (false, null, "Registration has closed for this session");
    }

    // ... rest of purchase logic
}

// Example: Update ticket cancellation logic
public async Task<(bool Success, string Error)> CancelTicketAsync(
    string ticketId,
    string userId,
    CancellationToken cancellationToken = default)
{
    var ticket = await _context.Tickets
        .Include(t => t.Session)
        .FirstOrDefaultAsync(t => t.Id.ToString() == ticketId, cancellationToken);

    if (ticket == null)
        return (false, "Ticket not found");

    // CHECK CANCELLATION CUTOFF
    if (!await _timeZoneService.IsRegistrationOpenAsync(ticket.Session.StartTime, cancellationToken))
    {
        return (false, "Ticket cancellations have closed for this session");
    }

    // ... rest of cancellation logic
}

// Apply same logic to:
// - CreateRsvpAsync
// - CancelRsvpAsync
```

---

### 6. Frontend - Admin Settings Page

**File**: `/apps/web/src/pages/admin/AdminSettingsPage.tsx`

```typescript
import React, { useState } from 'react';
import {
  Container,
  Title,
  Text,
  Paper,
  Stack,
  Select,
  TextInput,
  Button,
  Alert,
  Group,
  Loader,
} from '@mantine/core';
import { IconCheck, IconAlertCircle, IconSettings } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';

interface Settings {
  EventTimeZone: string;
  PreStartBufferMinutes: string;
}

// US Timezones only
const US_TIMEZONES = [
  { value: 'America/New_York', label: 'Eastern Time (ET)' },
  { value: 'America/Chicago', label: 'Central Time (CT)' },
  { value: 'America/Denver', label: 'Mountain Time (MT)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (PT)' },
];

export const AdminSettingsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [localSettings, setLocalSettings] = useState<Settings>({
    EventTimeZone: 'America/New_York',
    PreStartBufferMinutes: '0',
  });

  // Fetch current settings
  const { data: settings, isLoading, error } = useQuery({
    queryKey: ['adminSettings'],
    queryFn: async () => {
      const response = await api.get<Settings>('/api/admin/settings');
      return response.data;
    },
    onSuccess: (data) => {
      setLocalSettings(data);
    },
  });

  // Update settings mutation
  const updateMutation = useMutation({
    mutationFn: async (updates: Settings) => {
      const response = await api.put('/api/admin/settings', {
        settings: updates,
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adminSettings'] });
    },
  });

  const handleSave = () => {
    // Validate buffer minutes
    const bufferMinutes = parseInt(localSettings.PreStartBufferMinutes, 10);
    if (isNaN(bufferMinutes) || bufferMinutes < 0) {
      return; // Validation handled by form
    }

    updateMutation.mutate(localSettings);
  };

  const handleReset = () => {
    if (settings) {
      setLocalSettings(settings);
    }
  };

  const hasChanges =
    settings &&
    (localSettings.EventTimeZone !== settings.EventTimeZone ||
      localSettings.PreStartBufferMinutes !== settings.PreStartBufferMinutes);

  if (isLoading) {
    return (
      <Container size="md" py="xl">
        <Group justify="center">
          <Loader size="lg" />
        </Group>
      </Container>
    );
  }

  if (error) {
    return (
      <Container size="md" py="xl">
        <Alert icon={<IconAlertCircle />} title="Error" color="red">
          Failed to load settings. Please try again later.
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="md" py="xl">
      {/* Header */}
      <Group mb="xl">
        <IconSettings size={32} color="#8B8680" />
        <Title
          order={1}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Admin Settings
        </Title>
      </Group>

      <Text size="sm" c="dimmed" mb="xl">
        Configure system-wide settings for event management and timezone handling.
      </Text>

      {/* Success Alert */}
      {updateMutation.isSuccess && (
        <Alert icon={<IconCheck />} title="Success" color="green" mb="md" onClose={() => updateMutation.reset()}>
          Settings updated successfully!
        </Alert>
      )}

      {/* Error Alert */}
      {updateMutation.isError && (
        <Alert icon={<IconAlertCircle />} title="Error" color="red" mb="md" onClose={() => updateMutation.reset()}>
          {updateMutation.error?.message || 'Failed to update settings'}
        </Alert>
      )}

      {/* Settings Form */}
      <Paper shadow="sm" p="xl" radius="md" style={{ background: '#FFF8F0', border: '1px solid rgba(136, 1, 36, 0.1)' }}>
        <Stack gap="lg">
          {/* Local Timezone Setting */}
          <Select
            label="Local Timezone"
            description="The timezone where your events occur. All event times will be interpreted in this timezone."
            data={US_TIMEZONES}
            value={localSettings.EventTimeZone}
            onChange={(value) =>
              setLocalSettings((prev) => ({
                ...prev,
                EventTimeZone: value || 'America/New_York',
              }))
            }
            required
            styles={{
              label: { fontWeight: 600, color: '#2B2B2B' },
              description: { fontSize: '14px' },
            }}
          />

          {/* Pre-Start Buffer Setting */}
          <TextInput
            label="Pre-Start Buffer to Cancel RSVP / Ticket Refunds"
            description="Minutes before event start when registration and cancellations close. Set to 0 to allow until event starts."
            placeholder="0"
            value={localSettings.PreStartBufferMinutes}
            onChange={(e) =>
              setLocalSettings((prev) => ({
                ...prev,
                PreStartBufferMinutes: e.target.value,
              }))
            }
            type="number"
            min={0}
            required
            error={
              isNaN(parseInt(localSettings.PreStartBufferMinutes, 10)) ||
              parseInt(localSettings.PreStartBufferMinutes, 10) < 0
                ? 'Must be a non-negative number'
                : undefined
            }
            styles={{
              label: { fontWeight: 600, color: '#2B2B2B' },
              description: { fontSize: '14px' },
            }}
          />

          {/* Action Buttons */}
          <Group justify="space-between" mt="md">
            <Button variant="subtle" color="gray" onClick={handleReset} disabled={!hasChanges || updateMutation.isPending}>
              Reset Changes
            </Button>

            <Button
              variant="filled"
              color="#880124"
              onClick={handleSave}
              disabled={!hasChanges || updateMutation.isPending}
              loading={updateMutation.isPending}
            >
              Save Settings
            </Button>
          </Group>
        </Stack>
      </Paper>

      {/* Information Box */}
      <Paper shadow="xs" p="md" mt="xl" style={{ background: '#F5F5F5', borderLeft: '4px solid #880124' }}>
        <Text size="sm" fw={600} mb="xs">
          ℹ️ About These Settings
        </Text>
        <Text size="sm" c="dimmed">
          <strong>Timezone:</strong> All events are assumed to occur in the selected timezone. Users in different timezones
          will see times converted to their local timezone when viewing events.
        </Text>
        <Text size="sm" c="dimmed" mt="xs">
          <strong>Buffer Time:</strong> Prevents last-minute registrations and cancellations. For example, setting this to 30
          means registration closes 30 minutes before the event starts.
        </Text>
      </Paper>
    </Container>
  );
};
```

---

### 7. Frontend - API Integration

**File**: `/apps/web/src/api/settings.ts`

```typescript
import { api } from './client';

export interface AdminSettings {
  EventTimeZone: string;
  PreStartBufferMinutes: string;
}

export async function getAdminSettings(): Promise<AdminSettings> {
  const response = await api.get<AdminSettings>('/api/admin/settings');
  return response.data;
}

export async function updateAdminSettings(settings: AdminSettings): Promise<void> {
  await api.put('/api/admin/settings', { settings });
}
```

---

### 8. Frontend - Router Configuration

**File**: `/apps/web/src/routes/router.tsx`

Add route:
```typescript
import { AdminSettingsPage } from '../pages/admin/AdminSettingsPage';

// In admin routes section:
{
  path: '/admin/settings',
  element: <AdminSettingsPage />,
  loader: requireAdminRole,
}
```

---

## Testing Requirements

### Backend Tests

**File**: `/tests/WitchCityRope.Core.Tests/Features/Settings/SettingsServiceTests.cs`

Test cases:
1. GetSetting_ExistingSetting_ReturnsValue
2. GetSetting_NonExistentSetting_ReturnsNull
3. UpdateSetting_ExistingSetting_UpdatesValue
4. UpdateSetting_NonExistentSetting_ReturnsFalse
5. UpdateMultipleSettings_AllExist_UpdatesAll
6. UpdateMultipleSettings_SomeMissing_ReturnsError

**File**: `/tests/WitchCityRope.Core.Tests/Features/Settings/TimeZoneServiceTests.cs`

Test cases:
1. GetEventTimeZone_ValidSetting_ReturnsTimeZone
2. GetEventTimeZone_InvalidSetting_ReturnsDefault
3. GetPreStartBufferMinutes_ValidSetting_ReturnsInt
4. GetPreStartBufferMinutes_InvalidSetting_ReturnsZero
5. IsRegistrationOpen_BeforeCutoff_ReturnsTrue
6. IsRegistrationOpen_AfterCutoff_ReturnsFalse
7. IsRegistrationOpen_WithZeroBuffer_ClosesAtEventStart

### Frontend Tests

**File**: `/apps/web/src/pages/admin/__tests__/AdminSettingsPage.test.tsx`

Test cases:
1. Renders settings form with current values
2. Updates timezone dropdown when changed
3. Updates buffer minutes input when changed
4. Enables save button when changes made
5. Disables save button when no changes
6. Calls API with correct payload on save
7. Shows success message on successful update
8. Shows error message on failed update
9. Reset button restores original values

### Integration Tests

**Scenario 1: Registration Cutoff**
- Create event starting in 2 hours
- Set buffer to 30 minutes
- Attempt registration at T-35 min: ✅ Success
- Attempt registration at T-25 min: ❌ Fail
- Attempt registration after start: ❌ Fail

**Scenario 2: Zero Buffer**
- Create event starting in 1 hour
- Set buffer to 0 minutes
- Attempt registration at T-5 min: ✅ Success
- Attempt registration at T+1 min: ❌ Fail

**Scenario 3: Timezone Change**
- Change timezone from ET to PT
- Verify cutoff times adjust correctly
- Verify DST handling

---

## Implementation Checklist

### Backend Tasks
- [ ] Create `Setting` entity model
- [ ] Update `ApplicationDbContext` with DbSet and configuration
- [ ] Create EF migration: `AddSettingsTable`
- [ ] Add seed data in `SeedDataService`
- [ ] Create `ISettingsService` and `SettingsService`
- [ ] Create `ITimeZoneService` and `TimeZoneService`
- [ ] Create settings request/response DTOs
- [ ] Create `/api/admin/settings` endpoints (GET, PUT)
- [ ] Register services in `Program.cs`
- [ ] Map endpoints in `Program.cs`
- [ ] Update `EventService.PurchaseTicketAsync` with cutoff check
- [ ] Update `EventService.CancelTicketAsync` with cutoff check
- [ ] Update `EventService.CreateRsvpAsync` with cutoff check
- [ ] Update `EventService.CancelRsvpAsync` with cutoff check

### Frontend Tasks
- [ ] Create `AdminSettingsPage.tsx` component
- [ ] Create settings API functions in `/api/settings.ts`
- [ ] Add route in `router.tsx`
- [ ] Verify Settings card navigation on Admin Dashboard
- [ ] Test timezone dropdown with 4 US options
- [ ] Test buffer minutes input validation
- [ ] Test save/reset functionality

### Database Tasks
- [ ] Run migration on local dev database
- [ ] Verify Settings table created
- [ ] Verify seed data inserted
- [ ] Test unique constraint on Key column

### Testing Tasks
- [ ] Write backend unit tests for SettingsService
- [ ] Write backend unit tests for TimeZoneService
- [ ] Write frontend component tests for AdminSettingsPage
- [ ] Write integration tests for registration cutoff
- [ ] Manual test: Change timezone, verify event display
- [ ] Manual test: Change buffer, attempt registration near cutoff

---

## Edge Cases to Handle

1. **Invalid timezone in database**: TimeZoneService falls back to America/New_York
2. **Non-numeric buffer value**: TimeZoneService defaults to 0
3. **Negative buffer value**: Validation in API endpoint prevents save
4. **Settings not seeded**: TimeZoneService uses hardcoded defaults
5. **Concurrent updates**: Database unique constraint prevents duplicate keys
6. **Timezone cache**: 60-minute cache to avoid repeated DB queries

---

## Files to Create/Modify

### New Files (13)
1. `/apps/api/Models/Setting.cs`
2. `/apps/api/Features/Settings/Services/SettingsService.cs`
3. `/apps/api/Features/Settings/Services/TimeZoneService.cs`
4. `/apps/api/Features/Settings/Endpoints/SettingsEndpoints.cs`
5. `/apps/api/Features/Settings/Models/UpdateSettingsRequest.cs`
6. `/apps/web/src/pages/admin/AdminSettingsPage.tsx`
7. `/apps/web/src/api/settings.ts`
8. `/tests/WitchCityRope.Core.Tests/Features/Settings/SettingsServiceTests.cs`
9. `/tests/WitchCityRope.Core.Tests/Features/Settings/TimeZoneServiceTests.cs`
10. `/apps/web/src/pages/admin/__tests__/AdminSettingsPage.test.tsx`
11. Migration file: `YYYYMMDDHHMMSS_AddSettingsTable.cs`

### Modified Files (5)
1. `/apps/api/Data/ApplicationDbContext.cs` - Add Settings DbSet and configuration
2. `/apps/api/Services/SeedDataService.cs` - Add settings seed method
3. `/apps/api/Features/Events/Services/EventService.cs` - Add cutoff checks
4. `/apps/api/Program.cs` - Register services and endpoints
5. `/apps/web/src/routes/router.tsx` - Add settings route

---

## Success Criteria

### Backend
✅ Settings table exists in database
✅ Seed data populates default settings
✅ GET /api/admin/settings returns current settings
✅ PUT /api/admin/settings updates settings
✅ TimeZoneService correctly calculates cutoff times
✅ EventService blocks actions after cutoff

### Frontend
✅ Admin Settings page loads at /admin/settings
✅ Settings card on dashboard navigates correctly
✅ Timezone dropdown shows 4 US timezones
✅ Buffer input accepts non-negative integers
✅ Save button updates settings via API
✅ Success/error messages display appropriately

### Integration
✅ Ticket purchase blocked after cutoff
✅ Ticket cancellation blocked after cutoff
✅ RSVP creation blocked after cutoff
✅ RSVP cancellation blocked after cutoff
✅ Buffer time of 0 allows until event starts
✅ Timezone changes don't break existing events

---

## Questions Answered

**Q: Where to store settings?**
A: Database (Settings table) - survives Docker deployments ✅

**Q: Default buffer time?**
A: 0 minutes (NOT 30) ✅

**Q: Which timezones to support?**
A: 4 US timezones only (ET, CT, MT, PT) ✅

**Q: Add timezone label to event cards?**
A: No, skip this UX change ✅

**Q: Settings page navigation?**
A: Clicking Settings card on Admin Dashboard → /admin/settings ✅

---

## Implementation Estimate

**Backend**: 6-8 hours
- Database/migration: 1 hour
- Services: 3 hours
- API endpoints: 1 hour
- Business logic updates: 2-3 hours

**Frontend**: 3-4 hours
- Admin Settings page: 2-3 hours
- API integration: 1 hour

**Testing**: 4-5 hours
- Backend tests: 2-3 hours
- Frontend tests: 1 hour
- Integration testing: 1-2 hours

**Total**: 13-17 hours

---

## Next Steps

1. ✅ Review this specification
2. ⏸️ Approval from user
3. ⏳ Assign to backend-developer (database + API)
4. ⏳ Assign to react-developer (frontend UI)
5. ⏳ Assign to test-developer (test suites)
6. ⏳ Assign to test-executor (run tests)
7. ⏳ Manual QA testing
8. ⏳ Documentation update
9. ⏳ Git commit
