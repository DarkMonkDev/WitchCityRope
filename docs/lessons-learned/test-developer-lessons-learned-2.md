# Test Developer Lessons Learned - Part 2

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 MANDATORY STARTUP PROCEDURE - CONTINUATION FROM PART 1 🚨

### 🚨 ULTRA CRITICAL TESTING DOCUMENTS (MUST READ FIRST): 🚨
1. **🛑 DOCKER-ONLY TESTING STANDARD**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/testing/docker-only-testing-standard.md` - **MANDATORY FOR ALL TESTING**
2. **Testing Prerequisites**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/testing/testing-prerequisites.md`
3. **Test Architecture**: `/home/chad/repos/witchcityrope-react/docs/architecture/testing-architecture.md`
4. **Entity Framework Test Patterns**: `/home/chad/repos/witchcityrope-react/docs/standards-processes/testing/entity-framework-testing.md`
5. **Project Architecture**: `/home/chad/repos/witchcityrope-react/ARCHITECTURE.md` - **TECH STACK AND STANDARDS**

### Validation Gates (MUST COMPLETE):
- [ ] **Read Docker-Only Testing Standard FIRST** - Prevents test environment failures
- [ ] Review Testing Prerequisites for mandatory health checks
- [ ] Check Test Architecture for patterns and standards
- [ ] Review Entity Framework testing patterns
- [ ] Verify Docker containers running before ANY test work

## 📚 MULTI-FILE LESSONS LEARNED - PART 2
**Files**: 2 total
**This is**: Part 2 of 2
**Read Part 1**: `/docs/lessons-learned/test-developer-lessons-learned.md` FIRST
**Write to**: This file ONLY for new lessons
**Max size**: 2,000 lines per file (NOT 2,500)
**IF READ FAILS**: STOP and fix per documentation-standards.md

## 🚨 HARD BLOCK ENFORCEMENT (CRITICAL)
If you cannot read ANY part of these lessons learned:
1. **STOP ALL WORK IMMEDIATELY**
2. **DO NOT PROCEED** with any task or request
3. **FIX THE PROBLEM** using procedure in documentation-standards.md
4. **ONLY PROCEED** when all files read successfully
5. These files contain critical knowledge - **NO EXCEPTIONS**

---


**Solution**: Use shared fixture pattern to prevent database conflicts.

```csharp
[Collection("PostgreSQL Integration Tests")]
public class MyTests : IClassFixture<PostgreSqlFixture>
{
    // Tests share the same container instance
}
```

### PostgreSQL DateTime UTC Requirement

**Problem**: "Cannot write DateTime with Kind=Unspecified" errors.

**Solution**: PostgreSQL requires UTC timestamps. Always use DateTimeKind.Utc.

```csharp
// ❌ WRONG
var event = new Event { StartTime = DateTime.Now };
new DateTime(1990, 1, 1) // Kind is Unspecified

// ✅ CORRECT
var event = new Event { StartTime = DateTime.UtcNow };
new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```

### Integration Test Data Isolation

**Problem**: Tests affecting each other's data causing duplicate key violations.

**Solution**: Use unique identifiers for ALL test data to ensure isolation.

```csharp
// ❌ WRONG - Will cause conflicts
var sceneName = "TestUser";
var email = "test@example.com";

// ✅ CORRECT - Always unique
var sceneName = $"TestUser_{Guid.NewGuid():N}";
var email = $"test-{Guid.NewGuid():N}@example.com";
```

### Entity ID Initialization Requirement

**Problem**: Default Guid.Empty values causing duplicate key violations.

**Solution**: Always initialize IDs in entity constructors.

```csharp
public Rsvp(Guid userId, Event @event)
{
    Id = Guid.NewGuid(); // CRITICAL: Must set this!
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

## React Testing Patterns - MANDATORY

### React Testing Framework Stack

**Problem**: Inconsistent testing framework across React codebase.

**Solution**: Use Vitest (not Jest) for React testing consistency.

**Required Stack**:
- ✅ **Vitest**: Primary testing framework
- ✅ **React Testing Library**: Component testing
- ✅ **MSW**: API mocking for integration tests
- ✅ **Playwright**: E2E testing (NOT Puppeteer)
- ❌ **Jest**: Avoid - project uses Vitest

### TanStack Query Hook Testing

**Problem**: Need pattern for testing TanStack Query hooks with proper provider setup.

**Solution**: Use renderHook with QueryClientProvider wrapper.

```typescript
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
  })
  return ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}
```

### Zustand Store Testing

**Problem**: Complex setup for testing Zustand stores.

**Solution**: Test Zustand stores directly without complex setup.

```typescript
describe('authStore', () => {
  beforeEach(() => {
    authStore.getState().logout() // Reset state
  })
  
  it('should update state on login', () => {
    const user = { id: '1', email: 'test@example.com' }
    authStore.getState().login(user)
    
    expect(authStore.getState().user).toEqual(user)
    expect(authStore.getState().isAuthenticated).toBe(true)
  })
})
```

### React Router v7 Testing

**Problem**: Testing React Router v7 navigation in tests.

**Solution**: Use createMemoryRouter for testing navigation flows.

```typescript
const renderWithRouter = (routes, initialEntries = ['/']) => {
  const router = createMemoryRouter(routes, { initialEntries })
  return {
    ...render(<RouterProvider router={router} />),
    router
  }
}
```

### Mantine Component Testing

**Problem**: Testing Mantine UI components requires proper provider setup.

**Solution**: Use userEvent and MantineProvider for component testing.

```typescript
const renderWithMantine = (component) => {
  return render(
    <MantineProvider>{component}</MantineProvider>
  )
}
```

### MSW Axios BaseURL Compatibility

**Problem**: MSW handlers with relative paths not matching axios requests using baseURL.

**Solution**: Use full URLs when API client uses baseURL.

```typescript
// ❌ WRONG - Relative paths don't work with axios baseURL
http.post('/api/auth/login', handler)

// ✅ CORRECT - Use full URLs
http.post('http://localhost:5651/api/auth/login', handler)

// ✅ ALWAYS INCLUDE - Auth refresh interceptor handler
http.post('http://localhost:5651/auth/refresh', () => {
  return new HttpResponse('Unauthorized', { status: 401 })
})
```

### MSW Response Structure Alignment

**Problem**: MSW handlers returning different response structures than expected by mutations.

**Solution**: MSW response structure must exactly match actual API response structure.

```typescript
// ✅ CORRECT - Match exact API response structure
http.post('http://localhost:5651/api/auth/login', async ({ request }) => {
  return HttpResponse.json({
    success: true,
    data: {  // User object directly, not nested
      id: '1',
      email: body.email,
      sceneName: 'TestAdmin',
      createdAt: '2025-08-19T00:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    },
    message: 'Login successful'
  })
})
```

### MSW Global Handler vs Component Test Mocking

**Problem**: Component tests using mockFetch failing because MSW intercepts globally.

**Solution**: When MSW is global, use MSW handlers instead of mock fetch.

```typescript
// ❌ WRONG - Mock fetch directly when MSW is running globally
const mockFetch = vi.fn()
global.fetch = mockFetch

// ✅ CORRECT - Use MSW server.use() to override handlers per test
server.use(
  http.get('http://localhost:5655/api/events', () => {
    return HttpResponse.json([])
  })
)
```

### React Component Test Infinite Loop Prevention

**Problem**: LoginPage causing infinite loops due to redundant navigation logic.

**Solution**: Avoid duplicate navigation logic between components and hooks.

```typescript
// ❌ WRONG - Double navigation causes infinite loops
useEffect(() => {
  if (isAuthenticated) {
    window.location.href = returnTo  // In component
  }
}, [isAuthenticated])
// AND navigation in mutation onSuccess  // In hook

// ✅ CORRECT - Single navigation source
// Let mutation handle all navigation, remove component navigation
```

### TanStack Query Mutation Timing

**Problem**: Checking `isPending` immediately after `mutate()` returning false unexpectedly.

**Solution**: Mutation state updates are asynchronous, even with `act()`.

```typescript
// ❌ WRONG - Immediate check fails
act(() => { result.current.mutate(data) })
expect(result.current.isPending).toBe(true) // May be false

// ✅ CORRECT - Wait for either pending or completion
await waitFor(() => {
  expect(result.current.isPending || result.current.isSuccess).toBe(true)
})
```

### Testing Documentation Reference

**Problem**: Need comprehensive testing patterns documented for consistency.

**Solution**: Follow patterns in `/docs/functional-areas/authentication/testing/comprehensive-testing-plan.md`

## Legacy .NET Testing Reference

**Note**: Essential .NET API testing patterns preserved for reference.
**Location**: Complete patterns in `/docs/standards-processes/testing/TESTING_GUIDE.md`

### .NET Authentication Mocking
```csharp
// Simplified test authentication for .NET API tests
var authService = new Mock<IAuthService>();
authService.Setup(x => x.GetCurrentUserAsync())
    .ReturnsAsync(new UserDto { Id = testUserId });
```

### .NET Validation Testing
```csharp
// Always test edge cases with Theory tests
[Theory]
[InlineData("")]           // Empty
[InlineData(null)]         // Null  
[InlineData("a")]          // Too short
[InlineData("valid@email")] // Valid
public async Task Email_Validation_Works(string email) { }
```

## Common Pitfalls


### Docker Environment Testing

**Problem**: Tests passing locally but failing in CI environments.

**Solution**: Always test with Docker environment to match CI conditions.

**Reference**: `/docs/guides-setup/docker-operations-guide.md`



## Deprecated Testing Approaches

### Removed from React Testing
1. **bUnit** - Blazor component testing framework (NO LONGER APPLICABLE)
2. **xUnit** - Use Vitest for React tests (xUnit still valid for .NET API tests)
3. **Blazor components testing** - All components now React with RTL
4. **SignalR testing** - Not needed unless WebSocket features added
5. **Blazor validation testing** - Use React form validation patterns

### Still Deprecated (All Testing)
1. **Puppeteer** - All E2E tests migrated to Playwright
2. **Jest** - Use Vitest for consistency across React testing
3. **In-memory database** - Use PostgreSQL TestContainers for integration tests
4. **Fixed test data** - Always use unique identifiers
5. **Thread.Sleep/waitForTimeout** - Use proper wait conditions
6. **Generic selectors** - Use data-testid attributes for reliable element selection

### React Integration Testing Pattern

**Problem**: Testing multiple React components working together (form → API → store → navigation).

**Solution**: Use renderHook for testing hook integration without complex component rendering.

```typescript
// Test multiple hooks working together using renderHook
const { result: loginResult } = renderHook(() => useLogin(), { wrapper: createWrapper() })

// Trigger login and verify complete flow
act(() => {
  loginResult.current.mutate({ email: 'admin@witchcityrope.com', password: 'Test123!' })
})

// Verify TanStack Query → Zustand Store → React Router integration
await waitFor(() => {
  expect(loginResult.current.isSuccess).toBe(true)
  expect(useAuthStore.getState().isAuthenticated).toBe(true)
  expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
})
```





## Quick Reference

**Essential Commands**:
- `npm run test:e2e:playwright` - Run E2E tests
- `dotnet test --filter "Category=HealthCheck"` - Health checks first
- `npm test` - Run React unit tests

**Reference**: `/docs/standards-processes/testing/TESTING_GUIDE.md`

## TestContainers Migration for Unit Tests

### Unit Tests Migration from Mocked DbContext to Real PostgreSQL

**Problem**: ApplicationDbContext doesn't have parameterless constructor required for mocking.

**Solution**: Use TestContainers with PostgreSQL + Respawn for database cleanup instead of mocking.

### Action Items
```csharp
// ❌ WRONG - Mocking DbContext causes constructor issues
var mockContext = new Mock<ApplicationDbContext>(); // Fails - no parameterless constructor

// ✅ CORRECT - Use real database with TestContainers
public class DatabaseTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private Respawner? _respawner;
    
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test")
            .Build();
        await _container.StartAsync();
        
        // Setup Respawn for cleanup
        await using var connection = new NpgsqlConnection(ConnectionString);
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }
}

// ✅ CORRECT - Base class for database tests
[Collection("Database")]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected ApplicationDbContext DbContext = null!;
    
    public virtual async Task InitializeAsync()
    {
        DbContext = DatabaseFixture.CreateDbContext(); // Real DbContext
    }
    
    public virtual async Task DisposeAsync()
    {
        DbContext?.Dispose();
        await DatabaseFixture.ResetDatabaseAsync(); // Fast cleanup
    }
}
```

**Performance**: Container-per-collection strategy, Respawn cleanup faster than recreating containers

### Tags
`testcontainers` `postgresql` `unit-testing` `dbcontext` `real-database`

### Unit Test Mechanical Conversion Pattern

**Problem**: Large test file migrations from mocked DbContext to TestContainers.

**Solution**: Systematic field-by-field conversion approach works well.

### Action Items
```csharp
// ✅ CORRECT - After conversion all fields use base class properties
MockUserManager.Setup(x => x.CreateAsync(...))  // Was _mockUserManager
CancellationTokenSource.Cancel()                // Was _cancellationTokenSource.Cancel()
await DbContext.Events.CountAsync()            // Was _mockContext.Setup(...)
// No more _mockTransaction - real database handles transactions

// ✅ CORRECT - Real database verification patterns
var eventsAfterCount = await DbContext.Events.CountAsync();
eventsAfterCount.Should().Be(12);

// ✅ CORRECT - Complex scenarios marked for integration testing
[Fact(Skip = "Requires integration-level testing with real database failure scenarios")]
```

**Key Conversions**:
- Field References: `_mockUserManager` → `MockUserManager`
- Database Operations: Mock setup → Real database queries
- Transaction Management: Remove `_mockTransaction` - real database handles automatically
- Event Testing: Mock verification → Real database count assertions

### Tags
`unit-testing` `mechanical-conversion` `testcontainers` `compilation-fix` `real-database`

### Moq Extension Method Mocking Fix

**Problem**: Moq cannot mock extension methods like `CreateScope()` and `GetRequiredService<T>()`.

**Solution**: Mock the underlying interface methods instead of extension methods.

### Action Items
```csharp
// ❌ WRONG - Cannot mock extension methods
MockServiceProvider.Setup(x => x.CreateScope()).Returns(mockScope);
MockServiceProvider.Setup(x => x.GetRequiredService<ApplicationDbContext>()).Returns(dbContext);

// ✅ CORRECT - Mock underlying interface methods
MockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
    .Returns(MockServiceScopeFactory.Object);
MockServiceScopeFactory.Setup(x => x.CreateScope())
    .Returns(MockServiceScope.Object);
MockScopeServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext)))
    .Returns(DbContext);
```

### Service Provider Mock Hierarchy
```csharp
// Root service provider
Mock<IServiceProvider> MockServiceProvider;
Mock<IServiceScopeFactory> MockServiceScopeFactory;

// Scoped service provider  
Mock<IServiceScope> MockServiceScope;
Mock<IServiceProvider> MockScopeServiceProvider;

// Setup hierarchy
MockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
    .Returns(MockServiceScopeFactory.Object);
MockServiceScopeFactory.Setup(x => x.CreateScope())
    .Returns(MockServiceScope.Object);
MockServiceScope.Setup(x => x.ServiceProvider)
    .Returns(MockScopeServiceProvider.Object);
```


## .NET Background Service Testing Patterns

### Background Service Testing with Static State

**Problem**: Testing BackgroundService implementations with static state and async execution.

**Solution**: Background services need careful test isolation and timing considerations.

### Action Items
```csharp
// ✅ CORRECT - Test background service execution
await service.StartAsync(cancellationToken);
await Task.Delay(100); // Allow background execution
await service.StopAsync(cancellationToken);

// ✅ CORRECT - Reset static state in test cleanup
public void Dispose()
{
    var field = typeof(BackgroundService)
        .GetField("_staticField", BindingFlags.NonPublic | BindingFlags.Static);
    field?.SetValue(null, initialValue);
}

// ✅ CORRECT - Test private methods with reflection when needed
var method = typeof(Service)
    .GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Instance);
var result = method!.Invoke(service, parameters);
```


### PostgreSQL TestContainers Integration Pattern

**Problem**: Database initialization tests require real PostgreSQL for migrations and constraints.

**Solution**: TestContainers with shared fixtures provides reliable PostgreSQL integration testing.

### Action Items
```csharp
// ✅ CORRECT - Shared fixture for PostgreSQL
[Collection("PostgreSQL Integration Tests")]
public class Tests : IClassFixture<PostgreSqlIntegrationFixture>

// ✅ CORRECT - TestContainer setup
_container = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("witchcityrope_test")
    .WithUsername("testuser")
    .WithPassword("testpass")
    .WithCleanUp(true)
    .Build();

// ✅ CORRECT - UTC DateTime verification
createdEvents.Should().OnlyContain(e => e.StartDate.Kind == DateTimeKind.Utc);
```


