# Technology Research: React Charting Library Selection
<!-- Last Updated: 2026-03-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Final -->

## Executive Summary

**Decision Required**: Select a charting library for React + TypeScript + Vite + Mantine v7 dashboard with bar charts, line charts, and interactive click-to-drill-down capability.

**Recommendation**: **@mantine/charts** (Confidence: High, 90%)

**Key Factors**:
1. Already using Mantine v7 extensively -- zero new design system integration needed
2. Built on recharts, providing escape hatches to recharts API for click handlers
3. Simplest path: one `npm install` adds charts that match existing UI perfectly

## Research Scope

### Requirements
- Bar charts for daily transaction data
- Line charts for trend visualization over time
- Interactive charts: clicking a data point triggers detail view
- Clean styling that integrates with existing Mantine v7 design system
- Strong TypeScript support
- Active maintenance and healthy community
- Works with Vite build system (no SSR needed, SPA only)

### Success Criteria
- Seamless visual integration with Mantine v7 theme (colors, spacing, fonts)
- Click event handlers that provide data point context for drill-down
- TypeScript types for chart props and event callbacks
- Minimal bundle size impact (mobile users on limited bandwidth)
- Straightforward developer experience (volunteer development team)

### Out of Scope
- 3D charts, geographic maps, or real-time streaming data
- Server-side rendering requirements
- Charts with 10,000+ data points (typical use is daily data, dozens to hundreds of points)

## Technology Options Evaluated

---

### Option 1: @mantine/charts

**Overview**: Official Mantine charting package. A wrapper around recharts that provides Mantine-styled chart components with built-in theme integration.

**Version Evaluated**: 7.17.8 (matches current Mantine version in project)
**Documentation Quality**: Excellent -- integrated into Mantine docs site with live demos

**Pros**:
- **Native Mantine integration**: Automatically uses Mantine theme colors, fonts, spacing, and dark mode
- **Already in ecosystem**: Same version scheme as existing @mantine/core, @mantine/dates, @mantine/form, etc.
- **Recharts escape hatch**: `barProps`, `lineChartProps`, `dotProps`, `activeDotProps` allow passing recharts props directly, including onClick handlers
- **13 chart types**: BarChart, LineChart, AreaChart, CompositeChart, DonutChart, PieChart, FunnelChart, RadarChart, ScatterChart, BubbleChart, RadialBarChart, Sparkline, Heatmap
- **CSS Modules styling**: Matches project's existing Mantine v7 CSS Modules approach (no runtime CSS-in-JS)
- **Consistent API**: Same BoxProps, StylesApiProps patterns used across all Mantine components
- **Developer familiarity**: Team already knows Mantine prop patterns

**Cons**:
- **Abstraction layer**: Some advanced recharts features require knowing the underlying recharts API
- **Click handling not first-class**: No dedicated `onBarClick` or `onDotClick` prop; must use `barProps={{ onClick: handler }}` or `dotProps={{ onClick: handler }}` passthrough
- **Bundle overhead**: Adds recharts as a transitive dependency (~135kB minified for recharts core)
- **Tied to recharts version**: Mantine v7 pins to recharts 2.x; Mantine 9+ requires recharts 3.x

**Click Interaction Pattern**:
```typescript
// Bar chart click handling via barProps passthrough
<BarChart
  data={dailyTransactions}
  dataKey="date"
  series={[{ name: 'amount', color: 'blue.6' }]}
  barProps={(series) => ({
    onClick: (data, index, event) => {
      // data.payload contains the full data point
      onTransactionDayClick(data.payload.date);
    }
  })}
/>

// Line chart click handling via dotProps passthrough
<LineChart
  data={trendData}
  dataKey="month"
  series={[{ name: 'revenue', color: 'teal.6' }]}
  dotProps={{
    onClick: (data, index, event) => {
      onTrendPointClick(data.payload);
    }
  }}
/>
```

**WitchCityRope Fit**:
- Safety/Privacy: No external dependencies or CDN calls; all data stays local
- Mobile Experience: SVG-based charts scale cleanly on mobile viewports
- Learning Curve: Minimal -- team already uses Mantine daily
- Community Values: Open source, MIT licensed, part of Mantine ecosystem (26k+ GitHub stars for Mantine overall)

---

### Option 2: Recharts (directly)

**Overview**: The most popular React charting library, built on D3 and React. Declarative, composable chart components using SVG rendering.

**Version Evaluated**: 3.8.0 (latest as of March 2026)
**Documentation Quality**: Good -- comprehensive API docs with examples

**Pros**:
- **Most popular React charting lib**: ~24.8k GitHub stars, ~16M weekly npm downloads
- **Declarative React API**: Composable components (`<BarChart>`, `<Bar>`, `<XAxis>`, `<Tooltip>`, etc.)
- **Strong TypeScript support**: Full type definitions with good IDE autocomplete
- **First-class click handlers**: Direct `onClick` prop on `<Bar>`, `<Line>`, `<Dot>` components
- **Active maintenance**: v3.8.0 released March 2026, frequent releases
- **Excellent documentation**: Well-organized with many examples
- **Large ecosystem**: Many community examples, tutorials, Stack Overflow answers

**Cons**:
- **No Mantine theme integration**: Must manually match colors, fonts, spacing to Mantine theme
- **Bundle size**: ~135kB minified (not tree-shakeable effectively -- D3 submodules included)
- **SVG rendering only**: Can be slow with very large datasets (not an issue for this use case)
- **Version mismatch risk**: Mantine v7 charts uses recharts 2.x; using recharts 3.x directly means two different APIs in one project if also using @mantine/charts
- **Styling effort**: Requires custom CSS/styling work to look native alongside Mantine components

**Click Interaction Pattern**:
```typescript
<BarChart data={dailyTransactions}>
  <XAxis dataKey="date" />
  <YAxis />
  <Tooltip />
  <Bar
    dataKey="amount"
    fill="#228be6"
    onClick={(data, index) => {
      onTransactionDayClick(data.payload.date);
    }}
  />
</BarChart>
```

**WitchCityRope Fit**:
- Safety/Privacy: No external dependencies; all local
- Mobile Experience: SVG scales well on mobile
- Learning Curve: Moderate -- new API to learn, styling integration effort
- Community Values: MIT licensed, very active open source community

---

### Option 3: react-chartjs-2 (Chart.js)

**Overview**: React wrapper for Chart.js, the most widely used JavaScript charting library. Uses Canvas rendering.

**Version Evaluated**: 5.3.1 (wrapping Chart.js 4.x)
**Documentation Quality**: Moderate -- relies heavily on Chart.js docs

**Pros**:
- **Canvas rendering**: Better performance with large datasets
- **Tree-shakeable**: Import only needed chart types and plugins; can reduce to ~68kB gzipped
- **Animations built-in**: Smooth transitions out of the box
- **Responsive by default**: Charts resize automatically
- **Wide adoption**: 6.8k GitHub stars, ~1.6M weekly downloads
- **25+ chart types**: Very comprehensive chart type support

**Cons**:
- **Canvas rendering**: Text/elements not accessible as DOM nodes; harder to style consistently with Mantine
- **Imperative feel**: Chart.js configuration objects feel less "React-like" than declarative components
- **Click handling complexity**: Requires `useRef` + `getElementAtEvent` utility; more boilerplate than recharts
- **No Mantine integration**: Must manually coordinate colors, dark mode, theming
- **TypeScript experience**: Types come from Chart.js, not optimized for React patterns; requires importing ChartData, ChartOptions generics
- **Two paradigms**: Chart.js config objects vs React component props creates cognitive overhead

**Click Interaction Pattern**:
```typescript
const chartRef = useRef<ChartJS>(null);

const onClick = (event: MouseEvent<HTMLCanvasElement>) => {
  const chart = chartRef.current;
  if (!chart) return;
  const elements = getElementAtEvent(chart, event);
  if (elements.length > 0) {
    const { datasetIndex, index } = elements[0];
    const dataPoint = data.datasets[datasetIndex].data[index];
    onTransactionDayClick(data.labels[index]);
  }
};

<Bar ref={chartRef} data={data} options={options} onClick={onClick} />
```

**WitchCityRope Fit**:
- Safety/Privacy: No external dependencies; all local
- Mobile Experience: Canvas renders crisply but less flexible for responsive layouts
- Learning Curve: Moderate-high -- Chart.js config paradigm differs from React patterns
- Community Values: MIT licensed, strong open source community

---

### Option 4: Nivo (@nivo/bar, @nivo/line)

**Overview**: Rich data visualization components built on D3 and React. Modular package structure -- install only what you need.

**Version Evaluated**: 0.99.0
**Documentation Quality**: Good -- interactive playground on nivo.rocks

**Pros**:
- **Multiple renderers**: SVG, Canvas, and HTML rendering options
- **Beautiful defaults**: Charts look polished out of the box
- **Modular packages**: `@nivo/bar` (~343kB), `@nivo/line` (~371kB) -- install only needed types
- **Interactive playground**: nivo.rocks lets you configure charts visually
- **Click handlers**: Built-in `onClick` prop on chart components
- **Responsive support**: Built-in responsive wrapper components
- **Theming system**: Has its own theming but can be customized

**Cons**:
- **Large per-package size**: @nivo/bar alone is 343kB; @nivo/line is 371kB; combined is significantly larger than alternatives
- **No Mantine integration**: Separate theming system requires manual alignment
- **Peer dependency issues**: Has historically required `--legacy-peer-deps` flag; React 19 compatibility issues reported
- **Moderate maintenance**: Last release ~10 months ago (0.99.0); slower release cadence
- **Learning curve**: Different API patterns from both Mantine and recharts
- **Overkill**: Many features (server-side rendering, HTML rendering) unnecessary for this use case

**Click Interaction Pattern**:
```typescript
<ResponsiveBar
  data={dailyTransactions}
  keys={['amount']}
  indexBy="date"
  onClick={(datum) => {
    onTransactionDayClick(datum.data.date);
  }}
/>
```

**WitchCityRope Fit**:
- Safety/Privacy: No external dependencies; all local
- Mobile Experience: Responsive wrappers work well
- Learning Curve: Moderate-high -- entirely new API to learn
- Community Values: MIT licensed, 13.8k GitHub stars

---

### Option 5: react-google-charts

**Overview**: Thin React wrapper for Google Charts Visualization API. Charts are rendered by Google's JavaScript library loaded from Google CDN at runtime.

**Version Evaluated**: 5.2.1
**Documentation Quality**: Moderate -- wraps Google Charts docs

**Pros**:
- **25+ chart types**: Comprehensive chart support
- **Familiar**: Already used in sister project
- **Small wrapper size**: The npm package itself is only ~55kB
- **Google polish**: Charts have a clean, professional look
- **TypeScript types**: Fully typed wrapper

**Cons**:
- **CRITICAL: External CDN dependency**: Google Charts JS library is loaded from gstatic.com at runtime; REQUIRES internet connection
- **CRITICAL: No offline support**: Google ToS prohibits offline use of chart libraries
- **CRITICAL: Privacy concern**: Every chart render makes requests to Google servers
- **No Mantine integration**: Google's Material Design styling conflicts with Mantine aesthetic
- **Slow initial load**: Must download Google Charts from CDN before rendering
- **Maintenance concerns**: Last npm publish was over a year ago; slower community response
- **Limited customization**: Constrained to Google Charts styling options
- **Click handling**: Uses Google Charts event system, not React patterns; `chartEvents` prop

**Click Interaction Pattern**:
```typescript
<Chart
  chartType="BarChart"
  data={chartData}
  chartEvents={[{
    eventName: 'select',
    callback: ({ chartWrapper }) => {
      const selection = chartWrapper.getChart().getSelection();
      if (selection.length > 0) {
        const row = selection[0].row;
        onTransactionDayClick(chartData[row + 1][0]);
      }
    }
  }]}
/>
```

**WitchCityRope Fit**:
- Safety/Privacy: **FAIL** -- external CDN calls to Google on every render; privacy concern for community platform
- Mobile Experience: Depends on CDN load speed; poor on slow connections
- Learning Curve: Low if already familiar from sister project
- Community Values: Proprietary Google dependency conflicts with open-source values

---

### Option 6: Tremor (@tremor/react)

**Overview**: Dashboard-focused React component library built on Tailwind CSS and Recharts. Provides pre-built dashboard components including charts.

**Version Evaluated**: 3.18.7
**Documentation Quality**: Good -- focused on dashboard use cases

**Pros**:
- **Dashboard-focused**: KPI cards, metric displays, and charts designed to work together
- **Built on recharts**: Familiar underlying library
- **Beautiful defaults**: Polished dashboard aesthetic
- **TypeScript support**: Full TypeScript types
- **Active community**: 16.4k GitHub stars

**Cons**:
- **CRITICAL: Tailwind CSS dependency**: Built on Tailwind CSS; WitchCityRope uses Mantine CSS Modules -- known styling conflicts between Tailwind preflight and Mantine
- **Design system conflict**: Tremor has its own design tokens (Radix UI based); conflicts with Mantine design system
- **Dual styling systems**: Would require managing both Tailwind and Mantine CSS Modules in the same project
- **CSS specificity battles**: Tailwind's utility classes conflict with Mantine's CSS Modules; requires `@layer` workarounds
- **Redundant dependency**: Adds recharts as transitive dependency just like @mantine/charts would, plus Tailwind overhead
- **Last publish**: Over a year ago (January 2025)

**WitchCityRope Fit**:
- Safety/Privacy: No external dependencies; all local
- Mobile Experience: Good responsive design
- Learning Curve: **High** -- introduces Tailwind CSS system alongside existing Mantine
- Community Values: MIT licensed, but introduces architectural friction

---

## Comparative Analysis

| Criteria | Weight | @mantine/charts | Recharts | Chart.js | Nivo | Google Charts | Tremor |
|----------|--------|-----------------|----------|----------|------|---------------|--------|
| **Mantine Integration** | 25% | 10/10 | 5/10 | 4/10 | 4/10 | 3/10 | 2/10 |
| **Click Interaction** | 20% | 7/10 | 9/10 | 6/10 | 9/10 | 5/10 | 7/10 |
| **TypeScript Quality** | 15% | 9/10 | 8/10 | 6/10 | 7/10 | 7/10 | 8/10 |
| **Bundle Size** | 10% | 6/10 | 6/10 | 8/10 | 4/10 | 3/10* | 5/10 |
| **Developer Experience** | 10% | 9/10 | 8/10 | 5/10 | 7/10 | 6/10 | 6/10 |
| **Community/Maintenance** | 10% | 9/10 | 10/10 | 8/10 | 6/10 | 5/10 | 7/10 |
| **Simplicity** | 10% | 10/10 | 7/10 | 5/10 | 6/10 | 7/10 | 4/10 |
| **Weighted Total** | 100% | **8.6** | **7.3** | **5.8** | **6.1** | **4.7** | **5.1** |

*Google Charts: npm package is small but loads ~200kB+ from external CDN at runtime.

### Scoring Rationale

**Mantine Integration (25% weight)**: This is the dominant factor. The project already uses 8 Mantine packages. Adding @mantine/charts means zero design system integration work. Every other option requires manual theme coordination.

**Click Interaction (20% weight)**: Core requirement. Recharts and Nivo have the most direct click APIs. @mantine/charts supports clicks via prop passthrough -- slightly less ergonomic but fully functional. Chart.js requires ref-based imperative handling.

**TypeScript Quality (15%)**: @mantine/charts inherits Mantine's excellent TypeScript patterns (BoxProps, StylesApiProps). Recharts has good types. Chart.js TypeScript experience involves verbose generic configurations.

**Bundle Size (10%)**: All options add ~100-370kB. Since @mantine/charts adds recharts as its only significant new dependency, and the project already accepts Mantine's footprint, this is a lower-weight differentiator.

**Simplicity (10%)**: The user explicitly values "not reinventing the wheel." @mantine/charts is the simplest option for a Mantine v7 project.

## Implementation Considerations

### If @mantine/charts Is Selected (Recommended)

**Installation**:
```bash
npm install @mantine/charts recharts@^2
```

**Setup** (in app entry point, alongside existing Mantine imports):
```typescript
import '@mantine/core/styles.css';
import '@mantine/charts/styles.css';  // Add this line
```

**Migration Path**: None needed -- this is a new addition, not a migration.

**Estimated Effort**: 2-4 hours per chart page (including click interaction wiring)

### Integration with Existing Architecture

- **Mantine Theme**: Charts automatically use the project's existing Mantine theme (colors, fonts, dark mode)
- **TypeScript**: Types follow existing Mantine patterns; no new type paradigms to learn
- **Vite**: No special configuration needed; recharts + @mantine/charts are standard ESM packages
- **Testing**: Charts render as SVG in DOM; testable with existing @testing-library/react setup

### Click Interaction Implementation Strategy

Since @mantine/charts provides click handling via recharts prop passthrough rather than dedicated props, the recommended pattern is:

```typescript
// Create a reusable hook for chart click handling
function useChartDrillDown<T>(onDrillDown: (item: T) => void) {
  return {
    barProps: {
      onClick: (data: { payload: T }) => onDrillDown(data.payload),
      style: { cursor: 'pointer' }
    },
    dotProps: {
      onClick: (data: { payload: T }) => onDrillDown(data.payload),
      style: { cursor: 'pointer' }
    }
  };
}

// Usage in a component
function DailyTransactionsChart({ onDayClick }: Props) {
  const { barProps } = useChartDrillDown<TransactionDay>(onDayClick);

  return (
    <BarChart
      h={300}
      data={dailyTransactions}
      dataKey="date"
      series={[{ name: 'amount', color: 'blue.6' }]}
      barProps={barProps}
    />
  );
}
```

### Performance Impact

- **Bundle addition**: ~135kB minified for recharts (the significant new dependency)
- **@mantine/charts wrapper**: ~15-20kB additional on top of recharts
- **Runtime**: SVG rendering; performant for datasets up to ~1000 data points
- **Tree shaking**: Limited for recharts (D3 submodules are bundled); however, since @mantine/charts uses recharts, the cost is paid once

## Risk Assessment

### Low Risk
- **@mantine/charts click handling less ergonomic than raw recharts**
  - **Mitigation**: Create a `useChartDrillDown` hook (shown above) that encapsulates the passthrough pattern; used once, abstracted forever

- **Recharts 2.x to 3.x migration when upgrading to Mantine 8/9**
  - **Mitigation**: Mantine team will handle recharts compatibility in their wrapper; migration follows standard Mantine upgrade path
  - **Monitoring**: Watch Mantine GitHub issues for recharts 3.x migration timeline

### Very Low Risk
- **Bundle size increase**
  - **Mitigation**: recharts ~135kB is within acceptable range for a charting library; the project already includes multiple heavier dependencies (TipTap editor, PayPal SDK, etc.)

### Non-Risks (Eliminated by Choice)
- No external CDN dependency (eliminated Google Charts)
- No CSS framework conflicts (eliminated Tremor/Tailwind)
- No new design system to learn (eliminated Nivo, Chart.js)

## Recommendation

### Primary Recommendation: @mantine/charts
**Confidence Level**: High (90%)

**Rationale**:
1. **Zero integration friction**: The project uses 8 Mantine packages already (@mantine/core, dates, form, hooks, modals, notifications, tiptap). Adding @mantine/charts follows the exact same pattern -- install, import styles, use components. No new design system, no theming conflicts, no CSS specificity battles.

2. **Click interaction works**: While not first-class props, the `barProps` and `dotProps` passthrough to recharts provides full click handling capability. A simple custom hook makes this ergonomic.

3. **Not reinventing the wheel**: This is the library that Mantine's creator (Vitaly Rtishchev) built specifically for Mantine users. It exists to solve exactly this problem. Using raw recharts or Chart.js alongside Mantine means doing integration work that @mantine/charts already did.

4. **Recharts underneath**: If you ever need something @mantine/charts doesn't expose, you can pass recharts props directly through the various `*Props` escape hatches. You get Mantine convenience with recharts power.

5. **Future-proof within Mantine ecosystem**: As the project upgrades Mantine versions, charts upgrade in lockstep.

**Implementation Priority**: Immediate -- ready to install and use

### Alternative Recommendation
- **Second Choice**: Recharts (directly) -- If @mantine/charts abstraction becomes too limiting for advanced chart customization. However, this adds manual Mantine theme integration work.
- **Not Recommended**: Google Charts (external CDN dependency + privacy concerns), Tremor (Tailwind conflicts with Mantine), Nivo (large bundles, slower maintenance), Chart.js (imperative API feel, Canvas rendering mismatch)

## Next Steps
- [ ] Install @mantine/charts and recharts: `npm install @mantine/charts@^7.17 recharts@^2`
- [ ] Add `import '@mantine/charts/styles.css'` to app entry point
- [ ] Create `useChartDrillDown` hook for click interaction pattern
- [ ] Build first chart component (bar chart for daily transactions)
- [ ] Validate click-to-detail interaction works as expected

## Research Sources
- [Mantine Charts - Getting Started](https://mantine.dev/charts/getting-started/)
- [Mantine BarChart Documentation](https://mantine.dev/charts/bar-chart/)
- [Mantine LineChart Documentation](https://mantine.dev/charts/line-chart/)
- [Mantine BarChart Source (GitHub)](https://github.com/mantinedev/mantine/blob/master/packages/@mantine/charts/src/BarChart/BarChart.tsx)
- [Mantine v7.4.0 Changelog (Charts introduction)](https://mantine.dev/changelog/7-4-0/)
- [Recharts GitHub Repository](https://github.com/recharts/recharts)
- [Recharts 3.0 Migration Guide](https://github.com/recharts/recharts/wiki/3.0-migration-guide)
- [react-chartjs-2 Events Documentation](https://react-chartjs-2.js.org/docs/working-with-events/)
- [Nivo Homepage](https://nivo.rocks/)
- [React Google Charts](https://www.react-google-charts.com/)
- [Google Charts Offline Issue #80](https://github.com/RakanNimer/react-google-charts/issues/80)
- [Tremor Documentation](https://www.tremor.so/)
- [Tailwind + Mantine Conflict Discussion](https://github.com/orgs/mantinedev/discussions/1672)
- [LogRocket: Best React Chart Libraries 2025](https://blog.logrocket.com/best-react-chart-libraries-2025/)
- [Syncfusion: Top 5 React Chart Libraries 2026](https://www.syncfusion.com/blogs/post/top-5-react-chart-libraries)
- [Mantine Charts Click Discussion (GitHub #6305)](https://github.com/orgs/mantinedev/discussions/6305)
- [Recharts Bar onClick (GitHub #94)](https://github.com/recharts/recharts/issues/94)
- [Mantine Charts NPM](https://www.npmjs.com/package/@mantine/charts)

## Quality Gate Checklist (100% Complete)
- [x] Multiple options evaluated (6 options)
- [x] Quantitative comparison provided (weighted scoring matrix)
- [x] WitchCityRope-specific considerations addressed (privacy, mobile, community values)
- [x] Performance impact assessed (bundle sizes, rendering approach)
- [x] Security implications reviewed (external CDN risks for Google Charts)
- [x] Mobile experience considered (SVG scaling, bandwidth impact)
- [x] Implementation path defined (installation steps, hook pattern, code examples)
- [x] Risk assessment completed (low risk items with mitigations)
- [x] Clear recommendation with rationale (5-point justification)
- [x] Sources documented for verification (17 sources)
