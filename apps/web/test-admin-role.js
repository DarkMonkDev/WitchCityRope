// Test that admin user now has roles in login response

async function testAdminRole() {
  try {
    // Login as admin
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
    
    console.log('✅ Login successful!');
    console.log('\n📋 User object:');
    console.log(JSON.stringify(data.user, null, 2));
    
    console.log('\n🔑 Role information:');
    console.log('  user.role:', data.user?.role);
    console.log('  user.roles:', data.user?.roles);
    
    // Check if Administrator role is present
    if (data.user?.roles?.includes('Administrator')) {
      console.log('\n✅ SUCCESS: Administrator role is properly returned!');
      console.log('   Admin menu should now be visible in the UI');
    } else {
      console.log('\n❌ ISSUE: Administrator role not found in response');
      console.log('   Admin menu will NOT be visible');
    }
    
  } catch (error) {
    console.error('Error:', error);
  }
}

testAdminRole();