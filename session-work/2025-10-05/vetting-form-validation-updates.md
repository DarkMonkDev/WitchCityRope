# Vetting Form Validation Updates - 2025-10-05

## Summary

Two tasks completed for the vetting application form:
1. **Removed minimum character limits** from two text fields
2. **Enhanced error logging** for 400 Bad Request debugging

## Task 1: Remove Character Limits

### User Request
Remove the minimum character limit on both of these fields:
- "Why would you like to join" field - remove 20 character minimum
- "What is your rope experience" field - remove 50 character minimum

### Changes Made

#### Frontend Changes

**1. Schema Validation (`/apps/web/src/features/vetting/schemas/simplifiedApplicationSchema.ts`)**

```typescript
// BEFORE:
whyJoin: z.string()
  .min(20, 'Please provide at least 20 characters explaining why you would like to join')
  .max(2000, 'Response must be less than 2000 characters')
  .trim(),

experienceWithRope: z.string()
  .min(50, 'Please provide at least 50 characters describing your experience')
  .max(2000, 'Experience description must be less than 2000 characters')
  .trim(),

// AFTER:
whyJoin: z.string()
  .max(2000, 'Response must be less than 2000 characters')
  .trim()
  .refine(val => val.length > 0, 'This field is required'),

experienceWithRope: z.string()
  .max(2000, 'Experience description must be less than 2000 characters')
  .trim()
  .refine(val => val.length > 0, 'This field is required'),
```

**Removed from `fieldValidationMessages`:**
- `whyJoin.minLength`
- `experienceWithRope.minLength`

**2. Form Component (`/apps/web/src/features/vetting/components/VettingApplicationForm.tsx`)**

```typescript
// BEFORE:
whyJoin: (value) => {
  if (!value || value.trim().length < 20) {
    return fieldValidationMessages.whyJoin.minLength;
  }
  // ...
},

// AFTER:
whyJoin: (value) => {
  if (!value || value.trim().length === 0) {
    return fieldValidationMessages.whyJoin.required;
  }
  // ...
},
```

#### Backend Changes

**3. API Model (`/apps/api/Features/Vetting/Models/SimplifiedApplicationRequest.cs`)**

```csharp
// BEFORE:
[StringLength(2000, MinimumLength = 20)]
public string WhyJoin { get; set; } = string.Empty;

[StringLength(2000, MinimumLength = 50)]
public string ExperienceWithRope { get; set; } = string.Empty;

// AFTER:
[StringLength(2000)]
public string WhyJoin { get; set; } = string.Empty;

[StringLength(2000)]
public string ExperienceWithRope { get; set; } = string.Empty;
```

### Validation Rules After Changes

**WhyJoin Field:**
- ✅ Required (must have content after trimming)
- ✅ Maximum 2000 characters
- ❌ No minimum character limit

**ExperienceWithRope Field:**
- ✅ Required (must have content after trimming)
- ✅ Maximum 2000 characters
- ❌ No minimum character limit

## Task 2: Enhanced Error Logging for 400 Bad Request

### Problem
User experiencing `POST http://localhost:5655/api/vetting/applications/simplified 400 (Bad Request)` error with no visibility into the backend error details.

### Solution
Enhanced error logging in the API service to capture full error response details.

**File: `/apps/web/src/features/vetting/api/simplifiedVettingApi.ts`**

```typescript
// BEFORE:
console.error('simplifiedVettingApi.submitApplication: Error:', {
  status: error.response?.status,
  message: error.message,
  responseData: error.response?.data
});

// AFTER:
console.error('simplifiedVettingApi.submitApplication: FULL ERROR DETAILS:', {
  status: error.response?.status,
  message: error.message,
  responseData: error.response?.data,
  fullError: error.response?.data?.error || error.response?.data?.Error,
  details: error.response?.data?.details || error.response?.data?.Details,
  validationErrors: error.response?.data?.errors || error.response?.data?.Errors,
  title: error.response?.data?.title,
  type: error.response?.data?.type,
  allResponseData: JSON.stringify(error.response?.data, null, 2)
});
```

Also added request payload logging:
```typescript
console.log('simplifiedVettingApi.submitApplication: Submitting application:', {
  request,
  requestStringified: JSON.stringify(request, null, 2),
  url: '/api/vetting/applications/simplified'
});
```

### How to Debug 400 Errors Now

1. **Open browser DevTools Console**
2. **Submit the vetting application form**
3. **Look for console logs:**
   - **Before submission**: Request payload with full JSON
   - **On error**: Full error object with all backend validation details

4. **Error details will show:**
   - HTTP status code
   - Backend error message
   - Validation errors (if any)
   - Field-specific issues
   - Full response data as formatted JSON

### Expected Common 400 Causes

Based on the code, 400 errors typically occur when:
- Missing required field
- Field name mismatch (frontend sends `x` but backend expects `y`)
- Invalid data type (string vs number, etc.)
- Backend validation rule not matching frontend (e.g., minimum length constraints)
- Email format invalid
- Boolean field not properly set

## Testing Required

### Backend Testing
1. **Restart API container** to load new validation rules:
   ```bash
   docker-compose restart api
   # OR
   cd apps/api && dotnet run
   ```

2. **Verify OpenAPI schema updated** at `http://localhost:5655/swagger`

### Frontend Testing
1. **Test minimum validation removed:**
   - Enter 1 character in "Why Join" field - should be accepted
   - Enter 1 character in "Experience with Rope" field - should be accepted

2. **Test required validation still works:**
   - Leave fields empty - should show "This field is required" error
   - Submit button should be disabled when fields are empty

3. **Test maximum validation still works:**
   - Enter 2001 characters - should show max length error

4. **Test 400 error debugging:**
   - If 400 error occurs, check browser console for full error details
   - Should see both request payload and detailed error response

## Files Changed

### Frontend (React)
1. `/apps/web/src/features/vetting/schemas/simplifiedApplicationSchema.ts`
2. `/apps/web/src/features/vetting/components/VettingApplicationForm.tsx`
3. `/apps/web/src/features/vetting/api/simplifiedVettingApi.ts`

### Backend (API)
4. `/apps/api/Features/Vetting/Models/SimplifiedApplicationRequest.cs`

## Next Steps for User

1. **Test the form** - minimum character limits should be gone
2. **If 400 error occurs**, check browser console for detailed error information
3. **Report back** with the full error details from console logs
4. **We can then fix** the specific backend validation issue

## Notes

- Frontend validation now only checks for non-empty content (after trimming whitespace)
- Backend validation aligned with frontend (no minimum length constraints)
- Maximum length validation (2000 chars) retained on both fields
- Enhanced logging will help debug any future API validation issues quickly
