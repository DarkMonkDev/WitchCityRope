using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Cms.Entities;

namespace WitchCityRope.Api.Features.Cms
{
    public static class CmsSeedData
    {
        public static async Task SeedInitialPagesAsync(ApplicationDbContext context)
        {
            // Get admin user for attribution (use first admin, or system user)
            var adminUser = await context.Users
                // Case-insensitive role match for seeding robustness
                .FirstOrDefaultAsync(u => u.Role != null && u.Role.ToLower() == "administrator");

            if (adminUser == null)
            {
                // If no admin exists yet, create a system user placeholder
                var firstUser = await context.Users.FirstOrDefaultAsync();
                if (firstUser == null)
                {
                    return; // No users yet, can't seed pages
                }
                adminUser = firstUser;
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
                },

                // NEW: About Us Page
                new ContentPage
                {
                    Slug = "about-us",
                    Title = "About WitchCityRope",
                    Content = @"<h1>About WitchCityRope</h1>
<p><strong>Last Updated:</strong> October 23, 2025</p>

<h2>Our Mission</h2>
<p>WitchCityRope is Salem, Massachusetts' premier rope bondage community, dedicated to creating a safe, inclusive, and educational space for exploring the art of rope bondage. We believe in the power of consent, communication, and continuous learning.</p>

<h2>Who We Are</h2>
<p>WitchCityRope emerged from a passionate group of rope enthusiasts who wanted to create a welcoming community in the Greater Salem area. We are:</p>
<ul>
<li><strong>Educators</strong> - Offering workshops and classes for all skill levels</li>
<li><strong>Artists</strong> - Celebrating rope as an art form and means of expression</li>
<li><strong>Community Builders</strong> - Fostering connections among rope bondage practitioners</li>
<li><strong>Safety Advocates</strong> - Prioritizing risk-aware consensual practices</li>
</ul>

<h2>Our Values</h2>

<h3>Consent First</h3>
<p>Everything we do is built on a foundation of enthusiastic, informed consent. We believe that consent is:</p>
<ul>
<li><strong>Freely given</strong> - No coercion or pressure</li>
<li><strong>Specific</strong> - Clear about what activities are consented to</li>
<li><strong>Reversible</strong> - Can be withdrawn at any time</li>
<li><strong>Informed</strong> - All parties understand risks and boundaries</li>
<li><strong>Enthusiastic</strong> - A clear ""yes,"" not just absence of ""no""</li>
</ul>

<h3>Safety and Education</h3>
<p>We are committed to:</p>
<ul>
<li>Comprehensive safety training for all members</li>
<li>Continuous education on techniques and risk awareness</li>
<li>Maintaining safe spaces for practice and exploration</li>
<li>Supporting newcomers with mentorship and guidance</li>
</ul>

<h3>Inclusivity and Respect</h3>
<p>WitchCityRope welcomes people of all:</p>
<ul>
<li>Gender identities and expressions</li>
<li>Sexual orientations</li>
<li>Racial and ethnic backgrounds</li>
<li>Body types and abilities</li>
<li>Experience levels</li>
</ul>
<p>We actively work to create an anti-discriminatory space where everyone can explore rope bondage authentically.</p>

<h3>Community and Connection</h3>
<p>We believe rope bondage is as much about human connection as it is about technique. Our community:</p>
<ul>
<li>Supports newcomers and experienced practitioners alike</li>
<li>Celebrates diversity in rope styles and practices</li>
<li>Creates spaces for both social connection and skill development</li>
<li>Maintains confidentiality and discretion</li>
</ul>

<h2>What We Offer</h2>

<h3>Workshops and Classes</h3>
<ul>
<li><strong>Beginner Classes</strong> - Introduction to rope bondage fundamentals</li>
<li><strong>Intermediate Workshops</strong> - Skill development and technique refinement</li>
<li><strong>Advanced Sessions</strong> - Suspension, predicaments, and complex ties</li>
<li><strong>Specialty Topics</strong> - Safety, communication, rope photography, and more</li>
</ul>

<h3>Social Events</h3>
<ul>
<li><strong>Rope Jams</strong> - Casual practice sessions with community support</li>
<li><strong>Social Meetups</strong> - Connect with fellow rope enthusiasts</li>
<li><strong>Demonstrations</strong> - Watch experienced practitioners showcase their art</li>
<li><strong>Community Gatherings</strong> - Celebrate our community and culture</li>
</ul>

<h3>Private Instruction</h3>
<p>One-on-one and small group lessons with experienced instructors for personalized learning.</p>

<h2>Our Community Standards</h2>
<p>Members of WitchCityRope are expected to:</p>
<ul>
<li>Prioritize consent, communication, and safety</li>
<li>Treat all community members with respect and dignity</li>
<li>Maintain confidentiality about other members' identities</li>
<li>Follow our Community Guidelines and Code of Conduct</li>
<li>Support a culture of continuous learning and improvement</li>
</ul>

<h2>Vetting Process</h2>
<p>To maintain a safe community, we require all participants to complete our vetting process before attending member-only events. This process includes:</p>
<ol>
<li>Application submission with references</li>
<li>Attendance at orientation events</li>
<li>Interview with vetting committee</li>
<li>Background check where appropriate</li>
</ol>
<p>Vetting helps ensure all members share our values and commitment to safety.</p>

<h2>Leadership and Organization</h2>
<p>WitchCityRope is organized by a dedicated team of volunteers including:</p>
<ul>
<li><strong>Event Coordinators</strong> - Plan and execute workshops and social events</li>
<li><strong>Safety Team</strong> - Maintain safety standards and protocols</li>
<li><strong>Vetting Committee</strong> - Review applications and conduct interviews</li>
<li><strong>Instructors</strong> - Share knowledge and skills with the community</li>
</ul>

<h2>Connect With Us</h2>
<p>We're active on several platforms:</p>
<ul>
<li><strong>Events Calendar</strong> - Check our website for upcoming workshops and social events</li>
<li><strong>Discord Server</strong> - Connect with members and join discussions</li>
<li><strong>FetLife Group</strong> - Find us on the WitchCityRope group</li>
<li><strong>Email</strong> - <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a> for general inquiries</li>
</ul>

<h2>Location</h2>
<p>We host events at various venues throughout the Greater Salem area. Specific locations are shared with registered participants before each event.</p>

<h2>Join Us</h2>
<p>Whether you're completely new to rope bondage or an experienced practitioner, WitchCityRope offers a welcoming space to learn, practice, and connect. Visit our events calendar to find your first class or social event, or reach out to learn more about our community.</p>

<hr>
<p><strong>WitchCityRope</strong> - <em>Building Community Through Rope</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: FAQ Page
                new ContentPage
                {
                    Slug = "faq",
                    Title = "Frequently Asked Questions",
                    Content = @"<h1>Frequently Asked Questions</h1>
<p><strong>Last Updated:</strong> October 23, 2025</p>

<h2>General Questions</h2>

<h3>What is WitchCityRope?</h3>
<p>WitchCityRope is Salem's rope bondage community, offering workshops, social events, and educational resources for rope enthusiasts of all skill levels. We're dedicated to creating a safe, inclusive space for exploring rope bondage as an art form and means of connection.</p>

<h3>Do I need experience to join?</h3>
<p>No! We welcome complete beginners. We offer beginner-friendly workshops designed to teach fundamentals safely. Our community includes everyone from first-timers to experienced practitioners.</p>

<h3>What is the minimum age to participate?</h3>
<p>All participants must be at least 21 years old. We strictly verify age during registration and vetting.</p>

<h3>Is WitchCityRope LGBTQ+ friendly?</h3>
<p>Absolutely. We celebrate and welcome people of all gender identities, sexual orientations, and relationship structures. Inclusivity is one of our core values.</p>

<h2>Membership and Vetting</h2>

<h3>What is the vetting process?</h3>
<p>Vetting is our way of ensuring all members share our values of consent, safety, and respect. The process includes:</p>
<ol>
<li>Submitting an application with basic information and references</li>
<li>Attending one or more public orientation events</li>
<li>Interview with our vetting committee</li>
<li>Background check (for certain membership levels)</li>
</ol>
<p>The entire process typically takes 2-4 weeks.</p>

<h3>Why is vetting required?</h3>
<p>Vetting helps maintain a safe, trusted community. It allows us to ensure members understand consent, safety practices, and community standards before attending member-only events.</p>

<h3>Can I attend events before being vetted?</h3>
<p>Yes! Many of our workshops and some social events are open to non-vetted members. Check event listings for access requirements.</p>

<h3>What if my vetting application is denied?</h3>
<p>Denials are rare and always explained. You may reapply after 6 months. If you believe there was an error, you can appeal the decision.</p>

<h3>Is there a membership fee?</h3>
<p>We operate on a sliding scale donation model to ensure accessibility. Suggested annual support is $50-$200, but we never turn away anyone for inability to pay.</p>

<h2>Events and Classes</h2>

<h3>What types of events do you offer?</h3>
<ul>
<li><strong>Workshops</strong> - Structured classes teaching specific techniques</li>
<li><strong>Rope Jams</strong> - Casual practice sessions</li>
<li><strong>Social Events</strong> - Community gatherings and discussions</li>
<li><strong>Demonstrations</strong> - Performance and showcasing</li>
<li><strong>Private Lessons</strong> - One-on-one instruction</li>
</ul>

<h3>How do I register for an event?</h3>
<p>Browse our events calendar, create an account, and register through the event page. Some events require vetting while others are open to all.</p>

<h3>What should I bring to my first workshop?</h3>
<ul>
<li>Comfortable clothing you can move in</li>
<li>Your own rope (we can recommend beginner-friendly options)</li>
<li>Water bottle</li>
<li>Notebook if you like to take notes</li>
<li>An open mind and willingness to learn!</li>
</ul>
<p>Many workshops also offer rope for purchase or loan.</p>

<h3>Can I bring a partner?</h3>
<p>Absolutely! Many people attend with partners. You can also attend solo - many workshops include partner rotation or practice with other solo attendees.</p>

<h3>What if I don't have rope?</h3>
<p>We often have rope available for purchase or temporary loan at workshops. Contact the event organizer beforehand to confirm.</p>

<h3>Are workshops hands-on?</h3>
<p>Yes! Most workshops include demonstration followed by hands-on practice with instructor guidance. You learn by doing in a supported environment.</p>

<h2>Safety</h2>

<h3>How do you ensure safety at events?</h3>
<ul>
<li>Mandatory safety briefings</li>
<li>Trained safety monitors at all events</li>
<li>Safety shears readily available</li>
<li>Clear protocols for emergencies</li>
<li>Consent and communication requirements</li>
<li>Experienced instructors and mentors</li>
</ul>

<h3>What if something goes wrong during rope play?</h3>
<p>Use your safe word immediately. Safety monitors are always present and can assist. We take all safety concerns seriously and investigate thoroughly.</p>

<h3>Do I need to disclose health conditions?</h3>
<p>You're encouraged to share relevant health information with your rope partners and event staff. This helps everyone make informed decisions about risk. All health information is kept confidential.</p>

<h3>What safety equipment is required?</h3>
<ul>
<li><strong>Safety shears</strong> - Always accessible during rope work</li>
<li><strong>First aid kit</strong> - Available at all events</li>
<li><strong>Communication tools</strong> - Safe words/signals established before play</li>
</ul>

<h2>Costs and Payment</h2>

<h3>How much do events cost?</h3>
<p>Costs vary by event:</p>
<ul>
<li>Workshops: $20-$50 (sliding scale)</li>
<li>Social events: $10-$30 or free</li>
<li>Private lessons: $50-$100 per hour (sliding scale)</li>
</ul>
<p>We use sliding scale pricing to ensure accessibility.</p>

<h3>What payment methods do you accept?</h3>
<p>We accept PayPal, major credit cards, and cash (at in-person events). All online payments are processed securely.</p>

<h3>What is your refund policy?</h3>
<ul>
<li>7+ days before event: Full refund minus processing fee</li>
<li>3-6 days before: 50% refund</li>
<li>Less than 3 days: No refund</li>
<li>Medical emergencies may qualify for exceptions</li>
</ul>

<h3>Can I get financial assistance?</h3>
<p>We offer scholarships and work-trade opportunities. Contact us at <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a> to discuss options.</p>

<h2>Community and Culture</h2>

<h3>What is the culture like?</h3>
<p>Our community values:</p>
<ul>
<li>Consent and communication</li>
<li>Continuous learning</li>
<li>Mutual respect and support</li>
<li>Inclusivity and diversity</li>
<li>Authenticity and self-expression</li>
</ul>
<p>We're friendly, welcoming, and supportive of newcomers.</p>

<h3>Can I take photos at events?</h3>
<p><strong>No photography without explicit consent from everyone visible in the photo.</strong> Some events have designated photo areas. Always ask first and respect ""no photo"" indicators.</p>

<h3>What about privacy and discretion?</h3>
<p>We take privacy seriously. Member identities and attendance are confidential. Many people use scene names. What happens at events stays at events.</p>

<h3>How can I get more involved?</h3>
<p>We're always looking for volunteers! Opportunities include:</p>
<ul>
<li>Event setup and breakdown</li>
<li>Safety monitoring</li>
<li>Mentoring newcomers</li>
<li>Teaching workshops</li>
<li>Organizing events</li>
</ul>
<p>Email <a href=""mailto:volunteer@witchcityrope.com"">volunteer@witchcityrope.com</a> to learn more.</p>

<h2>Technical Questions</h2>

<h3>What rope should I buy as a beginner?</h3>
<p>We recommend:</p>
<ul>
<li><strong>Material</strong>: Jute or hemp (natural fibers)</li>
<li><strong>Length</strong>: 2-3 pieces of 30-foot (10-meter) rope</li>
<li><strong>Diameter</strong>: 6mm for most people</li>
</ul>
<p>Ask instructors for specific recommendations based on your goals.</p>

<h3>How do I learn rope bondage safely?</h3>
<ol>
<li>Start with beginner workshops</li>
<li>Learn foundational safety principles</li>
<li>Practice with experienced partners</li>
<li>Progress gradually to more advanced techniques</li>
<li>Never attempt suspension without proper training</li>
<li>Always prioritize communication and consent</li>
</ol>

<h3>Are there different styles of rope bondage?</h3>
<p>Yes! Common styles include:</p>
<ul>
<li><strong>Shibari/Kinbaku</strong> - Japanese rope bondage emphasizing aesthetics and connection</li>
<li><strong>Western/Utility</strong> - Functional ties with focus on restraint</li>
<li><strong>Decorative</strong> - Rope as artistic adornment</li>
<li><strong>Suspension</strong> - Advanced technique involving rope support off the ground</li>
</ul>
<p>Our community welcomes all styles.</p>

<h2>Getting Help</h2>

<h3>Who do I contact for...</h3>
<ul>
<li><strong>Event questions</strong>: <a href=""mailto:events@witchcityrope.com"">events@witchcityrope.com</a></li>
<li><strong>Vetting questions</strong>: <a href=""mailto:vetting@witchcityrope.com"">vetting@witchcityrope.com</a></li>
<li><strong>Safety concerns</strong>: <a href=""mailto:safety@witchcityrope.com"">safety@witchcityrope.com</a></li>
<li><strong>General inquiries</strong>: <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a></li>
<li><strong>Technical support</strong>: <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a></li>
</ul>

<h3>How quickly will I get a response?</h3>
<p>We typically respond within 24-48 hours for non-urgent inquiries. Safety concerns are addressed immediately.</p>

<h3>I have a question not listed here</h3>
<p>Email <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a> or ask during any event. We're happy to answer questions!</p>

<hr>
<p><strong>Still have questions?</strong> Visit our Contact page or join us at a public event to learn more about our community.</p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: Safety Practices Page (continues beyond character limit, split for readability)
                new ContentPage
                {
                    Slug = "safety-practices",
                    Title = "Safety Practices",
                    Content = @"<h1>Safety Practices</h1>
<p><strong>Last Updated:</strong> October 23, 2025</p>

<h2>Introduction</h2>
<p>Safety is the foundation of all rope bondage practice at WitchCityRope. This document outlines essential safety practices, risk awareness, and emergency procedures. All members are expected to understand and follow these guidelines.</p>
<blockquote><strong>Remember</strong>: Rope bondage involves inherent risks. Risk-aware, consensual practice is our standard.</blockquote>

<h2>Core Safety Principles</h2>

<h3>1. Informed Consent</h3>
<ul>
<li>Discuss all planned activities beforehand</li>
<li>Establish safe words and non-verbal signals</li>
<li>Check in regularly during rope work</li>
<li>Respect limits and boundaries absolutely</li>
<li>Consent can be withdrawn at any time</li>
</ul>

<h3>2. Risk Awareness (RACK)</h3>
<p><strong>Risk-Aware Consensual Kink</strong> means:</p>
<ul>
<li>Understanding the risks involved</li>
<li>Taking steps to minimize those risks</li>
<li>Accepting responsibility for outcomes</li>
<li>Making informed decisions together</li>
</ul>

<h3>3. Communication</h3>
<ul>
<li>Establish clear communication before, during, and after rope work</li>
<li>Use safe words consistently</li>
<li>Report any discomfort immediately</li>
<li>Discuss aftercare needs</li>
<li>Debrief after sessions</li>
</ul>

<h2>Pre-Session Safety</h2>

<h3>Before Any Rope Work</h3>
<p><strong>Negotiation Checklist:</strong></p>
<ul>
<li>Establish safe words/signals (verbal and non-verbal)</li>
<li>Discuss boundaries and limits</li>
<li>Share relevant health information</li>
<li>Confirm experience levels</li>
<li>Agree on planned activities</li>
<li>Establish check-in frequency</li>
<li>Discuss aftercare needs</li>
<li>Verify safety equipment available</li>
</ul>

<p><strong>Health Considerations:</strong></p>
<ul>
<li>Disclose injuries, joint issues, or nerve damage</li>
<li>Mention recent surgeries or medical procedures</li>
<li>Share mobility limitations</li>
<li>Discuss pregnancy if applicable</li>
<li>Note medications that affect circulation or sensation</li>
<li>Confirm both parties are sober and clear-minded</li>
</ul>

<p><strong>Equipment Check:</strong></p>
<ul>
<li>Safety shears accessible</li>
<li>Rope in good condition (no fraying, dry rot, or damage)</li>
<li>Appropriate rope length and diameter for planned ties</li>
<li>First aid kit nearby</li>
<li>Phone accessible for emergencies</li>
</ul>

<h2>During Rope Work</h2>

<h3>Continuous Monitoring</h3>
<p><strong>Top/Rigger Responsibilities:</strong></p>
<ul>
<li>Monitor bottom's breathing, color, and verbal responses</li>
<li>Check circulation frequently (hands/feet color, temperature, pulse)</li>
<li>Watch for numbness or tingling (nerve compression)</li>
<li>Maintain awareness of weight distribution in ties</li>
<li>Stay alert and focused - no distractions</li>
<li>Never leave a tied person unattended</li>
</ul>

<p><strong>Bottom/Model Responsibilities:</strong></p>
<ul>
<li>Communicate honestly about sensation and comfort</li>
<li>Use safe words when needed</li>
<li>Report numbness, tingling, or pain immediately</li>
<li>Don't ""tough it out"" - your safety is paramount</li>
<li>Stay aware of your body's signals</li>
</ul>

<h3>Physical Safety Checks</h3>
<p><strong>Circulation Checks (Every 5-10 minutes):</strong></p>
<ul>
<li><strong>Color</strong>: Hands/feet should be normal skin tone (not pale, purple, or dark)</li>
<li><strong>Temperature</strong>: Warm to touch (not cold or hot)</li>
<li><strong>Capillary Refill</strong>: Press fingernail - color should return within 2 seconds</li>
<li><strong>Pulse</strong>: Palpable pulse points (wrist, ankle)</li>
<li><strong>Sensation</strong>: Can feel light touch and move fingers/toes</li>
</ul>

<p><strong>Nerve Checks:</strong></p>
<ul>
<li>Ask about numbness or tingling</li>
<li>Check grip strength</li>
<li>Test ability to spread fingers/wiggle toes</li>
<li>Pay special attention to nerve danger zones: Upper arms (radial nerve), Armpits (brachial plexus), Behind knees, Ankles</li>
</ul>

<p><strong>Warning Signs - STOP IMMEDIATELY:</strong></p>
<ul>
<li>Numbness or tingling</li>
<li>Loss of sensation</li>
<li>Color change (pale, blue, purple)</li>
<li>Cold extremities</li>
<li>Loss of grip strength</li>
<li>Difficulty breathing</li>
<li>Dizziness or disorientation</li>
<li>Panic or extreme distress</li>
<li>Use of safe word</li>
</ul>

<h2>Emergency Procedures</h2>

<h3>Immediate Response</h3>
<p><strong>If Someone Says Their Safe Word:</strong></p>
<ol>
<li>Stop all activity immediately</li>
<li>Ask what they need (release? stay? slow down?)</li>
<li>Remove rope if requested</li>
<li>Check for injury</li>
<li>Provide reassurance and support</li>
<li>Debrief when appropriate</li>
</ol>

<p><strong>If Circulation is Compromised:</strong></p>
<ol>
<li>Remove rope from affected area immediately</li>
<li>Assess sensation and color</li>
<li>Keep affected limb elevated</li>
<li>Monitor for return of circulation</li>
<li>Seek medical attention if no improvement in 5 minutes</li>
</ol>

<p><strong>If Someone Loses Consciousness:</strong></p>
<ol>
<li>Remove rope immediately</li>
<li>Lay person flat (recovery position)</li>
<li>Check breathing and pulse</li>
<li>Call 911 if no improvement in 30 seconds</li>
<li>Perform CPR if needed and trained</li>
<li>Monitor vital signs</li>
</ol>

<h3>Medical Emergencies</h3>
<p><strong>Call 911 if:</strong></p>
<ul>
<li>Loss of consciousness lasting more than a few seconds</li>
<li>Difficulty breathing</li>
<li>Chest pain</li>
<li>Severe numbness persisting after rope removal</li>
<li>Any serious injury</li>
<li>Suspicion of suspension trauma</li>
</ul>

<h2>Safety Equipment</h2>
<h3>Required at All Times</h3>
<ul>
<li><strong>Safety Shears</strong> - EMT scissors designed to cut rope quickly
  <ul>
    <li>Test before use</li>
    <li>Keep accessible (within arm's reach)</li>
    <li>Know exactly where they are</li>
    <li>Multiple pairs for suspension</li>
  </ul>
</li>
</ul>

<h3>Recommended</h3>
<ul>
<li>First aid kit</li>
<li>Phone for emergencies</li>
<li>Blankets (for warmth/shock)</li>
<li>Water</li>
<li>Pillow/cushions for comfort</li>
</ul>

<h2>Aftercare</h2>

<h3>Physical Aftercare</h3>
<ul>
<li>Check for rope marks, bruising, or injury</li>
<li>Apply lotion to rope mark areas</li>
<li>Stretch gently</li>
<li>Rest if needed</li>
<li>Monitor for delayed symptoms (numbness, pain)</li>
</ul>

<h3>Emotional Aftercare</h3>
<ul>
<li>Debrief the session</li>
<li>Provide comfort and reassurance</li>
<li>Discuss what worked and what didn't</li>
<li>Allow time for emotional processing</li>
<li>Respect that aftercare needs vary</li>
</ul>

<h2>Contact for Safety Concerns</h2>
<p><strong>Immediate Emergencies</strong>: Call 911</p>
<p><strong>Event Safety Issues</strong>: Speak to event safety monitor immediately</p>
<p><strong>Non-Emergency Safety Concerns</strong>: <a href=""mailto:safety@witchcityrope.com"">safety@witchcityrope.com</a></p>

<hr>
<h2>Acknowledgment</h2>
<p>By participating in rope bondage activities at WitchCityRope events, you acknowledge that you have read, understood, and agree to follow these safety practices.</p>
<p><strong>Remember: Safety is everyone's responsibility. When in doubt, stop and reassess.</strong></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: Code of Conduct Page
                new ContentPage
                {
                    Slug = "code-of-conduct",
                    Title = "Code of Conduct",
                    Content = @"<h1>Code of Conduct</h1>
<p><strong>Effective Date:</strong> October 23, 2025<br>
<strong>Last Updated:</strong> October 23, 2025</p>

<h2>Our Commitment</h2>
<p>WitchCityRope is dedicated to providing a safe, inclusive, and harassment-free experience for everyone, regardless of age, body size, visible or invisible disability, ethnicity, sex characteristics, gender identity and expression, level of experience, education, socio-economic status, nationality, personal appearance, race, religion, or sexual identity and orientation.</p>

<h2>Our Standards</h2>

<h3>Expected Behavior</h3>
<p>We expect all community members to:</p>

<p><strong>✅ Practice Enthusiastic Consent</strong></p>
<ul>
<li>Obtain clear, verbal consent for all rope work and physical contact</li>
<li>Respect all stated boundaries and limits</li>
<li>Accept ""no"" gracefully and without question</li>
</ul>

<p><strong>✅ Communicate Openly and Honestly</strong></p>
<ul>
<li>Share relevant safety information with rope partners</li>
<li>Use safe words and check-ins consistently</li>
<li>Address concerns directly and respectfully</li>
</ul>

<p><strong>✅ Prioritize Safety</strong></p>
<ul>
<li>Follow all safety protocols and guidelines</li>
<li>Attend mandatory safety briefings at events</li>
<li>Report accidents or safety concerns immediately</li>
<li>Never engage in rope work while intoxicated</li>
</ul>

<p><strong>✅ Respect Privacy and Confidentiality</strong></p>
<ul>
<li>Maintain confidentiality about other members' identities</li>
<li>No photography or recording without explicit consent from all visible parties</li>
<li>Respect ""scene names"" and pseudonyms</li>
<li>What happens at events stays at events</li>
</ul>

<p><strong>✅ Foster Inclusivity</strong></p>
<ul>
<li>Welcome people of all backgrounds and identities</li>
<li>Use preferred names and pronouns</li>
<li>Challenge discrimination when witnessed</li>
<li>Support newcomers and less experienced members</li>
</ul>

<p><strong>✅ Take Responsibility</strong></p>
<ul>
<li>Own your actions and their consequences</li>
<li>Learn from mistakes and do better</li>
<li>Seek help when needed</li>
<li>Support community accountability</li>
</ul>

<h3>Unacceptable Behavior</h3>
<p>The following behaviors will not be tolerated:</p>

<p><strong>❌ Consent Violations</strong></p>
<ul>
<li>Any non-consensual touch or activity</li>
<li>Ignoring safe words or withdrawal of consent</li>
<li>Pressuring, coercing, or manipulating others</li>
<li>Engaging in rope work while intoxicated</li>
</ul>

<p><strong>❌ Harassment and Discrimination</strong></p>
<ul>
<li>Sexual harassment or unwanted sexual advances</li>
<li>Bullying, intimidation, or threatening behavior</li>
<li>Discriminatory language or actions based on identity</li>
<li>Stalking or persistent unwanted contact</li>
<li>Deadnaming or misgendering</li>
</ul>

<p><strong>❌ Safety Violations</strong></p>
<ul>
<li>Reckless behavior endangering others</li>
<li>Refusing to follow safety protocols</li>
<li>Attempting techniques beyond your skill level without supervision</li>
<li>Bringing prohibited items to events</li>
</ul>

<p><strong>❌ Privacy Violations</strong></p>
<ul>
<li>Photography or recording without consent</li>
<li>Sharing others' personal information without permission</li>
<li>""Outing"" community members in vanilla spaces</li>
<li>Posting about others on social media without permission</li>
</ul>

<h2>Enforcement</h2>

<h3>Reporting</h3>
<p>If you experience or witness unacceptable behavior:</p>
<ol>
<li><strong>Immediate Safety Concerns</strong>: Alert event staff or safety monitors immediately</li>
<li><strong>Non-Urgent Issues</strong>: Email <a href=""mailto:reports@witchcityrope.com"">reports@witchcityrope.com</a></li>
<li><strong>Anonymous Reports</strong>: Use our anonymous reporting form on the website</li>
</ol>
<p>All reports are taken seriously and investigated promptly. We maintain confidentiality to the greatest extent possible.</p>

<h3>Consequences</h3>
<p>Violations of this Code of Conduct may result in:</p>
<ul>
<li><strong>Warning</strong>: For minor first offenses</li>
<li><strong>Temporary Suspension</strong>: For serious violations or repeated offenses</li>
<li><strong>Permanent Ban</strong>: For severe violations, safety threats, or persistent misconduct</li>
<li><strong>Legal Action</strong>: When appropriate for criminal behavior</li>
</ul>
<p>The severity and history of violations will be considered. Our priority is community safety.</p>

<h3>Appeals Process</h3>
<p>If you believe a moderation decision was made in error, you may submit a written appeal to <a href=""mailto:appeals@witchcityrope.com"">appeals@witchcityrope.com</a> within 14 days. Appeals will be reviewed by members of leadership who were not involved in the original decision.</p>

<h2>Scope</h2>
<p>This Code of Conduct applies to:</p>
<ul>
<li>All WitchCityRope events (workshops, social events, demonstrations)</li>
<li>Online spaces (website, Discord, social media)</li>
<li>Private messages between community members related to community activities</li>
<li>Conduct at community-related gatherings</li>
<li>Behavior that affects community safety, even outside official spaces</li>
</ul>

<h2>Acknowledgment</h2>
<p>By participating in WitchCityRope events and spaces, you agree to follow this Code of Conduct. We all share responsibility for maintaining a safe, inclusive, and respectful community.</p>

<h2>Contact Information</h2>
<ul>
<li><strong>Safety Emergencies</strong>: Speak to event staff immediately or call 911</li>
<li><strong>Report Violations</strong>: <a href=""mailto:reports@witchcityrope.com"">reports@witchcityrope.com</a></li>
<li><strong>General Questions</strong>: <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a></li>
<li><strong>Appeals</strong>: <a href=""mailto:appeals@witchcityrope.com"">appeals@witchcityrope.com</a></li>
</ul>

<hr>
<p><strong>Thank you for helping us maintain a community where everyone can explore rope bondage safely and authentically.</strong></p>
<p><em>This Code of Conduct is inspired by the Contributor Covenant and adapted for the specific needs of a rope bondage community.</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: Terms of Service Page
                new ContentPage
                {
                    Slug = "terms-of-service",
                    Title = "Terms of Service",
                    Content = @"<h1>Terms of Service</h1>
<p><strong>Effective Date:</strong> October 23, 2025<br>
<strong>Last Updated:</strong> October 23, 2025</p>

<h2>1. Acceptance of Terms</h2>
<p>By accessing or using WitchCityRope (""the Service""), you agree to be bound by these Terms of Service (""Terms""). If you do not agree to these Terms, do not use the Service.</p>
<p>These Terms constitute a legally binding agreement between you and WitchCityRope regarding your use of the Service.</p>

<h2>2. Eligibility Requirements</h2>

<h3>2.1 Age Requirement</h3>
<p>You must be at least 21 years of age to use this Service. By using the Service, you represent and warrant that you are 21 years or older.</p>

<h3>2.2 Legal Capacity</h3>
<p>You must have the legal capacity to enter into these Terms and to perform all obligations hereunder.</p>

<h3>2.3 Accurate Information</h3>
<p>You agree to provide accurate, current, and complete information during registration and to update such information to keep it accurate, current, and complete.</p>

<h2>3. User Conduct and Community Standards</h2>

<h3>3.1 Expected Behavior</h3>
<p>Users agree to:</p>
<ul>
<li>Treat all community members with respect and dignity</li>
<li>Obtain explicit consent for all interactions</li>
<li>Respect boundaries and safe words</li>
<li>Maintain confidentiality of other members</li>
<li>Follow event-specific rules and guidelines</li>
</ul>

<h3>3.2 Prohibited Conduct</h3>
<p>The following behaviors are strictly prohibited:</p>
<ul>
<li>Harassment, intimidation, or threatening behavior</li>
<li>Non-consensual activities of any kind</li>
<li>Photography or recording without explicit consent</li>
<li>Sharing others' personal information without permission</li>
<li>Discrimination based on race, gender, sexuality, or other protected characteristics</li>
<li>Illegal activities or substances at events</li>
<li>Misrepresentation of experience or credentials</li>
</ul>

<h2>4. Payment Terms</h2>

<h3>4.1 Event Fees</h3>
<ul>
<li>All event fees are clearly displayed before registration</li>
<li>Payment is due at time of registration</li>
<li>Fees are processed through secure third-party payment providers</li>
</ul>

<h3>4.2 Refund Policy</h3>
<ul>
<li>Cancellations 7+ days before event: Full refund minus processing fee</li>
<li>Cancellations 3-6 days before event: 50% refund</li>
<li>Cancellations less than 3 days before event: No refund</li>
<li>No-shows: No refund</li>
<li>Exceptions may be made for emergencies at our discretion</li>
</ul>

<h2>5. Event Policies and Liability</h2>

<h3>5.1 Assumption of Risk</h3>
<p>Participation in rope bondage activities involves inherent risks. By attending events, you acknowledge and assume all risks associated with these activities.</p>

<h3>5.2 Liability Waiver</h3>
<p>You agree to release and hold harmless WitchCityRope, its organizers, volunteers, and venue partners from any claims, damages, or injuries arising from your participation.</p>

<h3>5.3 Safety Requirements</h3>
<ul>
<li>All participants must attend safety briefings</li>
<li>Use of safety equipment as directed</li>
<li>Compliance with spotters and safety monitors</li>
<li>Immediate reporting of accidents or concerns</li>
</ul>

<h2>6. Privacy and Confidentiality</h2>

<h3>6.1 Privacy Policy</h3>
<p>Your use of the Service is also governed by our Privacy Policy, which is incorporated by reference.</p>

<h3>6.2 Community Confidentiality</h3>
<p>You agree to maintain the confidentiality of other members' identities and personal information learned through the Service.</p>

<h2>7. Dispute Resolution</h2>

<h3>7.1 Informal Resolution</h3>
<p>We encourage resolution of disputes through direct communication. Contact us at <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a>.</p>

<h3>7.2 Binding Arbitration</h3>
<p>If informal resolution fails, disputes shall be resolved through binding arbitration in accordance with the American Arbitration Association rules.</p>

<h2>8. Account Termination</h2>

<h3>8.1 Termination by You</h3>
<p>You may terminate your account at any time through account settings or by contacting support.</p>

<h3>8.2 Termination by Us</h3>
<p>We may suspend or terminate accounts for:</p>
<ul>
<li>Violation of these Terms or Community Guidelines</li>
<li>Safety concerns</li>
<li>Fraudulent or illegal activity</li>
<li>Extended inactivity</li>
</ul>

<h2>9. General Provisions</h2>

<h3>9.1 Governing Law</h3>
<p>These Terms are governed by the laws of Massachusetts, without regard to conflict of law principles.</p>

<h3>9.2 Modifications</h3>
<p>We may modify these Terms at any time. Continued use after changes constitutes acceptance of modified Terms.</p>

<h2>10. Contact Information</h2>
<p>For questions about these Terms:</p>
<p><strong>Email:</strong> <a href=""mailto:legal@witchcityrope.com"">legal@witchcityrope.com</a><br>
<strong>Mail:</strong> WitchCityRope Legal Team, Salem, MA</p>

<hr>
<p>By using WitchCityRope, you acknowledge that you have read, understood, and agree to be bound by these Terms of Service.</p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: Privacy Policy Page
                new ContentPage
                {
                    Slug = "privacy-policy",
                    Title = "Privacy Policy",
                    Content = @"<h1>Privacy Policy</h1>
<p><strong>Effective Date:</strong> October 23, 2025<br>
<strong>Last Updated:</strong> October 23, 2025</p>

<h2>1. Introduction</h2>
<p>WitchCityRope (""we,"" ""our,"" or ""us"") is committed to protecting your privacy and ensuring the security of your personal information. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our website and services.</p>
<p>We understand the sensitive nature of our community and are dedicated to maintaining the confidentiality and privacy of all our members.</p>

<h2>2. Information We Collect</h2>

<h3>2.1 Information You Provide to Us</h3>
<ul>
<li><strong>Account Information</strong>: Username, email address, date of birth (for age verification), and password</li>
<li><strong>Profile Information</strong>: Optional profile details you choose to share (pronouns, experience level, interests)</li>
<li><strong>Event Registration</strong>: Name, contact information, emergency contact details, and any health/safety information you provide</li>
<li><strong>Communication Data</strong>: Messages sent through our platform, forum posts, and event feedback</li>
<li><strong>Payment Information</strong>: Processed securely through third-party payment processors (we do not store credit card details)</li>
</ul>

<h3>2.2 Information Collected Automatically</h3>
<ul>
<li><strong>Usage Data</strong>: Pages visited, features used, time spent on site</li>
<li><strong>Device Information</strong>: IP address, browser type, operating system</li>
<li><strong>Cookies and Similar Technologies</strong>: Session cookies, preference cookies, and analytics cookies</li>
</ul>

<h2>3. How We Use Your Information</h2>
<p>We use your information to:</p>
<ul>
<li>Provide and maintain our services</li>
<li>Process event registrations and payments</li>
<li>Communicate with you about events, updates, and safety information</li>
<li>Verify age and ensure community safety</li>
<li>Improve our services and user experience</li>
<li>Comply with legal obligations</li>
<li>Protect against fraudulent or illegal activity</li>
</ul>

<h2>4. Data Sharing and Third Parties</h2>

<h3>4.1 We DO NOT:</h3>
<ul>
<li>Sell your personal information</li>
<li>Share your information with marketing companies</li>
<li>Disclose member lists or attendance records</li>
</ul>

<h3>4.2 We MAY share information with:</h3>
<ul>
<li><strong>Event Organizers</strong>: Limited information necessary for event management</li>
<li><strong>Payment Processors</strong>: To process transactions securely</li>
<li><strong>Legal Authorities</strong>: Only when required by law or to protect safety</li>
<li><strong>Service Providers</strong>: Who assist us in operating our platform (bound by confidentiality agreements)</li>
</ul>

<h2>5. Your Rights (GDPR Compliance)</h2>
<p>You have the right to:</p>
<ul>
<li><strong>Access</strong>: Request a copy of your personal data</li>
<li><strong>Rectification</strong>: Correct inaccurate personal data</li>
<li><strong>Erasure</strong>: Request deletion of your personal data (""right to be forgotten"")</li>
<li><strong>Restriction</strong>: Limit processing of your personal data</li>
<li><strong>Portability</strong>: Receive your data in a structured, machine-readable format</li>
<li><strong>Object</strong>: Oppose processing of your personal data</li>
<li><strong>Withdraw Consent</strong>: At any time for consent-based processing</li>
</ul>
<p>To exercise these rights, contact us at <a href=""mailto:privacy@witchcityrope.com"">privacy@witchcityrope.com</a></p>

<h2>6. Data Retention</h2>
<ul>
<li><strong>Account Data</strong>: Retained while your account is active</li>
<li><strong>Event Data</strong>: Retained for 3 years for safety and legal purposes</li>
<li><strong>Communication Data</strong>: Retained for 1 year unless legally required longer</li>
<li><strong>Deleted Accounts</strong>: Anonymized after 30 days, except data required for legal compliance</li>
</ul>

<h2>7. Security Measures</h2>
<p>We implement industry-standard security measures including:</p>
<ul>
<li>Encryption of data in transit and at rest</li>
<li>Regular security audits and updates</li>
<li>Limited access controls</li>
<li>Secure password requirements</li>
<li>Two-factor authentication options</li>
<li>Regular backups</li>
</ul>

<h2>8. Cookie Policy</h2>

<h3>8.1 Essential Cookies</h3>
<p>Required for site functionality, authentication, and security</p>

<h3>8.2 Preference Cookies</h3>
<p>Remember your settings and preferences</p>

<h3>8.3 Analytics Cookies</h3>
<p>Help us understand how our site is used (anonymized data only)</p>
<p>You can manage cookie preferences through your browser settings.</p>

<h2>9. Children's Privacy</h2>
<p>Our services are strictly for individuals 21 years or older. We do not knowingly collect information from anyone under 21. If we discover underage data, it will be immediately deleted.</p>

<h2>10. International Data Transfers</h2>
<p>If you access our services from outside the United States, your information may be transferred to and processed in the United States. We ensure appropriate safeguards are in place for international transfers.</p>

<h2>11. Changes to This Policy</h2>
<p>We may update this Privacy Policy periodically. We will notify you of significant changes via email and/or prominent notice on our website.</p>

<h2>12. Contact Information</h2>
<p>For privacy-related questions or concerns:</p>
<p><strong>Email:</strong> <a href=""mailto:privacy@witchcityrope.com"">privacy@witchcityrope.com</a><br>
<strong>Mail:</strong> WitchCityRope Privacy Team, Salem, MA</p>

<hr>
<p>By using WitchCityRope, you acknowledge that you have read and understood this Privacy Policy.</p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: Getting Started Page
                new ContentPage
                {
                    Slug = "getting-started",
                    Title = "Getting Started with Shibari",
                    Content = @"<h1>Getting Started with Shibari</h1>
<p><strong>Welcome to Your Rope Journey!</strong></p>
<p>Whether you're curious about rope bondage, interested in exploring a new form of artistic expression, or drawn to the intimacy and connection that shibari offers, you're in the right place. This guide will help you understand what shibari is, why people practice it, and how to begin your journey safely within a supportive community.</p>

<h2>What is Shibari?</h2>
<p><strong>Shibari</strong> (sometimes called <strong>Kinbaku</strong>) is the Japanese art of rope bondage. More than just tying knots, shibari is:</p>
<ul>
<li><strong>An Art Form</strong> - Rope creates beautiful patterns on the body, turning the human form into living sculpture</li>
<li><strong>A Form of Expression</strong> - Both physical and emotional, allowing for deep creative and personal exploration</li>
<li><strong>A Connection Tool</strong> - Building trust and communication between rope partners</li>
<li><strong>A Meditative Practice</strong> - Requiring focus, presence, and mindfulness from both the person tying (top/rigger) and the person being tied (bottom/model)</li>
<li><strong>A Community</strong> - Bringing together people who share a passion for rope, safety, and connection</li>
</ul>

<h2>Why Do People Practice Shibari?</h2>
<p>People are drawn to rope bondage for many different reasons, and there's no single ""right"" reason to explore it:</p>

<h3>Artistic Expression</h3>
<p>The visual beauty of rope on the body is captivating. Many practitioners are drawn to the aesthetic artistry - the geometric patterns, the interplay of rope and skin, and the sculptural quality of well-executed ties. Shibari can be photographed, performed, or simply enjoyed in the moment as a form of living art.</p>

<h3>Connection and Intimacy</h3>
<p>Rope bondage requires profound trust and communication. The process of negotiating a scene, tying someone, or being tied creates a unique form of connection. Many describe it as a dance between partners, requiring constant awareness, empathy, and attunement to each other.</p>

<h3>Sensation and Experience</h3>
<p>Rope provides unique physical sensations - the pressure of rope against skin, the feeling of restraint, the surrender of movement, or the challenge of an intricate tie. Some enjoy the meditative headspace that rope creates, while others appreciate the physical intensity.</p>

<h3>Power Exchange</h3>
<p>For some, rope is a tool for exploring consensual power dynamics. The act of being tied or tying someone can involve surrender, control, vulnerability, or dominance - always within carefully negotiated boundaries.</p>

<h3>Personal Challenge</h3>
<p>Learning rope is a skill-building journey. Riggers develop technical knowledge, body awareness, and creativity. Bottoms develop communication skills, body awareness, and understanding of their own limits and desires.</p>

<h3>Community and Belonging</h3>
<p>The rope community welcomes people exploring alternative expressions of sexuality, intimacy, and art. Many find acceptance, friendship, and authentic connection within rope spaces.</p>

<h2>Safety: The Foundation of All Rope Practice</h2>
<p><strong>Rope bondage involves inherent physical and emotional risks.</strong> Safety isn't optional - it's the foundation that allows us to explore freely and authentically.</p>

<h3>Physical Safety</h3>
<ul>
<li><strong>Nerve Damage</strong>: Improper rope placement can compress nerves, potentially causing temporary or permanent numbness, tingling, or loss of function</li>
<li><strong>Circulation Issues</strong>: Rope can restrict blood flow if too tight or placed incorrectly</li>
<li><strong>Falls</strong>: Suspension carries risk of falling if rigging fails or the bottom loses consciousness</li>
<li><strong>Joint Stress</strong>: Certain positions can stress joints, especially shoulders</li>
</ul>
<p><strong>Risk mitigation requires:</strong></p>
<ul>
<li>Proper education on anatomy and safe tie placement</li>
<li>Regular safety checks during rope work (circulation, sensation, comfort)</li>
<li>Safety shears always within reach to quickly cut someone free</li>
<li>Never leaving a tied person unattended</li>
<li>Starting with simple ties and progressing slowly</li>
<li>Understanding your own and your partner's physical limitations</li>
</ul>

<h3>Emotional Safety</h3>
<ul>
<li><strong>Vulnerability</strong>: Being tied or tying someone creates emotional vulnerability</li>
<li><strong>Triggers</strong>: Restraint or certain sensations may trigger unexpected emotional responses</li>
<li><strong>Aftercare</strong>: Both partners may need emotional support after intense scenes</li>
</ul>
<p><strong>Emotional safety requires:</strong></p>
<ul>
<li>Thorough negotiation before any rope work</li>
<li>Established safe words and check-ins</li>
<li>Respect for boundaries and consent</li>
<li>Aftercare and debriefing</li>
<li>Self-awareness and honest communication</li>
</ul>

<h3>The Importance of Consent</h3>
<p>Consent is <strong>non-negotiable</strong> in all rope practice. Consent must be:</p>
<ul>
<li><strong>Informed</strong>: All parties understand what they're consenting to and the risks involved</li>
<li><strong>Enthusiastic</strong>: A clear ""yes,"" not just the absence of ""no""</li>
<li><strong>Specific</strong>: Consenting to one activity doesn't mean consenting to everything</li>
<li><strong>Reversible</strong>: Can be withdrawn at any time, for any reason</li>
<li><strong>Ongoing</strong>: Requires regular check-ins throughout the scene</li>
</ul>

<h2>Why Learn in a Community Setting?</h2>
<p>While it might be tempting to learn rope from online videos alone, <strong>in-person community education is essential for safe practice</strong>. Here's why:</p>

<h3>Hands-On Feedback</h3>
<p>Experienced instructors can:</p>
<ul>
<li>Correct your technique in real-time</li>
<li>Show you the subtle details videos might miss</li>
<li>Help you understand the difference between ""looks right"" and ""is safe""</li>
<li>Identify dangerous habits before they become ingrained</li>
</ul>

<h3>Safety Culture</h3>
<p>Learning within a community means:</p>
<ul>
<li>Absorbing safety-first mindset from experienced practitioners</li>
<li>Seeing how others handle negotiations, check-ins, and emergencies</li>
<li>Understanding that safety is everyone's responsibility</li>
<li>Learning from others' experiences and near-misses</li>
</ul>

<h3>Skill Progression</h3>
<p>Community education provides:</p>
<li>Structured progression from basics to advanced techniques</li>
<li>Guidance on when you're ready for more complex ties</li>
<li>Mentorship from experienced practitioners</li>
<li>Practice partners at similar skill levels</li>
</ul>

<h3>Diverse Perspectives</h3>
<p>In-person community offers:</p>
<ul>
<li>Exposure to different rope styles and philosophies</li>
<li>Learning from multiple instructors with varied approaches</li>
<li>Seeing rope work on diverse body types</li>
<li>Understanding that there's no single ""right way""</li>
</ul>

<h3>Accountability and Support</h3>
<p>Being part of a community means:</p>
<ul>
<li>Standards of conduct that protect everyone</li>
<li>Support systems for addressing concerns</li>
<li>People who hold you accountable to safe practices</li>
<li>Belonging to something larger than yourself</li>
</ul>

<h2>Why Witch City Rope?</h2>
<p>WitchCityRope exists to provide exactly the kind of safe, supportive, educational community environment that rope bondage requires. Here's what makes us different:</p>

<h3>Safety-First Education</h3>
<ul>
<li>All instructors are experienced, safety-trained rope practitioners</li>
<li>Comprehensive beginner workshops covering fundamentals and safety</li>
<li>Small class sizes ensuring personal attention</li>
<li>Mandatory safety briefings at all events</li>
<li>Safety monitors present during practice sessions</li>
</ul>

<h3>Inclusive and Welcoming Community</h3>
<ul>
<li>All genders, sexualities, and body types welcome</li>
<li>Beginner-friendly environment - everyone was new once</li>
<li>Respect for diverse rope interests and styles</li>
<li>Zero tolerance for harassment or discrimination</li>
<li>Confidentiality and discretion for all members</li>
</ul>

<h3>Progressive Skill Development</h3>
<ul>
<li>Clear pathway from beginner to advanced techniques</li>
<li>Regular workshops at all skill levels</li>
<li>Rope jams for practice with community support</li>
<li>Mentorship opportunities</li>
<li>Private lessons available for personalized instruction</li>
</ul>

<h3>Community and Connection</h3>
<ul>
<li>Meet others who share your interests</li>
<li>Find practice partners and friends</li>
<li>Attend social events beyond rope practice</li>
<li>Be part of Salem's vibrant alternative community</li>
</ul>

<h3>Commitment to Excellence</h3>
<ul>
<li>Continuous instructor training and development</li>
<li>Regular community feedback and improvement</li>
<li>Adherence to community standards and best practices</li>
<li>Transparent communication and organization</li>
</ul>

<h2>Getting Started: Your Next Steps</h2>

<h3>Step 1: Apply for Vetting</h3>
<p>To maintain a safe community, we require all participants to complete our vetting process before attending member-only events.</p>
<p><strong><a href=""/vetting/apply"">→ Start Your Vetting Application</a></strong></p>
<p>The vetting process typically takes 2-4 weeks and includes:</p>
<ul>
<li>Application submission with references</li>
<li>Attendance at orientation events</li>
<li>Interview with our vetting committee</li>
<li>Background check for certain membership levels</li>
</ul>

<h3>Step 2: Attend Your First Workshop</h3>
<p>We offer beginner-friendly workshops specifically designed for people new to rope:</p>
<ul>
<li><strong>Intro to Shibari</strong> - Learn basic knots, safety principles, and your first simple ties</li>
<li><strong>Rope Safety Fundamentals</strong> - Deep dive into risk awareness and safe practices</li>
<li><strong>Communication for Rope</strong> - Negotiation, consent, and check-ins</li>
</ul>
<p>Check our <strong><a href=""/events"">Events Calendar</a></strong> for upcoming beginner workshops.</p>

<h3>Step 3: Get Your Rope</h3>
<p>We recommend starting with:</p>
<ul>
<li><strong>Material</strong>: Jute or hemp (natural fibers that grip well)</li>
<li><strong>Length</strong>: 2-3 pieces of 30-foot (10-meter) rope</li>
<li><strong>Diameter</strong>: 6mm for most people</li>
</ul>
<p>Instructors can provide specific recommendations based on your goals and budget. Many workshops also offer rope for purchase.</p>

<h3>Step 4: Continue Learning</h3>
<p>Rope is a journey, not a destination:</p>
<ul>
<li>Attend regular workshops and rope jams</li>
<li>Practice between events (with proper safety precautions)</li>
<li>Ask questions and seek mentorship</li>
<li>Progress at your own pace - there's no rush</li>
</ul>

<h2>External Learning Resources</h2>
<p>While in-person education is essential, these resources can supplement your learning:</p>

<h3>Shibari Study</h3>
<p><strong><a href=""https://shibaristudy.com"" target=""_blank"" rel=""noopener"">shibaristudy.com</a></strong></p>
<p>Comprehensive online rope bondage tutorials and courses. Excellent reference material for techniques learned in workshops. Features video tutorials from respected riggers worldwide.</p>

<h3>Rope 365</h3>
<p><strong><a href=""https://rope365.com"" target=""_blank"" rel=""noopener"">rope365.com</a></strong></p>
<p>Daily rope inspiration and technique videos. Great for seeing diverse rope styles and staying motivated in your practice.</p>

<p><strong>Remember:</strong> Online resources should <em>supplement</em>, not replace, in-person instruction and community learning.</p>

<h2>Frequently Asked Questions</h2>

<h3>Do I need a partner to get started?</h3>
<p>Not at all! Many workshops include partner rotation, allowing solo attendees to practice with others. You can also learn rope as a self-tying art (though some ties require a partner).</p>

<h3>Is shibari sexual?</h3>
<p>It can be, but doesn't have to be. Rope can be purely artistic, meditative, social, or intimate without being sexual. Each person's experience and interest is unique and valid.</p>

<h3>What if I'm not flexible or strong?</h3>
<p>Rope can be adapted to all body types and abilities. Good rigging works with your body, not against it. Instructors will help you find positions and ties that work for you.</p>

<h3>How long does it take to learn?</h3>
<p>Rope is a lifelong learning journey. You can learn simple, safe ties in your first class. More complex techniques like suspension take months or years of practice. Go at your own pace and enjoy the process.</p>

<h3>Is it safe?</h3>
<p>With proper education, safety precautions, and risk awareness, rope can be practiced safely. However, all rope bondage carries some inherent risk. That's why education and community standards are so important.</p>

<h3>What should I wear to my first workshop?</h3>
<p>Comfortable clothing you can move in. Some people prefer long sleeves/pants to reduce rope burn; others prefer shorts/tank tops. Avoid loose jewelry. Bring water and an open mind!</p>

<h2>Ready to Begin?</h2>
<p>Your rope journey starts with a single step. Whether you're curious about the art form, seeking connection, or exploring a new skill, WitchCityRope is here to support you.</p>

<div style=""background-color: #f5f5f5; padding: 20px; margin: 30px 0; border-left: 4px solid #8b5cf6;"">
<h3 style=""margin-top: 0;"">Take Your First Step</h3>
<p><strong>1. Apply for vetting</strong> → <a href=""/vetting/apply"">Start Application</a></p>
<p><strong>2. Browse upcoming events</strong> → <a href=""/events"">View Calendar</a></p>
<p><strong>3. Learn more about our community</strong> → <a href=""/cms/about-us"">About Us</a></p>
<p><strong>4. Read our safety practices</strong> → <a href=""/cms/safety-practices"">Safety Guide</a></p>
</div>

<p>Questions? Contact us at <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a> or check our <a href=""/cms/faq"">FAQ page</a>.</p>

<hr>
<p><strong>Welcome to WitchCityRope.</strong><br>
<em>We're excited to support you on your rope journey.</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: Community Guidelines Page
                new ContentPage
                {
                    Slug = "community-guidelines",
                    Title = "Community Guidelines",
                    Content = @"<h1>Community Guidelines</h1>
<p><strong>Effective Date:</strong> October 23, 2025<br>
<strong>Last Updated:</strong> October 23, 2025</p>

<h2>Our Community Values</h2>
<p>WitchCityRope is built on the foundations of <strong>Consent</strong>, <strong>Communication</strong>, <strong>Safety</strong>, and <strong>Respect</strong>. These guidelines ensure our community remains welcoming, inclusive, and safe for all members.</p>

<h2>1. Consent is Paramount</h2>

<h3>1.1 Explicit Consent Required</h3>
<ul>
<li>All activities require clear, enthusiastic, and ongoing consent</li>
<li>Consent must be obtained before any rope work or physical contact</li>
<li>""Maybe"" or silence is not consent</li>
<li>Consent can be withdrawn at any time</li>
</ul>

<h3>1.2 Negotiation</h3>
<ul>
<li>Discuss boundaries, limits, and expectations before play</li>
<li>Establish safe words/signals clearly</li>
<li>Honor all stated limits without question</li>
<li>Check in regularly during activities</li>
</ul>

<h3>1.3 Altered States</h3>
<ul>
<li>No play while intoxicated or impaired</li>
<li>Ensure all parties are in a clear state of mind</li>
<li>Alcohol may be present at some events but moderation is expected</li>
</ul>

<h2>2. Safety First</h2>

<h3>2.1 Risk Awareness</h3>
<ul>
<li>Understand that rope bondage involves inherent risks</li>
<li>Take responsibility for your own safety and that of your partners</li>
<li>Know your limits and communicate them clearly</li>
<li>Never engage in activities beyond your skill level</li>
</ul>

<h3>2.2 Safety Requirements</h3>
<ul>
<li>Attend mandatory safety briefings at events</li>
<li>Keep safety shears accessible at all times during rope work</li>
<li>Know basic safety protocols (circulation checks, nerve points, etc.)</li>
<li>Report any accidents or injuries immediately</li>
</ul>

<h3>2.3 Learning and Education</h3>
<ul>
<li>Seek proper education before attempting new techniques</li>
<li>Ask questions when uncertain</li>
<li>Share safety knowledge with the community</li>
<li>Acknowledge that everyone is still learning</li>
</ul>

<h2>3. Respect and Inclusivity</h2>

<h3>3.1 Inclusive Environment</h3>
<ul>
<li>Welcome people of all genders, sexualities, and backgrounds</li>
<li>Use preferred names and pronouns</li>
<li>Celebrate diversity in our community</li>
<li>Challenge discrimination when you see it</li>
</ul>

<h3>3.2 Personal Boundaries</h3>
<ul>
<li>Respect others' personal space</li>
<li>Ask before touching anyone or their rope/equipment</li>
<li>Accept ""no"" gracefully and without argument</li>
<li>Don't make assumptions about others' interests or limits</li>
</ul>

<h3>3.3 Communication</h3>
<ul>
<li>Communicate clearly and honestly</li>
<li>Listen actively to others</li>
<li>Address conflicts respectfully and directly</li>
<li>Seek mediation when needed</li>
</ul>

<h2>4. Privacy and Discretion</h2>

<h3>4.1 Photography and Recording</h3>
<p><strong>No photography or recording without explicit consent from all visible parties</strong></p>
<ul>
<li>Designated photo areas may be established at events</li>
<li>Share photos only with permission from all subjects</li>
<li>Respect ""no photo"" indicators (wristbands, etc.)</li>
</ul>

<h3>4.2 Confidentiality</h3>
<ul>
<li>What happens at events stays at events</li>
<li>Don't ""out"" community members in vanilla spaces</li>
<li>Protect others' privacy on social media</li>
<li>Use scene names when requested</li>
</ul>

<h2>5. Event-Specific Guidelines</h2>

<h3>5.1 Arrival and Check-in</h3>
<ul>
<li>Arrive on time for safety briefings</li>
<li>Provide accurate information during registration</li>
<li>Wear required identification (wristbands, badges)</li>
<li>Respect venue rules and staff</li>
</ul>

<h3>5.2 During Events</h3>
<ul>
<li>Keep play within designated areas</li>
<li>Clean up after yourself</li>
<li>Be mindful of space - don't crowd others</li>
<li>Share equipment fairly</li>
<li>Respect dungeon monitors' decisions</li>
</ul>

<h2>6. Prohibited Behaviors</h2>
<p>The following will result in immediate removal and possible ban:</p>

<h3>6.1 Consent Violations</h3>
<ul>
<li>Any non-consensual touch or activity</li>
<li>Ignoring safe words or withdrawal of consent</li>
<li>Pressuring or coercing others</li>
</ul>

<h3>6.2 Safety Violations</h3>
<ul>
<li>Reckless behavior endangering others</li>
<li>Refusing to follow safety protocols</li>
<li>Playing while intoxicated</li>
<li>Bringing weapons (except safety shears)</li>
</ul>

<h3>6.3 Harassment</h3>
<ul>
<li>Sexual harassment or unwanted advances</li>
<li>Bullying or intimidation</li>
<li>Discriminatory language or actions</li>
<li>Stalking or unwanted persistent contact</li>
</ul>

<h2>7. Accountability and Reporting</h2>

<h3>7.1 Community Accountability</h3>
<ul>
<li>We all share responsibility for maintaining our standards</li>
<li>Speak up when you see concerning behavior</li>
<li>Support those who report issues</li>
<li>Learn from mistakes and do better</li>
</ul>

<h3>7.2 Reporting Process</h3>
<ul>
<li>Report violations to event staff immediately</li>
<li>Email <a href=""mailto:reports@witchcityrope.com"">reports@witchcityrope.com</a> for non-urgent issues</li>
<li>All reports are taken seriously and investigated</li>
<li>Confidentiality maintained when possible</li>
</ul>

<h3>7.3 Consequences</h3>
<ul>
<li>Warnings for minor first offenses</li>
<li>Temporary suspension for serious violations</li>
<li>Permanent ban for severe or repeated violations</li>
<li>Legal action when appropriate</li>
</ul>

<h2>Acknowledgment</h2>
<p>By participating in WitchCityRope events and spaces, you agree to follow these Community Guidelines and support their enforcement.</p>

<hr>
<p><strong>Remember:</strong> When in doubt, ask. When uncertain, don't. When concerned, speak up.</p>
<p><strong>Together, we create a community where everyone can explore safely and authentically.</strong></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // NEW: Refund Policy Page
                new ContentPage
                {
                    Slug = "refund-policy",
                    Title = "Refund Policy",
                    Content = @"<h1>Refund Policy</h1>
<p><strong>Effective Date:</strong> October 23, 2025<br>
<strong>Last Updated:</strong> October 23, 2025</p>

<h2>Our Commitment to Accessibility</h2>
<p>WitchCityRope is committed to making our events and educational offerings accessible to all community members. Our refund policy balances this commitment with the practical realities of event planning and venue costs.</p>

<h2>Event Registration Refund Policy</h2>

<h3>Standard Refund Timeline</h3>
<p>Refunds for event registrations (workshops, classes, social events, and ticketed events) follow this schedule:</p>

<h4>7+ Days Before Event</h4>
<ul>
<li><strong>Refund Amount:</strong> Full refund minus payment processing fee</li>
<li><strong>Processing Fee:</strong> Typically 2.9% + $0.30 for credit card transactions</li>
<li><strong>Processing Time:</strong> 5-7 business days</li>
<li><strong>How to Request:</strong> Cancel through your account dashboard or email <a href=""mailto:refunds@witchcityrope.com"">refunds@witchcityrope.com</a></li>
</ul>

<h4>3-6 Days Before Event</h4>
<ul>
<li><strong>Refund Amount:</strong> 50% of registration price</li>
<li><strong>Reason:</strong> At this point, we have already committed to venue costs and instructor fees based on registration numbers</li>
<li><strong>Processing Time:</strong> 5-7 business days</li>
<li><strong>How to Request:</strong> Email <a href=""mailto:refunds@witchcityrope.com"">refunds@witchcityrope.com</a> with your registration details</li>
</ul>

<h4>Less Than 3 Days Before Event</h4>
<ul>
<li><strong>Refund Amount:</strong> No refund</li>
<li><strong>Reason:</strong> Venue and instructor commitments are finalized; late cancellations prevent us from filling the spot</li>
<li><strong>Alternative:</strong> You may transfer your registration to another person (see Transfer Policy below)</li>
</ul>

<h4>No-Shows</h4>
<ul>
<li><strong>Refund Amount:</strong> No refund</li>
<li><strong>Note:</strong> If you cannot attend, please cancel in advance so we can offer your spot to someone on the waitlist</li>
</ul>

<h2>Emergency and Hardship Exceptions</h2>

<h3>Medical Emergencies</h3>
<p>We understand that unexpected medical situations arise. Refund requests due to medical emergencies will be considered on a case-by-case basis, even within the ""no refund"" window.</p>
<p><strong>Required Documentation:</strong></p>
<ul>
<li>Doctor's note or medical documentation</li>
<li>Explanation of why attendance is not possible</li>
<li>Request submitted within 7 days of the event</li>
</ul>
<p><strong>Contact:</strong> <a href=""mailto:refunds@witchcityrope.com"">refunds@witchcityrope.com</a> with subject line ""Medical Emergency Refund Request""</p>

<h3>Financial Hardship</h3>
<p>If you are experiencing financial hardship and need to cancel, please reach out to us. We may be able to offer:</p>
<ul>
<li>Credit toward a future event</li>
<li>Partial refund beyond our standard policy</li>
<li>Payment plan for future events</li>
</ul>
<p><strong>Contact:</strong> <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a> - All hardship requests are handled confidentially</p>

<h3>Event Cancellation by WitchCityRope</h3>
<p>If WitchCityRope cancels an event for any reason:</p>
<ul>
<li><strong>Full refund</strong> including all processing fees</li>
<li><strong>OR</strong> credit toward any future event of equal or greater value</li>
<li>Notification sent within 24 hours of cancellation decision</li>
<li>Refunds processed within 5-7 business days</li>
</ul>

<h3>Event Rescheduling by WitchCityRope</h3>
<p>If WitchCityRope reschedules an event:</p>
<ul>
<li>Your registration automatically transfers to the new date</li>
<li>If you cannot attend the new date, <strong>full refund</strong> available (no processing fee deducted)</li>
<li>Request refund within 14 days of rescheduling notification</li>
</ul>

<h2>Registration Transfer Policy</h2>

<h3>Transferring to Another Person</h3>
<p>If you cannot attend, you may transfer your registration to another person:</p>
<ul>
<li><strong>Deadline:</strong> Up to 24 hours before the event</li>
<li><strong>Requirements:</strong>
  <ul>
    <li>Transferee must meet all event requirements (age, vetting status, prerequisites)</li>
    <li>Both parties must complete transfer form</li>
    <li>Transferee must agree to all event terms and waivers</li>
  </ul>
</li>
<li><strong>Fee:</strong> No transfer fee</li>
<li><strong>How to Transfer:</strong> Email <a href=""mailto:registrations@witchcityrope.com"">registrations@witchcityrope.com</a> with both parties' information</li>
</ul>

<h3>Transferring to a Different Event</h3>
<p>You may request to transfer your registration to a different event:</p>
<ul>
<li><strong>Deadline:</strong> At least 7 days before the original event</li>
<li><strong>Conditions:</strong>
  <ul>
    <li>New event must be within 90 days of original event</li>
    <li>New event must be of equal or lesser value (price difference not refunded)</li>
    <li>If new event costs more, pay difference before transfer</li>
    <li>Subject to availability in new event</li>
  </ul>
</li>
<li><strong>Fee:</strong> $5 administrative fee</li>
<li><strong>How to Transfer:</strong> Email <a href=""mailto:registrations@witchcityrope.com"">registrations@witchcityrope.com</a></li>
</ul>

<h2>Private Lessons Cancellation Policy</h2>

<p>Private lessons have a different cancellation policy due to instructors' time commitments:</p>

<h3>Cancellation Timeline</h3>
<ul>
<li><strong>48+ hours before lesson:</strong> Full refund minus processing fee</li>
<li><strong>24-48 hours before lesson:</strong> 50% refund</li>
<li><strong>Less than 24 hours:</strong> No refund</li>
<li><strong>No-shows:</strong> No refund</li>
</ul>

<h3>Rescheduling Private Lessons</h3>
<ul>
<li>Free rescheduling available up to 48 hours before lesson</li>
<li>Contact your instructor directly or email <a href=""mailto:lessons@witchcityrope.com"">lessons@witchcityrope.com</a></li>
<li>Lesson must be rescheduled within 90 days</li>
</ul>

<h2>Membership and Annual Support</h2>

<h3>Sliding Scale Donations</h3>
<p>Annual community support donations are non-refundable as they directly support our operational costs and community initiatives. However:</p>
<ul>
<li>You may adjust your support level at any time for future years</li>
<li>Hardship support is available - contact <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a></li>
<li>We never turn anyone away for inability to pay</li>
</ul>

<h2>Multi-Event Packages and Passes</h2>

<h3>Workshop Series Packages</h3>
<p>If you purchase a multi-workshop package:</p>
<ul>
<li><strong>Unused workshops:</strong> Refundable at pro-rated amount (per-workshop price) if cancelled 7+ days before each workshop</li>
<li><strong>Completed workshops:</strong> Non-refundable</li>
<li><strong>Package discounts:</strong> If you partially use a package and request refund, the discount may be recalculated</li>
</ul>

<p><strong>Example:</strong> You purchase a 4-workshop package at $120 ($30/workshop, normally $40 each). You attend 2 workshops and cancel before the remaining 2 (with proper notice). Refund: $60 for the 2 unused workshops.</p>

<h2>Processing Information</h2>

<h3>Refund Processing Time</h3>
<ul>
<li><strong>Approval:</strong> Most refund requests processed within 2 business days</li>
<li><strong>Refund to Original Payment Method:</strong> 5-7 business days after approval</li>
<li><strong>Credit Card Refunds:</strong> May take 1-2 billing cycles to appear on statement</li>
<li><strong>PayPal Refunds:</strong> Typically within 24-48 hours of approval</li>
</ul>

<h3>Payment Processing Fees</h3>
<p>Payment processing fees are deducted from refunds (except in cases of event cancellation by WitchCityRope):</p>
<ul>
<li>Credit/Debit Card: Approximately 2.9% + $0.30</li>
<li>PayPal: Approximately 2.9% + $0.30</li>
<li>The exact fee depends on your payment method and is charged by the payment processor, not by WitchCityRope</li>
</ul>

<h3>Refund Method</h3>
<ul>
<li>Refunds are issued to the original payment method used for registration</li>
<li>If original payment method is no longer valid, contact <a href=""mailto:refunds@witchcityrope.com"">refunds@witchcityrope.com</a> to arrange alternative refund method</li>
</ul>

<h2>Special Circumstances</h2>

<h3>Weather-Related Cancellations</h3>
<p>For outdoor events or events affected by severe weather:</p>
<ul>
<li>If WitchCityRope cancels due to weather: Full refund or transfer to rescheduled date</li>
<li>If you choose not to attend due to weather but event proceeds: Standard refund policy applies</li>
</ul>

<h3>Venue Issues</h3>
<p>If venue becomes unavailable and we cannot find suitable alternative:</p>
<ul>
<li>Full refund including processing fees</li>
<li>OR credit toward future event of your choice</li>
</ul>

<h3>Instructor Cancellation</h3>
<p>If the scheduled instructor cannot teach and we cannot find a suitable replacement:</p>
<ul>
<li>Full refund including processing fees</li>
<li>OR transfer to future date when instructor is available</li>
<li>Decision to proceed with replacement instructor or cancel event will be communicated clearly</li>
</ul>

<h2>Disputes and Appeals</h2>

<h3>If You Disagree with a Refund Decision</h3>
<ol>
<li><strong>Contact Us:</strong> Email <a href=""mailto:refunds@witchcityrope.com"">refunds@witchcityrope.com</a> explaining your situation</li>
<li><strong>Appeal:</strong> If not resolved, submit written appeal to <a href=""mailto:appeals@witchcityrope.com"">appeals@witchcityrope.com</a></li>
<li><strong>Review:</strong> Appeals reviewed by leadership team member not involved in original decision</li>
<li><strong>Response:</strong> Decision provided within 7 business days</li>
</ol>

<h3>Payment Disputes and Chargebacks</h3>
<p>If you initiate a chargeback or payment dispute with your bank/credit card company:</p>
<ul>
<li>We will respond with our cancellation policy and records of your agreement to terms</li>
<li>Please contact us directly before initiating a chargeback - we're happy to work with you</li>
<li>Chargebacks may result in suspension of account pending resolution</li>
</ul>

<h2>Contact Information</h2>

<h3>For Refund Requests</h3>
<p><strong>Email:</strong> <a href=""mailto:refunds@witchcityrope.com"">refunds@witchcityrope.com</a></p>
<p><strong>Include:</strong></p>
<ul>
<li>Your name and email used for registration</li>
<li>Event name and date</li>
<li>Registration/Order number (if available)</li>
<li>Reason for cancellation (optional but helpful)</li>
</ul>

<h3>For Registration Transfers</h3>
<p><strong>Email:</strong> <a href=""mailto:registrations@witchcityrope.com"">registrations@witchcityrope.com</a></p>

<h3>For General Questions</h3>
<p><strong>Email:</strong> <a href=""mailto:info@witchcityrope.com"">info@witchcityrope.com</a></p>

<h3>For Hardship Assistance</h3>
<p><strong>Email:</strong> <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a></p>

<hr>

<h2>Policy Updates</h2>
<p>This refund policy may be updated periodically. Changes will be posted on this page with the ""Last Updated"" date. Significant changes will be communicated via email to registered members.</p>

<h2>Acknowledgment</h2>
<p>By registering for WitchCityRope events, you acknowledge that you have read, understood, and agree to this refund policy.</p>

<hr>

<p><strong>Our Philosophy:</strong> We understand that life happens. While we must maintain a clear refund policy to sustain our operations, we also believe in supporting our community members during difficult times. If you're facing exceptional circumstances not covered by this policy, please reach out - we'll do our best to find a solution that works for everyone.</p>

<p><strong>Thank you for supporting WitchCityRope and our community.</strong></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // Event Waiver Page
                new ContentPage
                {
                    Slug = "event-waiver",
                    Title = "Event Waiver",
                    Content = @"<h1>Witch City Rope Event Waiver</h1>
<p><em>Last Updated July 7, 2022</em></p>

<h2>Event Waiver</h2>

<p>In consideration of the services of Witch City Rope (DBA), their agents, owners, officers, volunteers, participants, employees, and all other persons or entities acting in any capacity on their behalf (hereinafter collectively referred to as ""Witch City Rope""), I hereby agree to release, indemnify, and discharge Witch City Rope, on behalf of myself, my spouse, my children, my parents, my heirs, assigns, personal representative and estate as follows:</p>

<ol>
<li>
<p>I acknowledge that my participation in aerial arts, drama skills, suspension, bondage, kink, stagecraft training and instruction activities entails known and unanticipated risks that could result in physical or emotional injury, paralysis, death, or damage to myself, to property, or to third parties. I understand that such risks simply cannot be eliminated without jeopardizing the essential qualities of the activity.</p>

<p>The risks include, among other things: slips and falls; falling from equipment; burns; blisters; rope burns; strap burns; pinches, scrapes, twists and jolts that could result in scratches, bruises, sprains, lacerations, fractures, concussions, or even more severe life threatening hazards; strains, cuts, muscle soreness and fractures; musculoskeletal injuries including head, neck, and back; injuries to internal organs; the negligence of other participants or persons who may be present; my own physical condition; dehydration; nerve damage; wrist drop; failing to act safely or within one's own ability; accidents or illness can occur in remote places without medical facilities and emergency treatment or other services rendered; and the risk of emotional and psychological injuries or physical damage associated with this activity. In any event, if you are injured, any medical assistance will be at your own expense.</p>

<p>Furthermore, Witch City Rope agents have difficult jobs to perform. They seek safety, but they are not infallible. They might be unaware of a participant's fitness or abilities. They might misjudge the weather or other environmental conditions. They may give incomplete warnings or instructions, and the equipment being used might malfunction.</p>
</li>

<li>I am 21 years of age or older, and I will verify my age by showing proof of my identification to the event organizers upon entry.</li>

<li>I agree to indemnify and defend Witch City Rope, against all claims, causes of action, damages, judgments, costs or expenses, including but not limited to attorney fees, and other litigation costs, which may in any way arise from my participation and attendance at Witch City Rope.</li>

<li>I agree to pay for any and all damages I cause to the facilities during my attendance at an Witch City Rope event.</li>

<li>I have received a copy of the Member and Event Code of Conduct. I have had an opportunity to read and ask questions about these Codes of Conduct. I agree to follow these Code of Conducts, and I understand these Code of Conducts are non-negotiable and subject to change. I understand that violating any of these Code of Conducts or any terms of this waiver could result in my ejection from the event without refund, and ban from future events.</li>

<li>I am also responsible for my own property. I understand and agree that Witch City Rope will not be held responsible for lost, stolen, or damaged items that I bring to the event.</li>

<li>I agree to abide by Local, State, and Federal laws while at this event. I will not participate or condone any illegal activities while at Witch City Rope. I will not engage in illegal drug use before or during the event. I will not participate in any exchange of money for services.</li>

<li>I expressly agree and promise to accept and assume all of the risks existing in this activity. My participation in this activity is purely voluntary, and I elect to participate in spite of the risks.</li>

<li>I hereby voluntarily release, forever discharge, and agree to indemnify and hold harmless Witch City Rope from any and all claims, demands, or causes of action, which are in any way connected with my participation in this activity or my use of Witch City Rope's equipment or facilities, including any such claims which allege negligent acts or omissions of Witch City Rope.</li>

<li>Should Witch City Rope or anyone acting on their behalf, be required to incur attorney's fees and costs to enforce this agreement, I agree to indemnify and hold them harmless for all such fees and costs.</li>

<li>I certify that I have adequate insurance to cover any injury or damage I may cause or suffer while participating, or else I agree to bear the costs of such injury or damage myself. I further certify that I am willing to assume the risk of any medical or physical condition I may have.</li>

<li>I certify that I am here for my own personal enjoyment. If I am here in any official capacity as a city or law enforcement official, I will show the proper identification to the Witch City Rope representatives. I understand that Witch City Rope values and respects its local community, and abiding by the law is important to Witch City Rope and its organizers.</li>

<li>I further certify that I am not a member, or acting under the direction of any media organization, government institution, or personal interest group, nor am I here to research, investigate, report, expose or otherwise collect and use information obtained at this event. I understand that if I am, I will be removed immediately, my presence at the event will be published to the kink community, and Witch City Rope will pursue the maximum extent of relief as allowed by law against me.</li>

<li>I will respect the right to privacy of all Witch City Rope attendees. This includes not divulging legal names, unless previously sanctioned by the individual, not taking any photos, videos, and audio recordings of the event, and not divulging details of scenes. I further certify that I am in no way connected with/representing any form of media and that if I am representing such an entity in any way, I agree to pay $1 million in total to Witch City Rope agents for any breach of this release/statement. If I profit from any said reports, images, or recounts in any way, including normal payroll, I agree to pay $1 million to Witch City Rope agents for each breach.</li>

<li>Regardless of affiliation with a media entity, if I should divulge to any news/media/broadcast organization anything I see, hear, or photograph while attending this event, I acknowledge and agree that it would be impractical and extremely difficult to estimate the damages that Witch City Rope agents might incur for such disclosure. Therefore, I agree that a reasonable estimate of the total detriment that Witch City Rope agents would suffer in the event of such disclosure is and shall be, as the sole and exclusive remedy and not as a penalty, One Million Dollars ($1,000,000) (""Liquidated Damages Amount""). I recognize I shall be held separately liable to Witch City Rope agents for each instance of such broadcast or display over the news/media/broadcast/internet outlet for no less than the specified Liquidated Damages Amount.</li>

<li>I agree that any such recordings/pictures I create or take during an Witch City Rope event become copyright of Witch City Rope agents and their operators and any broadcast, posting, or reproduction of these will violate U.S. Copyright Protection Act unless specifically authorized by the event holders.</li>

<li>I certify that any images I record will not be used in any way for commercial or personal gain nor will they be used for the purpose of entrapment or to gather information and/or testimony that would lead to the harm, persecution or prosecution of the organizers of Witch City Rope, the owner of the event site, or any individual attending or connected with the event. I agree to hold harmless the organizers of Witch City Rope, the owners of the event site, and attendees of the event if my image is captured and distributed inadvertently.</li>

<li>Witch City Rope events are social events for rope bondage enthusiasts to gather to practice, teach, learn, demonstrate, discuss, and perform various styles and expressions of rope bondage with other consenting adults. I understand I am under no obligations to participate in demonstrations or interact with other scenes. I also understand that content shown or discussed at this event may be sensitive or triggering subjects including but not limited to rope bondage, sexuality, domination and submission, consensual power exchange, sadomasochism, and more.</li>

<li>I am responsible for clearly communicating my personal preferences and limits to others with whom I interact. I am responsible to ensure I participate at a level that does not exceed my personal limits and I am responsible for removing myself should any activity exceed my limits. I understand that should I violate any of the above agreements, I may be asked to leave the event.</li>

<li>I understand that I am attending an event that is adult in nature. In doing so, I may be exposed to adult related activities between consenting adults that some people might find offensive or immoral. I do not find these activities offensive or immoral and I consent to being exposed to such activities. With my participation in this event, I willingly and with full consent and knowledge waive and relinquish any type of claim as to the offensive nature of anything I might see or experience while attending a Witch City Rope event. I take personal responsibility that if I do find something offensive, I will leave the area without disruption or comment.</li>

<li>Should any legal issue arise, I agree to pay Witch City Rope's legal fees should I make a legal claim. I also agree to consent to pre-filing mediation and court ordered mediation should any claim arise. If I do not participate in pre-filing mediation, I understand my claim may be dismissed for violating the terms of this waiver.</li>

<li>I agree that if any portion of this agreement is found to be void or unenforceable, the remaining document shall remain in full force and effect.</li>
</ol>

<p>By signing this document, I acknowledge that if anyone is hurt or property is damaged during my participation in this activity, I may be found by a court of law to have waived my right to maintain a lawsuit against Witch City Rope on the basis of any claim from which I have released them herein.</p>

<p>I have had sufficient opportunity to read this entire document. I have read and understood it, and I agree to be bound by its terms.</p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // Incident Reporting Policy & Procedures
                new ContentPage
                {
                    Slug = "incident-reporting-policy",
                    Title = "Incident Reporting Policy & Procedures",
                    Content = @"<h1>Incident Reporting Policy & Procedures</h1>

<p><strong>Last Updated:</strong> December 2024</p>

<p>WitchCityRope is committed to maintaining a safe, respectful community. Our incident reporting system allows community members to report safety concerns, consent violations, code of conduct breaches, and other issues. This document explains how we handle reports and what you can expect from the process.</p>

<h2>Purpose of Incident Reporting</h2>

<p>Our incident reporting system exists to:</p>
<ul>
<li>Document safety concerns and patterns of behavior</li>
<li>Enable the safety team to investigate potential issues</li>
<li>Protect community members from harm</li>
<li>Maintain accountability within our community</li>
<li>Inform decisions about membership and event access</li>
</ul>

<h2>How Reports Are Handled</h2>

<h3>Receipt and Initial Review</h3>
<p>When you submit a report:</p>
<ul>
<li>Your report is encrypted and stored securely</li>
<li>Only designated safety team members can access report details</li>
<li>You will receive a reference number for tracking (if you provide contact information)</li>
<li>The safety team will review your report within 48-72 hours for non-emergencies</li>
</ul>

<h3>Investigation Process</h3>
<p>Depending on the nature of the report, the safety team may:</p>
<ul>
<li>Document the incident for records without further action</li>
<li>Reach out to you for additional information (if contact was provided)</li>
<li>Speak with witnesses or other involved parties</li>
<li>Review relevant event records or prior reports</li>
<li>Consult with event organizers or leadership</li>
</ul>

<h3>Possible Outcomes</h3>
<p>Based on investigation findings, possible outcomes include:</p>
<ul>
<li>Documentation only (no further action at this time)</li>
<li>Informal conversation with involved parties</li>
<li>Formal warning</li>
<li>Mandatory education or training requirements</li>
<li>Temporary suspension from events</li>
<li>Permanent removal from the community</li>
<li>Referral to law enforcement (in cases involving illegal activity)</li>
</ul>

<h2>What We Cannot Guarantee</h2>

<p>While we take every report seriously, please understand:</p>
<ul>
<li><strong>No guaranteed outcome:</strong> We cannot promise any specific action will be taken as a result of your report</li>
<li><strong>Investigation limitations:</strong> Some situations cannot be fully investigated due to lack of evidence, conflicting accounts, or other factors</li>
<li><strong>Confidentiality limits:</strong> In some cases, we may need to share limited information to conduct a proper investigation or protect community safety</li>
<li><strong>Not a substitute for legal action:</strong> Our process is separate from law enforcement; we encourage you to contact authorities for criminal matters</li>
</ul>

<h2>Your Rights as a Reporter</h2>

<ul>
<li><strong>Anonymous reporting:</strong> You may submit reports anonymously. Anonymous reports are taken seriously, though they may limit our ability to follow up or investigate fully.</li>
<li><strong>Confidentiality preferences:</strong> You can specify whether you wish to remain anonymous during the investigation and/or in any final documentation.</li>
<li><strong>No retaliation:</strong> WitchCityRope prohibits retaliation against anyone who makes a good-faith report. Retaliation is itself a violation of our code of conduct.</li>
<li><strong>Updates (if identified):</strong> If you provide contact information and request follow-up, we will keep you informed of general progress where appropriate.</li>
</ul>

<h2>How We Use Your Information</h2>

<p>Information from your report will be used to:</p>
<ul>
<li>Assess and investigate the reported incident</li>
<li>Document patterns of behavior across multiple reports</li>
<li>Make decisions about community membership and event access</li>
<li>Improve our safety practices and policies</li>
</ul>

<p>Your report information is governed by our <a href=""/cms/privacy-policy"">Privacy Policy</a>. Report data is retained according to our data retention policies and may be referenced in future investigations if relevant.</p>

<h2>Emergency Situations</h2>

<p>This reporting system is <strong>not for emergencies</strong>. If you or someone else is in immediate danger:</p>
<ul>
<li><strong>At an event:</strong> Alert event staff or safety monitors immediately</li>
<li><strong>Medical emergency:</strong> Call 911</li>
<li><strong>Immediate threat:</strong> Remove yourself from the situation and contact authorities</li>
</ul>

<h2>Questions</h2>

<p>If you have questions about this policy or the reporting process, contact us at <a href=""mailto:safety@witchcityrope.com"">safety@witchcityrope.com</a>.</p>

<hr>

<p><em>By submitting an incident report, you acknowledge that you have read and understood this policy.</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                },

                // Vetting Interview Scheduling
                //
                // Linked to from the dashboard VettingAlertBox (and the /join page
                // VettingAlertBox) for users whose vetting status is InterviewApproved.
                // The backend sets VettingStatusDto.InterviewScheduleUrl to this slug
                // in UserDashboardProfileService — see that file's InterviewApproved
                // case for the link logic.
                //
                // This seeder entry exists so every environment (dev, staging, prod)
                // has a landing page at /vetting-interview-scheduling on first seed.
                // The content below is a reasonable default; admins can edit the real
                // copy via the admin CMS UI without affecting this seeder.
                //
                // Idempotency: the seeder at the bottom of SeedInitialPagesAsync skips
                // any slug that already exists, so rerunning will not overwrite manual
                // CMS edits made in any environment.
                new ContentPage
                {
                    Slug = "vetting-interview-scheduling",
                    Title = "Schedule Your Vetting Interview",
                    Content = @"<h1>Schedule Your Vetting Interview</h1>

<p>Congratulations! Your membership application has been approved to move forward. The next step is scheduling your vetting interview with a member of our team.</p>

<h2>What to Expect</h2>

<p>The vetting interview is a friendly, informal conversation — typically 20 to 30 minutes — where you'll meet with a member of our vetting team to:</p>
<ul>
<li>Talk about your interest in the rope bondage community</li>
<li>Review our community standards and expectations</li>
<li>Answer any questions you have about the group, our events, and how things work</li>
<li>Make sure you feel comfortable and informed before becoming a full member</li>
</ul>

<p>There are no trick questions, and there is nothing to prepare. Just come as yourself.</p>

<h2>How to Book</h2>

<p>To schedule your interview, please email <a href=""mailto:vetting@witchcityrope.com"">vetting@witchcityrope.com</a> with a few dates and times that work for you over the next two weeks. A vetting team member will respond to confirm a slot.</p>

<p>If you have any questions before your interview, you can also reach out at the same address.</p>

<h2>After the Interview</h2>

<p>Once your interview is complete, our team will review the conversation and you will receive an email with the outcome within a few days. If approved, you'll gain full access to community events and resources.</p>

<p><em>We look forward to meeting you!</em></p>",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = adminUser.Id,
                    LastModifiedBy = adminUser.Id,
                    IsPublished = true
                }
            };

            // Add only pages that don't already exist (idempotent seeding)
            var existingSlugs = await context.ContentPages
                .Select(p => p.Slug)
                .ToListAsync();

            var pagesToAdd = initialPages
                .Where(p => !existingSlugs.Contains(p.Slug))
                .ToList();

            if (pagesToAdd.Any())
            {
                await context.ContentPages.AddRangeAsync(pagesToAdd);
                await context.SaveChangesAsync();
            }
        }
    }
}
