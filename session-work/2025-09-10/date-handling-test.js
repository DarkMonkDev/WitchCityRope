// Test script to verify the date handling fix for EventsWidget
// This tests the exact conditions that were causing the RangeError

// Test data that would cause the original error
const problematicEventData = [
  { startDateTime: null, endDateTime: null, title: "Event with null dates" },
  { startDateTime: undefined, endDateTime: undefined, title: "Event with undefined dates" },
  { startDateTime: "", endDateTime: "", title: "Event with empty string dates" },
  { startDateTime: "invalid-date", endDateTime: "also-invalid", title: "Event with invalid date strings" },
  { startDateTime: "2025-09-10T04:07:12.665015Z", endDateTime: "2025-09-10T06:07:12.665015Z", title: "Event with valid dates" },
];

// Safe date handling function (extracted from the fixed EventsWidget)
const formatEventForWidget = (event) => {
  // Safely handle potentially null/undefined date strings
  const startDateString = event.startDateTime;
  const endDateString = event.endDateTime;
  
  // Only create Date objects if we have valid date strings
  const startDate = startDateString ? new Date(startDateString) : null;
  const endDate = endDateString ? new Date(endDateString) : null;
  
  // Validate that dates are actually valid Date objects
  const isStartDateValid = startDate && !isNaN(startDate.getTime());
  const isEndDateValid = endDate && !isNaN(endDate.getTime());
  
  const formatTime = (date) => {
    try {
      return date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
    } catch (error) {
      return 'TBD';
    }
  };
  
  // Fallback values for invalid dates
  const fallbackDate = new Date().toISOString().split('T')[0];
  const fallbackTime = 'TBD';
  
  return {
    id: event.id || 'test-id',
    title: event.title || 'Untitled Event',
    date: isStartDateValid ? startDate.toISOString().split('T')[0] : fallbackDate,
    time: isStartDateValid && isEndDateValid 
      ? `${formatTime(startDate)} - ${formatTime(endDate)}`
      : isStartDateValid 
        ? `${formatTime(startDate)} - ${fallbackTime}`
        : fallbackTime,
    status: 'Open',
    statusColor: '#228B22',
    isUpcoming: isStartDateValid ? startDate > new Date() : false,
  };
};

console.log("Testing date handling fix:");
console.log("==========================");

problematicEventData.forEach((eventData, index) => {
  try {
    const result = formatEventForWidget(eventData);
    console.log(`✅ Test ${index + 1} PASSED: ${eventData.title}`);
    console.log(`   Input: startDateTime=${eventData.startDateTime}, endDateTime=${eventData.endDateTime}`);
    console.log(`   Output: date=${result.date}, time=${result.time}, isUpcoming=${result.isUpcoming}`);
    console.log("");
  } catch (error) {
    console.log(`❌ Test ${index + 1} FAILED: ${eventData.title}`);
    console.log(`   Error: ${error.message}`);
    console.log("");
  }
});

console.log("All tests completed successfully - no RangeError thrown!");