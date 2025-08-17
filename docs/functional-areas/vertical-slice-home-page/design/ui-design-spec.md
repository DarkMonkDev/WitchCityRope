# UI Design Specification: Vertical Slice Home Page
<!-- Last Updated: 2025-08-16 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Development -->

## Design Overview

This is a **minimal technical proof-of-concept** design to validate React + API + Database communication. The design prioritizes **functional testing over visual polish** since this is throwaway code for stack validation.

## Design Principles for POC

1. **Extreme Simplicity**: Basic HTML elements, minimal styling
2. **Clear State Representation**: Loading, error, empty, and success states visible
3. **Responsive Foundation**: Mobile-first grid layout
4. **Developer-Friendly**: Clear data-testid attributes for testing
5. **No Brand Requirements**: This is technical validation, not production design

## Component Structure

### Page Layout
```
Header (Fixed)
├── Site Title: "WitchCityRope Events"
├── Subtitle: "Technical Stack Test"
└── Dev Note: Context for throwaway code

Main Content (Responsive Container)
├── Loading State (Spinner + Text)
├── Error State (Red alert box)
├── Empty State (Centered message)
└── Events Grid (CSS Grid, responsive)
    ├── Event Card 1
    ├── Event Card 2
    └── Event Card 3...
```

## State Management

### 1. Loading State
```jsx
// When useEffect starts fetching
<div className="loading">
  <div className="spinner"></div>
  <span>Loading events...</span>
</div>
```

### 2. Error State
```jsx
// When fetch fails
<div className="error">
  <strong>Error:</strong> Failed to load events. 
  Please check that the API service is running on http://localhost:5653
</div>
```

### 3. Empty State
```jsx
// When API returns empty array
<div className="empty-state">
  <h3>No events available</h3>
  <p>The API returned no events. Check the database or hardcoded test data.</p>
</div>
```

### 4. Success State
```jsx
// When events load successfully
<div className="events-grid">
  {events.map(event => <EventCard key={event.id} event={event} />)}
</div>
```

## Component Specifications

### HomePage Component
- **Purpose**: Root container with title and EventsList
- **Props**: None
- **State**: None (stateless container)
- **Styling**: Basic container with padding

### EventsList Component  
- **Purpose**: Fetch events and manage loading/error states
- **Props**: None
- **State**: events[], loading, error
- **Key Logic**: useEffect with fetch to http://localhost:5653/api/events

### EventCard Component
- **Purpose**: Display individual event information
- **Props**: event: Event
- **State**: None (pure display component)
- **Key Elements**: title, description, formatted date, location

### LoadingSpinner Component
- **Purpose**: Simple loading indicator
- **Props**: None
- **State**: None
- **Styling**: CSS animation spinner + text

## CSS Grid Layout

### Desktop View (1024px+)
```css
.events-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
}
```

### Tablet View (768px - 1024px)
- Cards automatically adjust with auto-fit
- 2 columns typically

### Mobile View (< 768px)
```css
@media (max-width: 768px) {
  .events-grid {
    grid-template-columns: 1fr;
    gap: 1rem;
  }
}
```

## Event Card Design

### Visual Structure
```
┌─────────────────────────────────┐
│ Event Title (h3, semibold)      │
├─────────────────────────────────┤
│ Description text...             │
│ (gray, line-height 1.5)         │
├─────────────────────────────────┤
│ 📅 Friday, August 25, 2025      │
│ 📍 Salem, MA                    │
└─────────────────────────────────┘
```

### Key Styling
- **Border**: 1px solid gray, 8px border-radius
- **Padding**: 1.5rem (desktop), 1rem (mobile)
- **Shadow**: Subtle box-shadow, enhanced on hover
- **Typography**: Sans-serif system font stack
- **Icons**: Simple emoji icons (📅 📍)

## Data Requirements

### Event Interface
```typescript
interface Event {
  id: string;          // For React key prop
  title: string;       // Display as card heading
  description: string; // Display as body text
  startDate: string;   // ISO 8601, format for display
  location: string;    // Display with location icon
}
```

### API Endpoint
- **URL**: http://localhost:5653/api/events
- **Method**: GET
- **Response**: Event[] (JSON array)
- **Error Handling**: Display user-friendly message for any fetch error

## Testing Considerations

### Data Test IDs
```html
<!-- For Playwright E2E tests -->
<div data-testid="events-grid">
  <div data-testid="event-card">
    <h3 data-testid="event-title">...</h3>
    <p data-testid="event-description">...</p>
    <div data-testid="event-meta">...</div>
  </div>
</div>

<div data-testid="loading-spinner">...</div>
<div data-testid="error-message">...</div>
<div data-testid="empty-state">...</div>
```

### Visual Testing Points
- [ ] Events display in grid layout
- [ ] Cards are properly spaced
- [ ] Mobile layout stacks vertically
- [ ] Loading spinner is centered
- [ ] Error message is clearly visible
- [ ] Date formatting is readable

## Styling Approach

### Technology Stack
- **No CSS Framework**: Basic CSS for simplicity
- **CSS Grid**: For responsive event layout
- **Flexbox**: For card internal layout and centering
- **CSS Custom Properties**: Not needed for POC
- **Responsive Design**: Media queries for mobile

### Color Palette (Minimal)
```css
:root {
  --background: #f8f9fa;
  --white: #ffffff;
  --text-primary: #2c3e50;
  --text-secondary: #6c757d;
  --border: #dee2e6;
  --error: #721c24;
  --error-bg: #f8d7da;
  --info-bg: #d1ecf1;
  --shadow: rgba(0,0,0,0.1);
}
```

## Progressive Enhancement Plan

### Step 1: Hardcoded API Response
- Display 2-3 hardcoded events from API
- Focus on proving React ↔ API communication
- Basic error handling for API unavailable

### Step 2: Database Integration
- Add more events to display database querying
- Test pagination if many events (not required for POC)
- Validate full data flow: React → API → PostgreSQL → API → React

## Development Implementation Notes

### React Hooks Usage
```jsx
// EventsList.tsx core pattern
const [events, setEvents] = useState<Event[]>([]);
const [loading, setLoading] = useState(true);
const [error, setError] = useState<string | null>(null);

useEffect(() => {
  const fetchEvents = async () => {
    try {
      const response = await fetch('http://localhost:5653/api/events');
      if (!response.ok) throw new Error('API Error');
      const data = await response.json();
      setEvents(data);
    } catch (err) {
      setError('Failed to load events');
    } finally {
      setLoading(false);
    }
  };
  fetchEvents();
}, []);
```

### Error Boundaries
- Not required for POC
- Basic try/catch in useEffect is sufficient
- Console.error for debugging

### Performance Considerations
- No optimization needed (POC)
- No lazy loading, code splitting, or memoization
- Focus on working functionality

## Success Criteria

### Visual Requirements
- [ ] Page loads without visual errors
- [ ] Events display in clean card layout
- [ ] Responsive design works on mobile
- [ ] Loading state is visible during fetch
- [ ] Error state displays helpful message
- [ ] All text is readable (basic contrast)

### Functional Requirements
- [ ] React components render correctly
- [ ] API fetch completes successfully
- [ ] Events data displays from API response
- [ ] Component state updates properly
- [ ] No console errors during normal operation

## Quality Checklist

### Code Quality
- [ ] TypeScript interfaces defined
- [ ] Functional components with hooks
- [ ] Proper key props for lists
- [ ] Clean component separation
- [ ] Basic error handling

### UX Quality  
- [ ] Clear loading indication
- [ ] Helpful error messages
- [ ] Empty state handled
- [ ] Mobile-friendly layout
- [ ] Fast perceived performance

### Testing Quality
- [ ] Data-testid attributes present
- [ ] Components can be unit tested
- [ ] E2E test paths clear
- [ ] States easily testable

---

## Implementation Priority

1. **High Priority**: Core functionality (fetch, display, basic styling)
2. **Medium Priority**: Responsive design, loading states
3. **Low Priority**: Visual polish, animations, advanced error recovery

**Remember**: This is throwaway code for technical validation. Focus on proving the stack works, not creating production-ready design.

**Next Phase**: React Development - Implement components based on this specification