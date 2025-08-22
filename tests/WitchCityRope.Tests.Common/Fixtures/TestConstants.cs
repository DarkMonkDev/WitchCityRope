namespace WitchCityRope.Tests.Common.Fixtures
{
    /// <summary>
    /// Common test constants
    /// </summary>
    public static class TestConstants
    {
        public static class Users
        {
            public const string ValidEncryptedLegalName = "encrypted_legal_name_123";
            public const string ValidSceneName = "TestScene";
            public const string ValidEmail = "test@example.com";
            public const int MinimumAge = 21;
        }

        public static class Events
        {
            public const string ValidTitle = "Test Event";
            public const string ValidDescription = "Test event description";
            public const string ValidLocation = "Test Location";
            public const int DefaultCapacity = 50;
            public const int SmallCapacity = 10;
            public const int LargeCapacity = 200;
        }

        public static class Money
        {
            public const string DefaultCurrency = "USD";
            public const decimal MinimumAmount = 0m;
            public const decimal StandardAmount = 25.00m;
            public const decimal LowTierAmount = 15.00m;
            public const decimal MidTierAmount = 25.00m;
            public const decimal HighTierAmount = 35.00m;
        }

        public static class Registration
        {
            public const string DefaultDietaryRestrictions = "Vegetarian";
            public const string DefaultAccessibilityNeeds = "Wheelchair accessible";
            public const string DefaultCancellationReason = "Unable to attend";
            public const int DefaultRefundWindowHours = 48;
        }
    }
}