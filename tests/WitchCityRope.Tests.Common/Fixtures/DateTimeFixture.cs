namespace WitchCityRope.Tests.Common.Fixtures
{
    /// <summary>
    /// Provides common date/time values for testing
    /// </summary>
    public static class DateTimeFixture
    {
        public static DateTime Now => DateTime.UtcNow;
        
        public static DateTime Today => DateTime.Today;
        
        public static DateTime Tomorrow => DateTime.UtcNow.AddDays(1);
        
        public static DateTime Yesterday => DateTime.UtcNow.AddDays(-1);
        
        public static DateTime NextWeek => DateTime.UtcNow.AddDays(7);
        
        public static DateTime LastWeek => DateTime.UtcNow.AddDays(-7);
        
        public static DateTime NextMonth => DateTime.UtcNow.AddMonths(1);
        
        public static DateTime LastMonth => DateTime.UtcNow.AddMonths(-1);
        
        public static DateTime ValidBirthDate => DateTime.Today.AddYears(-25);
        
        public static DateTime UnderAgeBirthDate => DateTime.Today.AddYears(-20);
        
        public static DateTime MinimumAgeBirthDate => DateTime.Today.AddYears(-21).AddDays(-1);
    }
}