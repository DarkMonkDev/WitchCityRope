// Simple test to verify JWT token implementation
// This can be run to manually test the login flow

const axios = require('axios');

async function testJWTLogin() {
  try {
    console.log('Testing JWT Login Flow...');
    
    // Test login with valid credentials
    const loginResponse = await axios.post('http://localhost:5655/api/Auth/login', {
      email: 'admin@witchcityrope.com',
      password: 'Test123!',
      rememberMe: false
    });
    
    console.log('Login Response Status:', loginResponse.status);
    console.log('Login Response Structure:', JSON.stringify(loginResponse.data, null, 2));
    
    if (loginResponse.data.success && loginResponse.data.data.token) {
      const token = loginResponse.data.data.token;
      console.log('JWT Token received:', token.substring(0, 50) + '...');
      
      // Test authenticated API call with JWT token
      const userResponse = await axios.get('http://localhost:5655/api/auth/user', {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      
      console.log('User API Response Status:', userResponse.status);
      console.log('User Data:', JSON.stringify(userResponse.data, null, 2));
      
      console.log('✅ JWT Token Flow Working!');
    } else {
      console.log('❌ No JWT token in login response');
    }
    
  } catch (error) {
    console.error('❌ Test failed:', error.response?.data || error.message);
  }
}

// Only run if called directly
if (require.main === module) {
  testJWTLogin();
}

module.exports = testJWTLogin;