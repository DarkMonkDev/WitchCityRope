# Page snapshot

```yaml
- link "Report an Incident":
  - /url: /incident-report
- link "Private Lessons":
  - /url: /private-lessons
- link "Contact":
  - /url: /contact
- banner:
  - link "WITCH CITY ROPE":
    - /url: /
  - link "Events & Classes":
    - /url: /events
  - link "How to Join":
    - /url: /join
  - link "Resources":
    - /url: /resources
  - link "Login":
    - /url: /login
- main:
  - heading "Events Management API Integration Demo" [level=1]
  - heading "API Integration Demo" [level=2]
  - tablist:
    - tab "Current API (Working)" [selected]
    - tab "Future Events Management API"
  - tabpanel "Current API (Working)":
    - alert:
      - img
      - strong: "Current API Integration:"
      - text: This demonstrates the working API integration with the existing backend endpoints.
    - heading "Published Events (Current API)" [level=3]
    - button "Refresh Events"
- button "Open Tanstack query devtools":
  - img
```