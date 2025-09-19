// Simple test to verify BFF authentication implementation
// This can be run to manually test the cookie-based login flow

const axios = require('axios');

// Create axios instance with cookie jar support
const axiosWithCookies = axios.create({
  withCredentials: true,
  jar: true
});

async function testBFFLogin() {
  try {
    console.log('Testing BFF Authentication Flow (Cookie-based)...');

    // Test login with valid credentials
    const loginResponse = await axiosWithCookies.post('http://localhost:5655/api/auth/login', {
      email: 'admin@witchcityrope.com',
      password: 'Test123!'
    });

    console.log('Login Response Status:', loginResponse.status);
    console.log('Login Response Structure:', JSON.stringify(loginResponse.data, null, 2));

    if (loginResponse.data.success) {
      console.log('✅ Login successful - authentication cookie set');

      // Test authenticated API call with httpOnly cookie (no Authorization header)
      const userResponse = await axiosWithCookies.get('http://localhost:5655/api/auth/user');

      console.log('User API Response Status:', userResponse.status);
      console.log('User Data:', JSON.stringify(userResponse.data, null, 2));

      console.log('✅ BFF Authentication Flow Working!');

      // Test logout
      const logoutResponse = await axiosWithCookies.post('http://localhost:5655/api/auth/logout');
      console.log('Logout Response Status:', logoutResponse.status);
      console.log('✅ Logout successful - authentication cookie cleared');

    } else {
      console.log('❌ Login failed');
    }

  } catch (error) {
    console.error('❌ Test failed:', error.response?.data || error.message);
  }
}

// Only run if called directly
if (require.main === module) {
  testBFFLogin();
}

module.exports = testBFFLogin;