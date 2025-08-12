# Email Display "mailto:" Fix

## Issue
The user reported seeing "mailto:" prefix in usernames after login.

## Investigation Results

1. **Database Check**: The test accounts in DbInitializer are correctly configured without any "mailto:" prefix:
   - admin@witchcityrope.com
   - member@witchcityrope.com
   - staff@witchcityrope.com
   - guest@witchcityrope.com
   - organizer@witchcityrope.com

2. **Code Analysis**: No "mailto:" strings found in the codebase. The issue was likely caused by:
   - Browser auto-linking email addresses
   - CSS framework behavior
   - HTML rendering of email addresses

## Fix Applied

Updated `MainLayout.razor` line 89 to:
1. Strip any "mailto:" prefix if present using `.Replace("mailto:", "")`
2. Changed from `<div>` to `<span>` to prevent potential auto-linking

### Before:
```razor
<div class="user-email">@_currentUser?.Email</div>
```

### After:
```razor
<span class="user-email">@(_currentUser?.Email?.Replace("mailto:", ""))</span>
```

## Additional Notes

- The fix is defensive - it will remove "mailto:" if it appears from any source
- The database already stores emails correctly without the prefix
- All test accounts are properly configured with the password "Test123!"

## Testing
After this change, the email display in the mobile menu should show just the email address without any "mailto:" prefix.