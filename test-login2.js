const loginData = {
  email: "admin@witchcityrope.com",
  password: "Test123!"
};

console.log("Testing login with:", loginData);
console.log("JSON string:", JSON.stringify(loginData));

// Test with properly escaped JSON
fetch('http://localhost:5655/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify(loginData)
})
.then(async response => {
  console.log('Status:', response.status);
  const text = await response.text();
  
  if (response.ok) {
    try {
      const data = JSON.parse(text);
      console.log('Success! Response:', data);
    } catch (e) {
      console.log('Success but response is not JSON:', text);
    }
  } else {
    console.log('Error response:', text);
  }
})
.catch(error => {
  console.error('Network error:', error);
});