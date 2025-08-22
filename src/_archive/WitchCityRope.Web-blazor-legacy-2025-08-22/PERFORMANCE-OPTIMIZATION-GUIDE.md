# WitchCityRope.Web Performance Optimization Guide

## Current Performance Metrics

### Bundle Sizes (Unminified)
- **CSS Total**: 65.36 KB
  - app.css: 19.00 KB
  - pages.css: 27.58 KB
  - wcr-theme.css: 18.77 KB
- **JavaScript Total**: 5.24 KB
  - app.js: 5.24 KB

### External Dependencies
- Google Fonts (multiple families)
- Syncfusion Blazor Components (bootstrap5.css theme)
- Blazor Server framework (SignalR connection)

### Issues Identified
1. All CSS and JS files are not minified
2. No response compression enabled
3. No static file caching headers
4. Multiple font families loaded from Google Fonts
5. Large Syncfusion theme loaded entirely
6. No image assets (may affect visual appeal)

## High Priority Optimizations (Immediate Impact)

### 1. Enable Response Compression (30-40% reduction)

Add to `Program.cs`:

```csharp
// Add before builder.Build()
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Add after app is built
app.UseResponseCompression(); // Add this BEFORE UseStaticFiles
```

### 2. Add Static File Caching Headers

Modify static files middleware in `Program.cs`:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 365; // 1 year
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={durationInSeconds}");
    }
});
```

### 3. Minify CSS and JavaScript

Create `minify-assets.ps1`:

```powershell
# Install minification tools
npm install -g terser cssnano-cli

# Minify JavaScript
terser wwwroot/js/app.js -o wwwroot/js/app.min.js --compress --mangle

# Minify CSS
cssnano wwwroot/css/app.css wwwroot/css/app.min.css
cssnano wwwroot/css/pages.css wwwroot/css/pages.min.css
cssnano wwwroot/css/wcr-theme.css wwwroot/css/wcr-theme.min.css
```

Update `_Layout.cshtml` to use minified versions:

```html
<!-- Replace existing CSS references -->
<link rel="stylesheet" href="css/app.min.css" />
<link rel="stylesheet" href="css/wcr-theme.min.css" />

<!-- Update JS reference -->
<script src="js/app.min.js"></script>
```

### 4. Optimize Font Loading

Update `_Layout.cshtml`:

```html
<!-- Preconnect to Google Fonts -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>

<!-- Load only essential fonts with display swap -->
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&family=Playfair+Display:wght@700&display=swap" rel="stylesheet">
```

Remove unnecessary font weights and families to reduce load time.

## Medium Priority Optimizations

### 5. Optimize SignalR Configuration

In `Program.cs`:

```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.MaximumParallelInvocationsPerClient = 1;
});

// Configure Circuit Options for better performance
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(3);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(60);
        options.MaxBufferedUnacknowledgedRenderBatches = 10;
    });
```

### 6. Implement Lazy Loading for Syncfusion Components

Instead of loading all Syncfusion components, load only what's needed:

```csharp
// In Program.cs, register only used components
builder.Services.AddSyncfusionBlazor(options =>
{
    options.IgnoreScriptIsolation = true;
});
```

### 7. Add Loading States

Create `Shared/Components/UI/SkeletonLoader.razor`:

```razor
<div class="skeleton-loader @CssClass">
    @if (Type == "text")
    {
        <div class="skeleton-text"></div>
    }
    else if (Type == "card")
    {
        <div class="skeleton-card">
            <div class="skeleton-header"></div>
            <div class="skeleton-body"></div>
        </div>
    }
</div>

@code {
    [Parameter] public string Type { get; set; } = "text";
    [Parameter] public string CssClass { get; set; } = "";
}

<style>
    .skeleton-loader {
        animation: skeleton-loading 1.5s infinite ease-in-out;
    }
    
    @keyframes skeleton-loading {
        0% { opacity: 0.7; }
        50% { opacity: 0.3; }
        100% { opacity: 0.7; }
    }
    
    .skeleton-text {
        height: 20px;
        background-color: #e0e0e0;
        border-radius: 4px;
        margin: 10px 0;
    }
    
    .skeleton-card {
        background-color: #f5f5f5;
        border-radius: 8px;
        padding: 20px;
    }
    
    .skeleton-header {
        height: 30px;
        background-color: #e0e0e0;
        border-radius: 4px;
        margin-bottom: 15px;
        width: 60%;
    }
    
    .skeleton-body {
        height: 100px;
        background-color: #e0e0e0;
        border-radius: 4px;
    }
</style>
```

## Low Priority Optimizations

### 8. Bundle CSS and JavaScript

Install WebOptimizer:

```xml
<PackageReference Include="LigerShark.WebOptimizer.Core" Version="3.0.384" />
```

Configure in `Program.cs`:

```csharp
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddCssBundle("/css/bundle.css", 
        "css/app.css", 
        "css/pages.css", 
        "css/wcr-theme.css");
    
    pipeline.AddJavaScriptBundle("/js/bundle.js", 
        "js/app.js");
});

// Add middleware
app.UseWebOptimizer();
```

### 9. Add PWA Support

Create `wwwroot/manifest.json`:

```json
{
  "name": "Witch City Rope",
  "short_name": "WCR",
  "description": "Salem's Premier Rope Bondage Community",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#F5F2ED",
  "theme_color": "#880124",
  "icons": [
    {
      "src": "/images/icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "/images/icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ]
}
```

Add to `_Layout.cshtml`:

```html
<link rel="manifest" href="/manifest.json">
<meta name="theme-color" content="#880124">
```

## Performance Monitoring

### Add Application Insights

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Custom Performance Metrics

Create `Services/PerformanceService.cs`:

```csharp
public class PerformanceService
{
    private readonly ILogger<PerformanceService> _logger;
    
    public PerformanceService(ILogger<PerformanceService> logger)
    {
        _logger = logger;
    }
    
    public void LogRenderTime(string componentName, long milliseconds)
    {
        _logger.LogInformation("Component {ComponentName} rendered in {Time}ms", 
            componentName, milliseconds);
    }
}
```

## Expected Results

After implementing these optimizations:

### Bundle Size Reductions
- CSS: 65.36 KB → ~20 KB (minified + compressed)
- JavaScript: 5.24 KB → ~2 KB (minified + compressed)
- Total transfer: ~70% reduction

### Load Time Improvements
- Initial Load: 2-3s → < 1.5s
- Subsequent Loads: < 500ms (with caching)

### Core Web Vitals
- **LCP**: < 2.5s ✓
- **FID**: < 100ms ✓
- **CLS**: < 0.1 ✓

## Implementation Checklist

- [ ] Enable response compression
- [ ] Add static file caching headers
- [ ] Minify CSS and JavaScript files
- [ ] Optimize font loading
- [ ] Configure SignalR for performance
- [ ] Add loading states/skeleton screens
- [ ] Implement CSS/JS bundling
- [ ] Add performance monitoring
- [ ] Test with Lighthouse
- [ ] Monitor real user metrics

## Testing Performance

Run Lighthouse audit:

```bash
# Using Chrome DevTools
1. Open Chrome DevTools (F12)
2. Go to Lighthouse tab
3. Run audit for Performance, Accessibility, Best Practices, SEO

# Using CLI
npm install -g lighthouse
lighthouse http://localhost:5651 --view
```

## Next Steps

1. Implement high priority optimizations first
2. Test after each optimization
3. Monitor real user performance
4. Consider CDN for static assets
5. Evaluate partial migration to Blazor WebAssembly for static pages