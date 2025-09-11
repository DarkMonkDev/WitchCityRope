// Quick test to check what the admin login response contains

async function testAdminLogin() {
  try {
    const response = await fetch('http://localhost:5655/api/Auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        email: 'admin@witchcityrope.com',
        password: 'Test123!'
      })
    });

    if (!response.ok) {
      console.error('Login failed:', response.status);
      return;
    }

    const data = await response.json();
    console.log('Login Response:');
    console.log(JSON.stringify(data, null, 2));
    
    console.log('\nUser object properties:');
    if (data.user) {
      Object.keys(data.user).forEach(key => {
        console.log(`  - ${key}: ${JSON.stringify(data.user[key])}`);
      });
    }
    
    console.log('\nLooking for roles...');
    console.log('  user.roles:', data.user?.roles);
    console.log('  user.role:', data.user?.role);
    console.log('  user.userRole:', data.user?.userRole);
    console.log('  user.roleName:', data.user?.roleName);
    
  } catch (error) {
    console.error('Error:', error);
  }
}

testAdminLogin();