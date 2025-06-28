using FluentAssertions;
using FluentAssertions.Execution;

namespace WitchCityRope.Tests.Common.Extensions
{
    public static class TestExtensions
    {
        /// <summary>
        /// Performs multiple assertions in a single scope
        /// </summary>
        public static void ShouldSatisfy<T>(this T subject, params Action<T>[] assertions)
        {
            using (new AssertionScope())
            {
                foreach (var assertion in assertions)
                {
                    assertion(subject);
                }
            }
        }

        /// <summary>
        /// Gets a future date based on days from now
        /// </summary>
        public static DateTime DaysFromNow(this int days)
        {
            return DateTime.UtcNow.AddDays(days);
        }

        /// <summary>
        /// Gets a past date based on days ago
        /// </summary>
        public static DateTime DaysAgo(this int days)
        {
            return DateTime.UtcNow.AddDays(-days);
        }

        /// <summary>
        /// Gets a past date based on years ago
        /// </summary>
        public static DateTime YearsAgo(this int years)
        {
            return DateTime.Today.AddYears(-years);
        }
    }
}