using System;
using Microsoft.AspNetCore.Identity;

// Simple script to generate ASP.NET Core Identity password hash
var passwordHasher = new PasswordHasher<object>();
var password = "Test123!";
var hash = passwordHasher.HashPassword(null, password);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine();
Console.WriteLine("SQL to update admin user:");
Console.WriteLine($@"UPDATE auth.""Users"" SET ""PasswordHash"" = '{hash}' WHERE ""Email"" = 'admin@witchcityrope.com';");