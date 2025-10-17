using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Cms.Entities;

namespace WitchCityRope.Api.Features.Cms
{
    public static class CmsSeedData
    {
        public static async Task SeedInitialPagesAsync(ApplicationDbContext context)
        {
            // Check if any pages already exist
            if (await context.ContentPages.AnyAsync())
            {
                return; // Already seeded
            }

            // Get admin user for attribution (use first admin, or system user)
            var adminUser = await context.Users
                .FirstOrDefaultAsync(u => u.Role == "Administrator");

            if (adminUser == null)
            {
                // If no admin exists yet, create a system user placeholder
                adminUser = await context.Users.FirstAsync(); // Use first user as fallback
            }

            var now = DateTime.UtcNow;

            var initialPages = new List<ContentPage>
            {
                new ContentPage
                {
                    Slug = "resources",
                    Title = "Community Resources",
                    Content = @"<h1>Community Resources</h1>
<p>Welcome to the WitchCityRope resource center. Here you'll find important links and information for our rope bondage community.</p>

<h2>Safety Resources</h2>
<ul>
<li><strong>NCSF</strong> - National Coalition for Sexual Freedom</li>
<li><strong>The Rope Collective</strong> - Safety and education resources</li>
<li><strong>Rope365</strong> - Daily rope bondage inspiration and techniques</li>
</ul>

<h2>Educational Materials</h2>
<ul>
<li><strong>Books</strong>: ""The Seductive Art of Japanese Bondage"" by Midori</li>
<li><strong>Videos</strong>: Online tutorials and instructional content</li>
<li><strong>Workshops</strong>: Check our events calendar for upcoming classes</li>
</ul>

<h2>Local Resources</h2>
<ul>
<li><strong>Salem Safety Network</strong> - Community support and safety reporting</li>
<li><strong>LGBTQ+ Resources</strong> - Affirming care and community connections</li>
</ul>

<p><em>This page was last updated: {UpdatedAt}</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                new ContentPage
                {
                    Slug = "contact-us",
                    Title = "Contact Us",
                    Content = @"<h1>Contact Us</h1>
<p>Get in touch with WitchCityRope organizers and administrators.</p>

<h2>General Inquiries</h2>
<p><strong>Email</strong>: <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a></p>
<p><strong>Discord</strong>: Join our community server for real-time chat</p>

<h2>Event Questions</h2>
<p>For questions about specific events, workshops, or performances, please email our events team.</p>

<h2>Safety Concerns</h2>
<p>If you need to report a safety incident or have concerns about community safety, please use our <a href=""/safety"">Safety Incident Reporting System</a>.</p>

<h2>Vetting and Membership</h2>
<p>Questions about the vetting process? Contact our membership committee through the admin dashboard or email.</p>

<h2>Social Media</h2>
<ul>
<li><strong>FetLife</strong>: WitchCityRope group</li>
<li><strong>Instagram</strong>: @witchcityrope (public events and announcements)</li>
</ul>

<p><em>Response time: We typically respond within 24-48 hours.</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                new ContentPage
                {
                    Slug = "private-lessons",
                    Title = "Private Lessons & Instruction",
                    Content = @"<h1>Private Lessons & Rope Instruction</h1>
<p>Personalized rope bondage instruction with experienced WitchCityRope educators.</p>

<h2>What We Offer</h2>
<ul>
<li><strong>One-on-One Instruction</strong> - Personalized sessions tailored to your skill level and goals</li>
<li><strong>Small Group Classes</strong> - Learn with friends in a private setting (2-4 people)</li>
<li><strong>Partner Sessions</strong> - Develop rope skills with your partner</li>
<li><strong>Advanced Techniques</strong> - Suspension, predicaments, and complex ties</li>
</ul>

<h2>Pricing</h2>
<p>Private lessons are priced using our sliding scale model to ensure accessibility:</p>
<ul>
<li><strong>1-hour session</strong>: $50-$100 (sliding scale)</li>
<li><strong>2-hour session</strong>: $90-$180 (sliding scale)</li>
<li><strong>4-session package</strong>: $160-$320 (sliding scale, save 20%)</li>
</ul>

<h2>Our Instructors</h2>
<p>All WitchCityRope instructors are:</p>
<ul>
<li>Experienced rope practitioners with 5+ years of teaching experience</li>
<li>Trained in rope safety and risk awareness</li>
<li>Vetted members of the community</li>
<li>Committed to inclusive, body-positive instruction</li>
</ul>

<h2>How to Book</h2>
<p>To schedule a private lesson:</p>
<ol>
<li>Email us at <a href=""mailto:lessons@witchcityrope.com"">lessons@witchcityrope.com</a></li>
<li>Include your experience level and what you'd like to learn</li>
<li>We'll match you with an appropriate instructor and schedule a session</li>
</ol>

<p><strong>Cancellation Policy</strong>: 48-hour notice required for full refund. Same-day cancellations may be subject to a fee.</p>

<p><em>Note: Private lessons are available to vetted members only. If you're new to the community, please attend a few public workshops first.</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                }
            };

            await context.ContentPages.AddRangeAsync(initialPages);
            await context.SaveChangesAsync();
        }
    }
}
