# Syncfusion Blazor Component Mapping Guide

This document maps the Witch City Rope UI components to their Syncfusion Blazor equivalents, including customization requirements to match our design system.

## Design System Overview

### Color Palette
```css
/* Primary Colors */
--color-burgundy: #880124;
--color-plum: #614B79;

/* CTA Accent */
--color-amber: #FFBF00;
--color-amber-dark: #E6AC00;

/* Typography */
--font-heading: 'Montserrat', sans-serif;
--font-body: 'Source Sans 3', sans-serif;
```

## Component Mappings

### Navigation Components

#### 1. Main Navigation Bar
**Syncfusion Component**: `SfAppBar` + `SfMenu`
```razor
<SfAppBar ColorMode="AppBarColor.Light" IsSticky="true">
    <SfMenu Items="@MenuItems" CssClass="wcr-nav-menu">
        <MenuFieldSettings Text="Text" Url="Url" />
    </SfMenu>
</SfAppBar>
```
**Custom CSS Required**:
- Background: rgba(255, 248, 240, 0.95) with backdrop-filter
- Custom hover effects with underline animation
- Sticky behavior with scroll shadow

#### 2. Mobile Menu
**Syncfusion Component**: `SfSidebar` + `SfTreeView`
```razor
<SfSidebar @ref="Sidebar" Width="300px" Position="SidebarPosition.Right">
    <SfTreeView TValue="MenuItem" CssClass="wcr-mobile-nav">
        <TreeViewFieldsSettings DataSource="@MenuItems" />
    </SfTreeView>
</SfSidebar>
```

#### 3. Breadcrumb Navigation
**Syncfusion Component**: `SfBreadcrumb`
```razor
<SfBreadcrumb Items="@BreadcrumbItems" CssClass="wcr-breadcrumb">
    <BreadcrumbTemplates>
        <SeparatorTemplate>
            <span class="e-icons e-arrow-right"></span>
        </SeparatorTemplate>
    </BreadcrumbTemplates>
</SfBreadcrumb>
```

### Form Components

#### 1. Text Input Fields
**Syncfusion Component**: `SfTextBox`
```razor
<SfTextBox Placeholder="Scene Name" CssClass="wcr-input" FloatLabelType="FloatLabelType.Auto">
    <TextBoxEvents Focus="@HandleFocus" Blur="@HandleBlur" />
</SfTextBox>
```
**Custom Styling**:
- Background: var(--color-cream)
- Border: 2px solid var(--color-taupe)
- Focus border: var(--color-burgundy)
- Padding: 16px
- Border-radius: 8px

#### 2. Dropdown/Select
**Syncfusion Component**: `SfDropDownList`
```razor
<SfDropDownList TValue="string" TItem="EventType" Placeholder="Select event type"
                 DataSource="@EventTypes" CssClass="wcr-dropdown">
    <DropDownListFieldSettings Text="Name" Value="Id" />
</SfDropDownList>
```

#### 3. Multi-line Text Area
**Syncfusion Component**: `SfTextBox` with Multiline
```razor
<SfTextBox Multiline="true" Placeholder="Tell us about yourself..." 
           CssClass="wcr-textarea" Rows="6" />
```

#### 4. Checkbox
**Syncfusion Component**: `SfCheckBox`
```razor
<SfCheckBox Label="I agree to the terms" CssClass="wcr-checkbox" />
```

#### 5. Radio Buttons
**Syncfusion Component**: `SfRadioButton`
```razor
<SfRadioButton Label="Public Event" Name="eventType" Value="public" 
               CssClass="wcr-radio" @bind-Checked="SelectedType" />
```

#### 6. Toggle Switch
**Syncfusion Component**: `SfSwitch`
```razor
<SfSwitch @bind-Checked="EnableNotifications" CssClass="wcr-toggle">
    <SwitchEvents ValueChange="@HandleToggleChange" />
</SfSwitch>
```

#### 7. Date/Time Picker
**Syncfusion Component**: `SfDateTimePicker`
```razor
<SfDateTimePicker TValue="DateTime?" Placeholder="Select date and time"
                  CssClass="wcr-datetime" Format="MMM dd, yyyy h:mm tt">
    <DateTimePickerEvents ValueChange="@HandleDateChange" />
</SfDateTimePicker>
```

#### 8. File Upload
**Syncfusion Component**: `SfUploader`
```razor
<SfUploader AutoUpload="false" CssClass="wcr-uploader"
            AllowedExtensions=".jpg,.jpeg,.png,.pdf">
    <UploaderTemplates>
        <Template>
            <div class="wcr-upload-area">
                <i class="fas fa-cloud-upload-alt"></i>
                <p>Drop files here or click to browse</p>
            </div>
        </Template>
    </UploaderTemplates>
</SfUploader>
```

### Button Components

#### 1. Primary Button (CTA)
**Syncfusion Component**: `SfButton`
```razor
<SfButton CssClass="wcr-btn-primary" IsPrimary="true">
    Register Now
</SfButton>
```
**Custom CSS**:
```css
.wcr-btn-primary {
    background: linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%);
    color: var(--color-midnight);
    padding: 14px 32px;
    border-radius: 4px;
    text-transform: uppercase;
    letter-spacing: 1.5px;
    font-weight: 700;
}
```

#### 2. Secondary Button
**Syncfusion Component**: `SfButton`
```razor
<SfButton CssClass="wcr-btn-secondary">
    Learn More
</SfButton>
```

### Data Display Components

#### 1. Data Tables
**Syncfusion Component**: `SfGrid`
```razor
<SfGrid DataSource="@Events" CssClass="wcr-grid" AllowPaging="true" AllowSorting="true">
    <GridColumns>
        <GridColumn Field="@nameof(Event.Name)" HeaderText="Event Name" />
        <GridColumn Field="@nameof(Event.Date)" HeaderText="Date" Format="d" />
        <GridColumn Field="@nameof(Event.Status)" HeaderText="Status">
            <Template>
                <span class="wcr-badge wcr-badge-@context.Status">@context.Status</span>
            </Template>
        </GridColumn>
    </GridColumns>
</SfGrid>
```

#### 2. Cards
**Syncfusion Component**: Custom implementation with `SfCard` or div
```razor
<div class="wcr-card">
    <div class="wcr-card-header">
        <h3>@Event.Title</h3>
        <span class="wcr-badge">@Event.Type</span>
    </div>
    <div class="wcr-card-body">
        <p>@Event.Description</p>
    </div>
    <div class="wcr-card-footer">
        <SfButton CssClass="wcr-btn-primary">Register</SfButton>
    </div>
</div>
```

#### 3. Progress Indicators
**Syncfusion Component**: `SfProgressBar`
```razor
<SfProgressBar Type="ProgressType.Linear" Value="@Progress" Height="8"
               CssClass="wcr-progress">
    <ProgressBarAnimation Enable="true" Duration="2000" />
</SfProgressBar>
```

### Feedback Components

#### 1. Modal Dialogs
**Syncfusion Component**: `SfDialog`
```razor
<SfDialog @bind-Visible="IsModalVisible" Width="500px" CssClass="wcr-modal"
          ShowCloseIcon="true" IsModal="true">
    <DialogTemplates>
        <Header>
            <h2>Confirm Registration</h2>
        </Header>
        <Content>
            <p>Are you sure you want to register for this event?</p>
        </Content>
        <FooterTemplate>
            <SfButton CssClass="wcr-btn-primary" @onclick="Confirm">Confirm</SfButton>
            <SfButton CssClass="wcr-btn-secondary" @onclick="Cancel">Cancel</SfButton>
        </FooterTemplate>
    </DialogTemplates>
</SfDialog>
```

#### 2. Toast/Notifications
**Syncfusion Component**: `SfToast`
```razor
<SfToast @ref="ToastObj" CssClass="wcr-toast" Position="ToastPosition.TopRight">
    <ToastTemplates>
        <Template>
            <div class="wcr-toast-content">
                <i class="@GetToastIcon()"></i>
                <span>@ToastMessage</span>
            </div>
        </Template>
    </ToastTemplates>
</SfToast>
```

#### 3. Loading Spinner
**Syncfusion Component**: `SfSpinner`
```razor
<SfSpinner @bind-Visible="IsLoading" CssClass="wcr-spinner"
          Type="SpinnerType.Material" Size="40" />
```

### Interactive Components

#### 1. Tabs
**Syncfusion Component**: `SfTab`
```razor
<SfTab CssClass="wcr-tabs">
    <TabItems>
        <TabItem Header="Profile">
            <ContentTemplate>
                <!-- Profile content -->
            </ContentTemplate>
        </TabItem>
        <TabItem Header="Security">
            <ContentTemplate>
                <!-- Security content -->
            </ContentTemplate>
        </TabItem>
    </TabItems>
</SfTab>
```

#### 2. Calendar
**Syncfusion Component**: `SfCalendar`
```razor
<SfCalendar TValue="DateTime?" CssClass="wcr-calendar"
            ShowTodayButton="true">
    <CalendarEvents ValueChange="@HandleDateSelect" />
</SfCalendar>
```

#### 3. Search Box
**Syncfusion Component**: `SfAutoComplete` or `SfTextBox`
```razor
<SfAutoComplete TValue="string" TItem="Event" Placeholder="Search events..."
                DataSource="@Events" CssClass="wcr-search">
    <AutoCompleteFieldSettings Value="Name" />
    <AutoCompleteTemplates TItem="Event">
        <ItemTemplate>
            <div class="wcr-search-item">
                <span>@context.Name</span>
                <small>@context.Date.ToString("MMM dd")</small>
            </div>
        </ItemTemplate>
    </AutoCompleteTemplates>
</SfAutoComplete>
```

## Custom Theme Implementation

### 1. Create Theme Override CSS
```css
/* wcr-theme.css */
.e-btn.wcr-btn-primary {
    background: linear-gradient(135deg, #FFBF00 0%, #E6AC00 100%);
    border: none;
    box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4);
}

.e-btn.wcr-btn-primary:hover {
    background: linear-gradient(135deg, #E6AC00 0%, #FFBF00 100%);
    transform: translateY(-2px);
    box-shadow: 0 6px 25px rgba(255, 191, 0, 0.5);
}

/* Override Syncfusion input styles */
.e-input-group.wcr-input {
    background: var(--color-cream);
    border: 2px solid var(--color-taupe);
}

.e-input-group.wcr-input.e-input-focus {
    border-color: var(--color-burgundy);
    box-shadow: 0 0 0 3px rgba(136, 1, 36, 0.1);
}
```

### 2. Register Theme in _Host.cshtml
```html
<link href="https://cdn.syncfusion.com/blazor/24.1.41/styles/bootstrap5.css" rel="stylesheet" />
<link href="css/wcr-theme.css" rel="stylesheet" />
```

### 3. Configure Syncfusion License
```csharp
// Program.cs
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY");
builder.Services.AddSyncfusionBlazor();
```

## Implementation Notes

1. **Consistent Styling**: All Syncfusion components should use the `wcr-` prefix for custom CSS classes
2. **Animations**: Add custom transitions (0.3s ease) to match the design system
3. **Responsive Behavior**: Use Syncfusion's built-in responsive features with custom breakpoints
4. **Accessibility**: Ensure all components maintain ARIA labels and keyboard navigation
5. **Performance**: Use lazy loading for data grids and virtualization for large lists

## Migration Checklist

- [ ] Install Syncfusion.Blazor NuGet package
- [ ] Register Syncfusion license
- [ ] Create wcr-theme.css with all overrides
- [ ] Implement custom templates for complex components
- [ ] Test all components for visual consistency
- [ ] Verify mobile responsiveness
- [ ] Ensure accessibility compliance
- [ ] Performance test with large datasets