import { Button, Stack, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';

/**
 * Test page to verify Mantine Notifications are working correctly
 * This helps debug notification rendering issues
 */
export function TestNotifications() {
  const showTestNotification = () => {
    console.log('🔔 Test notification button clicked');
    notifications.show({
      title: 'Test Notification',
      message: 'This is a test notification to verify the system works',
      color: 'green',
      autoClose: 5000,
    });
    console.log('🔔 notifications.show() called successfully');
  };

  const showLongNotification = () => {
    console.log('🔔 Long notification button clicked');
    notifications.show({
      id: 'long-notification',
      title: 'Long Duration Test',
      message: 'This notification will stay for 30 seconds',
      color: 'blue',
      autoClose: 30000,
    });
    console.log('🔔 Long notification shown');
  };

  const showNoAutoCloseNotification = () => {
    console.log('🔔 No auto-close notification button clicked');
    notifications.show({
      id: 'manual-close',
      title: 'Manual Close Required',
      message: 'This notification will not auto-close',
      color: 'orange',
      autoClose: false,
    });
    console.log('🔔 No auto-close notification shown');
  };

  return (
    <div style={{ padding: '40px', maxWidth: '800px', margin: '0 auto' }}>
      <Title order={1} mb="xl">Notifications Test Page</Title>

      <Stack gap="md">
        <Button onClick={showTestNotification} color="green">
          Show Test Notification (5s)
        </Button>

        <Button onClick={showLongNotification} color="blue">
          Show Long Notification (30s)
        </Button>

        <Button onClick={showNoAutoCloseNotification} color="orange">
          Show Manual Close Notification
        </Button>
      </Stack>

      <div style={{ marginTop: '40px', padding: '20px', background: '#f0f0f0', borderRadius: '8px' }}>
        <h3>Instructions:</h3>
        <ol>
          <li>Click each button to test different notification configurations</li>
          <li>Check browser console for debug logs</li>
          <li>Look for notifications in top-right corner</li>
          <li>Inspect the DOM for #mantine-notifications-container</li>
        </ol>

        <h3>Expected Behavior:</h3>
        <ul>
          <li>Notifications should appear in top-right corner</li>
          <li>Green notification closes after 5 seconds</li>
          <li>Blue notification closes after 30 seconds</li>
          <li>Orange notification requires manual close (X button)</li>
        </ul>
      </div>
    </div>
  );
}
