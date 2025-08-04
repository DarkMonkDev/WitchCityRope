// Simple standalone test to verify auth endpoint status codes
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class TestRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string Username { get; set; }
    public string LegalName { get; set; }
    public string SceneName { get; set; }
}

class Program
{
    static async Task Main()
    {
        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5000");
            
            Console.WriteLine("Testing Authentication Endpoints Status Codes...\n");

            // Test 1: Register with valid data
            Console.WriteLine("Test 1: Register with valid data");
            var registerRequest = new TestRequest
            {
                Email = $"test{Guid.NewGuid()}@example.com",
                Password = "StrongPassword123!",
                ConfirmPassword = "StrongPassword123!",
                Username = "testuser",
                LegalName = "Test User",
                SceneName = "TestScene"
            };
            
            var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
            Console.WriteLine($"Expected: 200 OK, Actual: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"✓ PASS: Register returns 200 OK\n");

            // Test 2: Login with valid credentials
            Console.WriteLine("Test 2: Login with valid credentials");
            var loginRequest = new { Email = registerRequest.Email, Password = registerRequest.Password };
            response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            Console.WriteLine($"Expected: 200 OK, Actual: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"✓ PASS: Login returns 200 OK\n");

            // Test 3: Register with duplicate email
            Console.WriteLine("Test 3: Register with duplicate email");
            response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
            Console.WriteLine($"Expected: 400 BadRequest, Actual: {(int)response.StatusCode} {response.StatusCode}");
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.WriteLine($"✓ PASS: Duplicate registration returns 400 BadRequest\n");
            }
            else
            {
                Console.WriteLine($"✗ FAIL: Expected 400 but got {(int)response.StatusCode}\n");
            }

            // Test 4: Login with invalid credentials
            Console.WriteLine("Test 4: Login with invalid credentials");
            var invalidLogin = new { Email = "nonexistent@example.com", Password = "WrongPassword123!" };
            response = await client.PostAsJsonAsync("/api/auth/login", invalidLogin);
            Console.WriteLine($"Expected: 401 Unauthorized, Actual: {(int)response.StatusCode} {response.StatusCode}");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine($"✓ PASS: Invalid login returns 401 Unauthorized\n");
            }
            else
            {
                Console.WriteLine($"✗ FAIL: Expected 401 but got {(int)response.StatusCode}\n");
            }

            Console.WriteLine("All authentication status code tests completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Make sure the API is running on http://localhost:5000");
        }
    }
}