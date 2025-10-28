using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of sample events for development and testing.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating 8 diverse event records (6 upcoming, 2 past).
/// </summary>
public class EventSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventSeeder> _logger;

    // Historical event IDs for cross-seeder references
    private Guid _advancedSuspensionEventId;
    private Guid _ropeFundamentalsEventId;
    private Guid _practiceNightEventId;
    private Guid _welcomeMixerEventId;

    public EventSeeder(
        ApplicationDbContext context,
        ILogger<EventSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Public accessors for historical event IDs (used by other seeders)
    public Guid AdvancedSuspensionEventId => _advancedSuspensionEventId;
    public Guid RopeFundamentalsEventId => _ropeFundamentalsEventId;
    public Guid PracticeNightEventId => _practiceNightEventId;
    public Guid WelcomeMixerEventId => _welcomeMixerEventId;

    /// <summary>
    /// Seeds 8 sample events for testing event management functionality.
    /// Idempotent operation - skips if events already exist.
    ///
    /// Creates diverse events:
    /// - 3 upcoming class events (Introduction to Rope Safety, Suspension Basics, Advanced Floor Work)
    /// - 3 upcoming social events (Community Rope Jam, Rope Social & Discussion, New Members Meetup)
    /// - 1 past social event (Beginner Rope Circle)
    /// - 1 past class event (Rope Fundamentals Series)
    ///
    /// Each event includes:
    /// - Complete title, descriptions (short and long HTML), policies (HTML)
    /// - Proper UTC DateTime handling
    /// - Appropriate capacity, location, pricing
    /// - Published status
    ///
    /// Note: Sessions and ticket types are created by SessionTicketSeeder (separate seeder).
    /// </summary>
    public async Task SeedEventsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sample event creation");

        // Check if events already exist (idempotent operation)
        var existingEventCount = await _context.Events.CountAsync(cancellationToken);
        if (existingEventCount > 0)
        {
            _logger.LogInformation("Events already exist ({Count}), skipping event seeding", existingEventCount);
            return;
        }

        // Create diverse set of events per functional specification
        var sampleEvents = new[]
        {
            // Upcoming Events (3 classes and 3 social events)
            // Updated dates to ensure events are in the future (relative to current date)
            CreateSeedEvent(
                title: "Introduction to Rope Safety",
                daysFromNow: 7,  // Next week
                startHour: 18,
                capacity: 20,
                eventType: EventType.Class,
                price: 25.00m,
                shortDescription: "Learn the fundamentals of safe rope bondage practices in this comprehensive beginner workshop.",
                longDescription: @"<p>This comprehensive introduction to rope safety is designed for absolute beginners and those looking to refresh their fundamental knowledge.</p>

<h3>In this 3-hour workshop, you'll learn</h3>
<ul>
<li>Essential safety principles and risk awareness for rope bondage</li>
<li>Communication techniques including consent negotiation and safe words</li>
<li>Basic rope handling skills and material selection (jute, hemp, synthetic)</li>
<li>Recognition of nerve compression and circulation issues</li>
<li>Emergency release procedures and safety protocols</li>
<li>How to create a safe practice environment</li>
</ul>

<h3>Prerequisites</h3>
<p>None - this is a beginner-friendly class designed for people with no prior rope experience.</p>

<h3>Materials</h3>
<ul>
<li>Two 30-foot lengths of 6mm rope will be provided (jute or hemp)</li>
<li>You may bring your own rope if preferred</li>
<li>Comfortable clothing that allows movement is required</li>
</ul>

<h3>Class Structure</h3>
<ul>
<li>45 minutes: Safety theory and communication fundamentals</li>
<li>90 minutes: Hands-on practice with instructor guidance</li>
<li>30 minutes: Q&A and resource sharing</li>
<li>15 minutes: Community discussion and next steps</li>
</ul>

<p>This class emphasizes building a strong foundation in safety practices that will serve you throughout your rope journey. We focus on understanding risks, developing good habits, and creating positive experiences for all participants.</p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>Advance registration and payment required (no walk-ins)</li>
<li>Arrive 15 minutes early for check-in and orientation</li>
</ul>

<h3>Consent and Communication</h3>
<ul>
<li>Enthusiastic consent is required for all activities and interactions</li>
<li>Respect all safe words, boundaries, and limits without question</li>
<li>""No"" means no - immediately stop any activity when requested</li>
<li>Ask before touching others or their belongings</li>
</ul>

<h3>Photography and Recording</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from all participants</li>
<li>Cell phones must be on silent and stored during class</li>
<li>Violations will result in immediate removal without refund</li>
</ul>

<h3>Appropriate Conduct and Attire</h3>
<ul>
<li>Professional and respectful behavior expected at all times</li>
<li>Wear comfortable, non-restrictive clothing suitable for movement</li>
<li>Remove jewelry that may interfere with rope work</li>
<li>Closed-toe shoes recommended for safety</li>
</ul>

<h3>Health and Safety</h3>
<ul>
<li>Inform instructor of any medical conditions, injuries, or mobility limitations</li>
<li>Practice good hygiene - shower before attending</li>
<li>Do not attend if you are ill or contagious</li>
<li>Report any injuries or safety concerns immediately to staff</li>
</ul>

<h3>Scent-Free Environment</h3>
<ul>
<li>Please avoid strong perfumes, colognes, or scented products</li>
<li>Some community members have chemical sensitivities</li>
</ul>

<h3>Zero Tolerance Policies</h3>
<ul>
<li>No harassment, discrimination, or predatory behavior of any kind</li>
<li>No drugs or alcohol on premises</li>
<li>Violation of policies may result in permanent ban from all community events</li>
</ul>

<p><strong>By attending, you agree to abide by all policies and accept responsibility for your own safety and well-being.</strong></p>"
            ),

            CreateSeedEvent(
                title: "Suspension Basics",
                daysFromNow: 14,  // 2 weeks from now
                startHour: 18,
                capacity: 12,
                eventType: EventType.Class,
                price: 65.00m,
                shortDescription: "Introduction to suspension techniques with emphasis on safety and proper rigging.",
                longDescription: @"<p>Take your rope skills to the next level with this comprehensive introduction to suspension bondage. This intermediate-level workshop covers the essential techniques, safety considerations, and rigging fundamentals needed to begin exploring suspension safely.</p>

<h3>What You'll Learn</h3>
<ul>
<li>Suspension safety principles and risk mitigation strategies</li>
<li>Proper rigging setup and equipment requirements (hard points, rigging plates, carabiners)</li>
<li>Load-bearing tie techniques and weight distribution</li>
<li>Hip harness construction with emphasis on safety and comfort</li>
<li>Partial suspension techniques for beginners</li>
<li>How to assess and monitor your partner during suspension</li>
<li>Safe descent and emergency procedures</li>
<li>Common mistakes and how to avoid them</li>
</ul>

<h3>Prerequisites</h3>
<ul>
<li><strong>REQUIRED:</strong> Strong foundation in floor bondage techniques</li>
<li><strong>REQUIRED:</strong> Completion of 'Introduction to Rope Safety' or equivalent</li>
<li><strong>Recommended:</strong> 6+ months of regular rope practice</li>
<li>You must be comfortable tying basic harnesses and understand rope safety fundamentals</li>
</ul>

<h3>Equipment Provided</h3>
<ul>
<li>Suspension rigging equipment (hard points, carabiners, etc.)</li>
<li>Practice rope if needed</li>
</ul>

<h3>What to Bring</h3>
<ul>
<li>4-6 lengths of rope (30 feet each, 6mm diameter)</li>
<li>Comfortable, form-fitting clothing (leggings, fitted shirt)</li>
<li>Water bottle and snacks</li>
<li>Notebook for taking notes (optional)</li>
</ul>

<h3>Class Format</h3>
<ul>
<li>30 minutes: Suspension theory, safety review, and equipment overview</li>
<li>2 hours: Hands-on practice with instructor supervision</li>
<li>30 minutes: Q&A, troubleshooting, and safety discussion</li>
</ul>

<p><strong>Safety Note:</strong> Suspension carries inherent risks including nerve damage, circulation issues, and injury from falls. This class emphasizes conservative, safety-first approaches suitable for beginners. You will learn to recognize warning signs and respond appropriately.</p>

<p><em>Limited to 12 participants to ensure personalized instruction and adequate safety supervision.</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>Advance registration and payment required (no walk-ins)</li>
<li>Prerequisite: Completion of rope safety fundamentals course or instructor approval</li>
<li>Arrive 15 minutes early for safety briefing and equipment setup</li>
</ul>

<h3>Medical and Physical Requirements</h3>
<ul>
<li>You must disclose any medical conditions, injuries, or physical limitations to instructor before class</li>
<li>Certain conditions may prevent safe participation in suspension activities</li>
<li>You are responsible for knowing your own health status and limitations</li>
<li>Sign medical waiver acknowledging suspension risks before participating</li>
</ul>

<h3>Consent and Communication</h3>
<ul>
<li>Enthusiastic consent required for all activities and physical contact</li>
<li>Active communication expected throughout suspension scenes</li>
<li>Immediate stop on any safe word or request to stop</li>
<li>Check-ins required at regular intervals during suspension</li>
</ul>

<h3>Equipment and Safety</h3>
<ul>
<li>Do not touch or adjust rigging equipment without instructor permission</li>
<li>Inspect all equipment before use and report any damage or concerns</li>
<li>Follow all instructor directions regarding equipment usage</li>
<li>Spotter required for all suspension activities</li>
</ul>

<h3>Photography and Recording</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from all participants and instructor</li>
<li>Cell phones must be on silent and stored during class</li>
</ul>

<h3>Appropriate Conduct</h3>
<ul>
<li>Professional behavior required at all times</li>
<li>Wear appropriate athletic/practice clothing (no loose garments)</li>
<li>Remove all jewelry before participating in suspension</li>
<li>Notify instructor immediately of any discomfort, numbness, tingling, or other concerns</li>
</ul>

<h3>Zero Tolerance</h3>
<ul>
<li>No harassment, discrimination, or boundary violations</li>
<li>No drugs or alcohol - immediate removal and ban</li>
<li>Suspension is an advanced skill requiring focus and sobriety</li>
</ul>

<h3>Risk Acknowledgment</h3>
<ul>
<li>Suspension bondage carries inherent risks including nerve damage, circulation issues, rope burns, bruising, and injury from falls</li>
<li>By attending, you acknowledge these risks and accept responsibility for your participation</li>
<li>You agree to follow all safety protocols and instructor guidance</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Advanced Floor Work",
                daysFromNow: 21,  // 3 weeks from now
                startHour: 18,
                capacity: 10,
                eventType: EventType.Class,
                price: 55.00m,
                shortDescription: "Explore complex floor-based rope bondage techniques for experienced practitioners.",
                longDescription: @"<p>This advanced workshop is designed for experienced rope practitioners looking to expand their floor bondage repertoire with complex ties, creative positions, and artistic expression. We'll explore sophisticated techniques that challenge both rigger and model while maintaining safety and comfort.</p>

<h3>Workshop Topics</h3>
<ul>
<li>Complex multi-point ties and connection techniques</li>
<li>Asymmetrical and artistic floor positions</li>
<li>Creative use of space and body positioning</li>
<li>Transitions between positions while maintaining tie integrity</li>
<li>Strappado, hogtie, and other challenging positions</li>
<li>Decorative elements and aesthetic considerations</li>
<li>Managing longer scenes and endurance considerations</li>
<li>Troubleshooting common issues with complex ties</li>
</ul>

<h3>Prerequisites</h3>
<ul>
<li><strong>REQUIRED:</strong> Extensive floor bondage experience (1+ years regular practice)</li>
<li><strong>REQUIRED:</strong> Strong foundation in safety, communication, and basic ties</li>
<li>Must be proficient in single column, double column, chest harnesses, and hip harnesses</li>
<li>Experience with multi-limb ties and transitions</li>
<li><strong>This is NOT a beginner class</strong></li>
</ul>

<h3>What to Bring</h3>
<ul>
<li>6-8 lengths of rope (30 feet each, 6mm diameter) - quality rope recommended</li>
<li>Comfortable form-fitting practice clothing</li>
<li>Knee pads optional but recommended</li>
<li>Water and snacks</li>
<li>Notebook for notes and sketches</li>
</ul>

<h3>Class Structure</h3>
<ul>
<li>20 minutes: Demonstration of key concepts and techniques</li>
<li>2 hours: Hands-on practice with instructor feedback</li>
<li>20 minutes: Sharing, feedback, and Q&A</li>
</ul>

<h3>Teaching Philosophy</h3>
<p>This class emphasizes creative exploration within safe parameters. We'll focus on developing your personal style while respecting the fundamentals that keep everyone safe. Expect to be challenged and to learn from both successes and mistakes.</p>

<p><em>Small class size (limited to 10 participants) ensures individualized attention and the ability to work at your own pace.</em></p>

<p><strong>Note:</strong> Partners/models welcome but not required. Solo practitioners can focus on learning tie techniques and theory.</p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>Advance registration and payment required</li>
<li>Prerequisite: Extensive rope experience and instructor approval</li>
<li>Late arrival may result in denied entry for safety reasons</li>
</ul>

<h3>Experience Verification</h3>
<ul>
<li>This is an advanced class - beginners will not be admitted</li>
<li>Instructor reserves right to assess skill level and decline participation</li>
<li>If your experience level is uncertain, contact instructor before registering</li>
</ul>

<h3>Safety and Communication</h3>
<ul>
<li>Expert-level communication and consent skills expected</li>
<li>Proactive monitoring of partner required throughout scenes</li>
<li>Immediate stop on any safe word, numbness, tingling, or discomfort</li>
<li>Report all safety concerns to instructor immediately</li>
</ul>

<h3>Equipment Requirements</h3>
<ul>
<li>Bring your own quality rope in good condition</li>
<li>Inspect rope before use and remove any damaged rope from rotation</li>
<li>Recommended: natural fiber rope (jute/hemp) for better grip and control</li>
</ul>

<h3>Photography and Recording</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from all participants</li>
<li>This is a closed, private workshop</li>
<li>Cell phones must be stored during practice sessions</li>
</ul>

<h3>Professional Conduct</h3>
<ul>
<li>Respectful, professional behavior required at all times</li>
<li>Appropriate practice attire (form-fitting, allows movement)</li>
<li>Practice good hygiene</li>
<li>No unsolicited advice or critique of other participants</li>
</ul>

<h3>Health and Disclosure</h3>
<ul>
<li>Disclose relevant medical conditions, injuries, or limitations</li>
<li>You are responsible for your own physical and mental well-being</li>
<li>Take breaks as needed</li>
<li>Stay hydrated</li>
</ul>

<h3>Zero Tolerance Policies</h3>
<ul>
<li>No harassment, boundary violations, or predatory behavior</li>
<li>No drugs or alcohol</li>
<li>Violations result in immediate removal and permanent ban</li>
</ul>

<h3>Assumption of Risk</h3>
<ul>
<li>Advanced rope work carries increased risk of injury</li>
<li>By participating, you acknowledge these risks and accept responsibility</li>
<li>You agree to practice safely and follow all instructor guidance</li>
<li>Nerve damage, bruising, rope burns, and other injuries are possible</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Community Rope Jam",
                daysFromNow: 28,  // 4 weeks from now
                startHour: 19,
                capacity: 40,
                eventType: EventType.Social,
                price: 15.00m,
                shortDescription: "Casual practice session for all skill levels. Bring your rope and practice with the community.",
                longDescription: @"<p>Join us for our monthly Community Rope Jam - a relaxed, social practice session where rope enthusiasts of all skill levels can come together to practice, learn, and connect with fellow community members.</p>

<h3>What to Expect</h3>
<ul>
<li>Open practice space with supportive atmosphere</li>
<li>Practice your existing skills or try new techniques</li>
<li>Ask questions and learn from experienced practitioners</li>
<li>Make new friends and connections in the community</li>
<li>Casual, judgment-free environment</li>
</ul>

<h3>Who Should Attend</h3>
<ul>
<li><strong>ALL SKILL LEVELS</strong> welcome from absolute beginners to advanced practitioners</li>
<li>Solo practitioners welcome - you don't need to bring a partner</li>
<li>People interested in learning more about rope bondage</li>
<li>Experienced rope artists looking to practice and share knowledge</li>
<li>Anyone seeking community connection</li>
</ul>

<h3>Facilitators Present</h3>
<ul>
<li>Experienced community members available to answer questions</li>
<li>Safety monitors on site</li>
<li>Not a formal class, but informal guidance available</li>
</ul>

<h3>What to Bring</h3>
<ul>
<li>Your own rope (if you have it) - we'll have some available to borrow</li>
<li>Comfortable clothing suitable for movement</li>
<li>Water bottle</li>
<li>Positive attitude and respect for all participants</li>
</ul>

<h3>Activities</h3>
<ul>
<li>Self-directed practice at your own pace</li>
<li>Informal peer learning and knowledge sharing</li>
<li>Socializing and community building</li>
<li>Optional: Share what you're working on with supportive feedback</li>
</ul>

<h3>Perfect For</h3>
<ul>
<li>Practicing ties you've learned in classes</li>
<li>Experimenting with new ideas in a safe environment</li>
<li>Meeting practice partners</li>
<li>Building confidence in your rope skills</li>
<li>Connecting with the local rope community</li>
</ul>

<p>No pressure, no judgment - just a welcoming space to explore rope at your own comfort level. Whether you tie for 5 minutes or the whole session, you're welcome here.</p>

<p><em>Optional donation-based entry - pay what you can to help cover venue costs.</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>RSVP required for space planning</li>
<li>Sliding scale pricing ($5-15) - pay what you can</li>
<li>All skill levels welcome</li>
</ul>

<h3>Safety First</h3>
<ul>
<li>Practice safely within your skill level</li>
<li>Do not attempt techniques beyond your current abilities</li>
<li>Ask for help if you're unsure about safety</li>
<li>Safety monitors present to assist with concerns</li>
</ul>

<h3>Consent and Boundaries</h3>
<ul>
<li>Enthusiastic consent required for all interactions</li>
<li>Ask before approaching others or touching their belongings</li>
<li>Respect everyone's boundaries and personal space</li>
<li>""No"" means no - respect it immediately</li>
</ul>

<h3>Photography and Recording</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit written permission from ALL people who might appear in the shot</li>
<li>Ask permission EACH time before taking photos</li>
<li>Respect others' privacy and anonymity</li>
</ul>

<h3>Respectful Environment</h3>
<ul>
<li>Treat all participants with respect regardless of experience level</li>
<li>No unsolicited critique or advice</li>
<li>Keep coaching to your own partner unless specifically invited to help</li>
<li>Welcoming, judgment-free atmosphere</li>
</ul>

<h3>Appropriate Conduct</h3>
<ul>
<li>Professional behavior expected</li>
<li>Comfortable, appropriate clothing for rope practice</li>
<li>Practice good hygiene</li>
<li>No drugs or alcohol on premises</li>
</ul>

<h3>Space Sharing</h3>
<ul>
<li>Be mindful of shared space - keep practice areas compact</li>
<li>Clean up your area when finished</li>
<li>Return borrowed equipment to proper location</li>
<li>Help maintain welcoming environment for all</li>
</ul>

<h3>Bring Your Own Equipment</h3>
<ul>
<li>Bring your own rope if possible (limited rope available to borrow)</li>
<li>Bring water and any personal items needed</li>
<li>Label your belongings</li>
</ul>

<h3>Zero Tolerance</h3>
<ul>
<li>No harassment, discrimination, or predatory behavior</li>
<li>Violations result in immediate removal and permanent ban</li>
<li>Community safety is our top priority</li>
</ul>

<h3>Practice at Your Own Risk</h3>
<ul>
<li>This is not a supervised class</li>
<li>You are responsible for your own safety</li>
<li>Practice within your abilities and knowledge</li>
<li>Ask for help when needed</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Rope Social & Discussion",
                daysFromNow: 35,  // 5 weeks from now
                startHour: 19,
                capacity: 30,
                eventType: EventType.Social,
                price: 10.00m,
                shortDescription: "Monthly social gathering for community connection and discussion of rope topics.",
                longDescription: @"<p>Join us for our monthly Rope Social & Discussion - a casual gathering focused on community building, conversation, and connection. This is a chance to meet fellow rope enthusiasts, discuss topics of interest, and strengthen our community bonds.</p>

<h3>Event Format</h3>
<ul>
<li>Social hour: Mingle and meet community members</li>
<li>Discussion circle: Structured conversation on a monthly topic</li>
<li>Q&A and open discussion</li>
<li>Resource sharing and announcements</li>
</ul>

<h3>Monthly Discussion Topics (rotating)</h3>
<ul>
<li>Rope philosophy and personal practice</li>
<li>Navigating consent and communication</li>
<li>Building a sustainable rope practice</li>
<li>Rope community culture and values</li>
<li>Safety deep dives on specific topics</li>
<li>Artistic expression through rope</li>
<li>And more based on community interest</li>
</ul>

<h3>Who Should Attend</h3>
<ul>
<li>Anyone interested in rope bondage at any skill level</li>
<li>People curious about the rope community</li>
<li>Experienced practitioners looking to connect</li>
<li>Those seeking a thoughtful, discussion-oriented space</li>
<li>Community members interested in building connections</li>
</ul>

<h3>What Makes This Different</h3>
<ul>
<li><strong>TALKING</strong> focused, not practice focused</li>
<li>Emphasis on community and connection over techniques</li>
<li>Safe space to ask questions and share experiences</li>
<li>Opportunity to learn from others' perspectives</li>
<li>Building friendships and support networks</li>
</ul>

<h3>Topics Covered (examples from past months)</h3>
<ul>
<li>How do you maintain work-life-rope balance?</li>
<li>What does ethical rope practice mean to you?</li>
<li>How do you handle rope injuries and recovery?</li>
<li>Building diverse and inclusive community spaces</li>
<li>Your questions and topics welcome!</li>
</ul>

<h3>Atmosphere</h3>
<ul>
<li>Relaxed and welcoming</li>
<li>Judgment-free zone for all questions</li>
<li>Respectful discussion and active listening</li>
<li>Building understanding across experience levels</li>
</ul>

<p><strong>No rope practice at this event</strong> - this is purely social and discussion-based. Bring your curiosity, questions, and openness to connect with others.</p>

<p><em>Light refreshments provided. Optional sliding scale donation to support community programming.</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>RSVP requested for space planning and refreshment prep</li>
<li>Sliding scale pricing ($5-10) - pay what you can</li>
<li>All backgrounds and experience levels welcome</li>
</ul>

<h3>Respectful Discussion</h3>
<ul>
<li>This is a conversation-based event, not a practice session</li>
<li>Respect all perspectives and experiences shared</li>
<li>No interrupting or talking over others</li>
<li>Active listening encouraged</li>
<li>Disagree respectfully if you have different views</li>
</ul>

<h3>Confidentiality</h3>
<ul>
<li>What's shared in the discussion circle stays in the discussion circle</li>
<li>Don't repeat personal stories or identifying information shared by others</li>
<li>Respect privacy and anonymity of all participants</li>
</ul>

<h3>Safe Space Guidelines</h3>
<ul>
<li>No judgment for questions asked or experiences shared</li>
<li>Create welcoming environment for all experience levels</li>
<li>Step up/step back - make room for quieter voices</li>
<li>Assume good intent while allowing for mistakes and learning</li>
</ul>

<h3>Photography and Recording</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit permission from ALL participants</li>
<li>This is a private gathering</li>
<li>Respect attendees' privacy and anonymity</li>
</ul>

<h3>Appropriate Conduct</h3>
<ul>
<li>Professional, respectful behavior required</li>
<li>No recruitment for personal relationships or services</li>
<li>No sales pitches or self-promotion</li>
<li>Focus on community building and authentic connection</li>
</ul>

<h3>Accessibility</h3>
<ul>
<li>We strive to create accessible, welcoming space</li>
<li>Let organizers know if you have accessibility needs</li>
<li>Gender-neutral restrooms available</li>
<li>Scent-free preferred (some members have sensitivities)</li>
</ul>

<h3>Food and Beverages</h3>
<ul>
<li>Light refreshments provided</li>
<li>Let us know of any allergies or dietary restrictions</li>
<li>Clean up after yourself</li>
<li>No alcohol served</li>
</ul>

<h3>Zero Tolerance</h3>
<ul>
<li>No harassment, discrimination, or boundary violations</li>
<li>No drugs or alcohol</li>
<li>Violations result in removal and ban from community events</li>
</ul>

<h3>Community Care</h3>
<ul>
<li>Look out for each other</li>
<li>Let organizers know if someone seems in distress</li>
<li>This is a community space - we all contribute to making it safe and welcoming</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "New Members Meetup",
                daysFromNow: 42,  // 6 weeks from now
                startHour: 18,
                capacity: 25,
                eventType: EventType.Social,
                price: 5.00m,
                shortDescription: "Welcome gathering for new community members to meet established practitioners and learn about upcoming events.",
                longDescription: @"<p>Welcome to the WitchCityRope community! This special meetup is designed specifically for new members to get oriented, meet friendly faces, and learn about all the opportunities our community offers.</p>

<h3>Event Overview</h3>
<p>This casual, welcoming gathering helps new members feel comfortable and connected from day one. Whether you're brand new to rope or experienced but new to our community, this event is your gateway to getting involved.</p>

<h3>What We'll Cover</h3>
<ul>
<li>Welcome and community overview</li>
<li>Introduction to our values, culture, and code of conduct</li>
<li>Overview of upcoming classes, events, and programming</li>
<li>How to get the most out of your membership</li>
<li>Q&A about the community, practices, and events</li>
<li>Meet established community members who can offer guidance</li>
<li>Connect with other new members</li>
</ul>

<h3>Who Should Attend</h3>
<ul>
<li>New community members (joined within last 3 months)</li>
<li>People considering membership and want to learn more</li>
<li>Anyone who feels new and wants to build connections</li>
<li>Established members welcome to attend as greeters/mentors</li>
</ul>

<h3>What to Expect</h3>
<ul>
<li>Friendly, welcoming atmosphere designed to reduce ""new person"" anxiety</li>
<li>Structured introductions to break the ice</li>
<li>Small group discussions to meet others</li>
<li>Resource sharing (calendars, communication channels, etc.)</li>
<li>Opportunity to ask all your questions in a supportive environment</li>
</ul>

<h3>You'll Learn About</h3>
<ul>
<li>Different types of events (classes, practice jams, socials, performances)</li>
<li>How to register for events and manage your membership</li>
<li>Community communication channels and staying connected</li>
<li>Finding practice partners and building connections</li>
<li>Volunteer opportunities and community involvement</li>
<li>Vetting process and member-only events</li>
<li>Resources for continued learning</li>
</ul>

<h3>Meet Our Community</h3>
<ul>
<li>Greeters assigned to help new members feel welcome</li>
<li>Experienced members available to answer questions</li>
<li>Other new members to connect with</li>
<li>Leadership and event organizers</li>
</ul>

<h3>No Rope Required</h3>
<p>This is a social and informational event - no rope practice or experience necessary. Just bring yourself and your curiosity!</p>

<p><em>Light refreshments provided. Suggested $5 donation (free for those experiencing financial hardship).</em></p>",
                policies: @"<h2>Event Policies and Safety Guidelines</h2>

<h3>Attendance Requirements</h3>
<ul>
<li>All participants must be 18+ years old with valid government-issued photo ID</li>
<li>RSVP requested for name tags and refreshment planning</li>
<li>Sliding scale pricing ($0-5) - pay what you can</li>
<li>Both prospective and current members welcome</li>
</ul>

<h3>Welcoming Environment</h3>
<ul>
<li>This is explicitly a beginner-friendly, judgment-free space</li>
<li>All questions are welcome and encouraged</li>
<li>No experience with rope bondage required</li>
<li>Focus on building community connections</li>
</ul>

<h3>Confidentiality and Privacy</h3>
<ul>
<li>Respect privacy of all attendees</li>
<li>Don't share identifying information about who attended</li>
<li>What's discussed stays within the community</li>
<li>Use scene names if preferred for anonymity</li>
</ul>

<h3>Respectful Interaction</h3>
<ul>
<li>Treat all attendees with respect and kindness</li>
<li>No recruitment for personal relationships</li>
<li>Professional boundaries maintained</li>
<li>This is about community building, not dating</li>
</ul>

<h3>Getting to Know You</h3>
<ul>
<li>We'll do brief introductions (optional to share or pass)</li>
<li>Share whatever you're comfortable with</li>
<li>Use scene name or first name only if preferred</li>
<li>No pressure to share personal details</li>
</ul>

<h3>Photography and Recording</h3>
<ul>
<li>Absolutely no photography, video, or audio recording without explicit permission from ALL participants</li>
<li>Name tags available but optional</li>
<li>Respect wishes of those who prefer anonymity</li>
</ul>

<h3>Accessibility</h3>
<ul>
<li>Please let organizers know of any accessibility needs</li>
<li>We strive to create welcoming space for all</li>
<li>Gender-neutral restrooms available</li>
<li>Scent-free environment preferred</li>
</ul>

<h3>Food and Beverages</h3>
<ul>
<li>Light refreshments provided</li>
<li>Let us know of allergies or dietary restrictions</li>
<li>Clean up after yourself</li>
<li>No alcohol served</li>
</ul>

<h3>Information Sharing</h3>
<ul>
<li>Feel free to ask questions about community</li>
<li>Event organizers available to discuss programming</li>
<li>Take informational materials and resources</li>
<li>Connect via official community channels only</li>
</ul>

<h3>Zero Tolerance</h3>
<ul>
<li>No harassment, discrimination, or predatory behavior</li>
<li>No boundary violations or inappropriate advances</li>
<li>Violations result in removal and ban from all events</li>
<li>Community safety is our highest priority</li>
</ul>

<h3>Community Values</h3>
<ul>
<li>Consent, respect, and safety above all</li>
<li>Inclusive and welcoming to all backgrounds</li>
<li>Support for learning and growth</li>
<li>Building authentic connections and friendships</li>
</ul>"
            ),

            // Past Events (2 events) for testing historical data
            CreateSeedEvent(
                title: "Beginner Rope Circle",
                daysFromNow: -7,
                startHour: 18,
                capacity: 20,
                eventType: EventType.Social,
                price: 10.00m,
                shortDescription: "Past event: Introductory session for newcomers to rope bondage.",
                longDescription: @"<p>This past event was an introductory practice session designed for absolute beginners and those new to the rope community.</p>

<h3>Past attendees learned</h3>
<ul>
<li>Basic rope handling techniques</li>
<li>Simple single column ties</li>
<li>Introduction to safety principles</li>
<li>Communication fundamentals</li>
<li>How to get started with rope practice</li>
</ul>

<p>This was a supportive, beginner-friendly environment where participants could ask questions, meet fellow newcomers, and start their rope journey in a welcoming space.</p>",
                policies: @"<h2>Standard Event Policies Applied</h2>
<ul>
<li>18+ with ID required</li>
<li>Consent and communication required</li>
<li>No photography without permission</li>
<li>Respectful conduct expected</li>
<li>Zero tolerance for harassment</li>
</ul>"
            ),

            CreateSeedEvent(
                title: "Rope Fundamentals Series",
                daysFromNow: -14,
                startHour: 17,
                capacity: 15,
                eventType: EventType.Class,
                price: 40.00m,
                shortDescription: "Past event: Multi-session fundamentals course for serious students.",
                longDescription: @"<p>This past 4-week course provided comprehensive instruction in rope bondage fundamentals for dedicated students.</p>

<h3>Course Covered</h3>
<ul>
<li><strong>Week 1:</strong> Safety, consent, and communication foundations</li>
<li><strong>Week 2:</strong> Single column ties and basic cuffs</li>
<li><strong>Week 3:</strong> Double column ties and connecting techniques</li>
<li><strong>Week 4:</strong> Basic chest harnesses and body ties</li>
</ul>

<h3>Prerequisites</h3>
<p>Commitment to attend all 4 sessions</p>

<p>Past participants built a strong foundation in rope fundamentals with structured curriculum and progressive skill development.</p>",
                policies: @"<h2>Standard Class Policies Applied</h2>
<ul>
<li>18+ with ID required</li>
<li>All 4 sessions attendance required</li>
<li>Safety assessment and medical waiver</li>
<li>Consent protocols mandatory</li>
<li>No photography without permission</li>
<li>Professional conduct required</li>
</ul>"
            ),

            // Historical Event 1: Advanced Suspension Techniques Workshop
            CreateHistoricalEvent(
                id: out _advancedSuspensionEventId,
                title: "Advanced Suspension Techniques",
                daysFromNow: -75,
                startHour: 9,
                duration: 7,
                capacity: 20,
                eventType: EventType.Class,
                location: "Studio Space",
                shortDescription: "Advanced rope suspension workshop focusing on complex ties and safety protocols.",
                longDescription: @"<p>This advanced workshop covered sophisticated suspension bondage techniques for experienced practitioners.</p>

<h3>Workshop Content</h3>
<ul>
<li>Complex suspension ties and weight distribution</li>
<li>Advanced rigging safety and equipment</li>
<li>Multi-point suspension techniques</li>
<li>Emergency procedures and risk mitigation</li>
</ul>

<p>This was an intensive full-day workshop for experienced rope practitioners.</p>",
                policies: @"<h2>Advanced Workshop Policies</h2>
<ul>
<li>18+ with ID required</li>
<li>Prerequisite: Advanced rope experience required</li>
<li>Medical waiver mandatory</li>
<li>Professional conduct expected</li>
</ul>"
            ),

            // Historical Event 2: Rope Fundamentals Intensive
            CreateHistoricalEvent(
                id: out _ropeFundamentalsEventId,
                title: "Rope Fundamentals Intensive",
                daysFromNow: -60,
                startHour: 9,
                duration: 8.5m,
                capacity: 25,
                eventType: EventType.Class,
                location: "Salem Community Center",
                shortDescription: "Full-day intensive workshop covering rope bondage fundamentals from basics to practice.",
                longDescription: @"<p>This full-day intensive provided comprehensive instruction in rope bondage fundamentals.</p>

<h3>Workshop Covered</h3>
<ul>
<li>Safety principles and risk awareness</li>
<li>Core rope ties and techniques</li>
<li>Communication and consent</li>
<li>Hands-on practice and review</li>
</ul>

<p>Participants gained a solid foundation in rope bondage through structured instruction and guided practice.</p>",
                policies: @"<h2>Workshop Policies</h2>
<ul>
<li>18+ with ID required</li>
<li>Beginner-friendly, no experience required</li>
<li>Safety protocols mandatory</li>
<li>Respectful conduct expected</li>
</ul>"
            ),

            // Historical Event 3: Monthly Rope Practice Night
            CreateHistoricalEvent(
                id: out _practiceNightEventId,
                title: "Monthly Rope Practice Night",
                daysFromNow: -45,
                startHour: 19,
                duration: 4,
                capacity: 30,
                eventType: EventType.Social,
                location: "The Gathering Space",
                shortDescription: "Open practice session for all skill levels. Bring your rope and a partner!",
                longDescription: @"<p>This was our monthly open practice session where rope enthusiasts of all levels gathered to practice and connect.</p>

<h3>Event Features</h3>
<ul>
<li>Self-directed practice at your own pace</li>
<li>Informal peer learning and knowledge sharing</li>
<li>Welcoming atmosphere for all skill levels</li>
<li>Community building and socializing</li>
</ul>

<p>A casual, judgment-free space to explore rope bondage and meet fellow practitioners.</p>",
                policies: @"<h2>Practice Night Policies</h2>
<ul>
<li>18+ with ID required</li>
<li>All skill levels welcome</li>
<li>Practice safely within your abilities</li>
<li>Respect all boundaries and consent</li>
</ul>"
            ),

            // Historical Event 4: New Member Welcome Mixer
            CreateHistoricalEvent(
                id: out _welcomeMixerEventId,
                title: "New Member Welcome Mixer",
                daysFromNow: -30,
                startHour: 18,
                duration: 3,
                capacity: 40,
                eventType: EventType.Social,
                location: "Salem Community Center",
                shortDescription: "Social mixer to welcome new members and connect with the community.",
                longDescription: @"<p>This welcome mixer provided new members an opportunity to meet the community and learn about upcoming events.</p>

<h3>Event Activities</h3>
<ul>
<li>Meet and greet with community members</li>
<li>Overview of community programs and events</li>
<li>Q&A about the rope community</li>
<li>Building connections and friendships</li>
</ul>

<p>A welcoming, inclusive space for new members to feel at home in the community.</p>",
                policies: @"<h2>Welcome Event Policies</h2>
<ul>
<li>18+ with ID required</li>
<li>Welcoming to all backgrounds</li>
<li>Respectful interaction required</li>
<li>No experience necessary</li>
</ul>"
            )
        };

        await _context.Events.AddRangeAsync(sampleEvents, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sample event creation completed. Created: {EventCount} events", sampleEvents.Length);
    }

    /// <summary>
    /// Helper method to create sample events with proper UTC DateTime handling.
    /// Follows ApplicationDbContext patterns for UTC date storage and audit fields.
    ///
    /// Creates realistic event data with proper scheduling, capacity, pricing information,
    /// and complete descriptive fields (short description, long description, policies).
    /// </summary>
    private Event CreateSeedEvent(
        string title,
        int daysFromNow,
        int startHour,
        int capacity,
        EventType eventType,
        decimal price,
        string shortDescription,
        string longDescription,
        string policies)
    {
        // Calculate UTC dates following ApplicationDbContext patterns
        var startDate = DateTime.UtcNow.AddDays(daysFromNow).Date.AddHours(startHour);
        var endDate = startDate.AddHours(eventType == EventType.Social ? 2 : 3); // Social events 2hrs, classes 3hrs

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            ShortDescription = shortDescription,      // Brief summary for cards/listings
            Description = longDescription,             // Full detailed description
            Policies = policies,                       // Event policies and safety guidelines
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            Capacity = capacity,
            EventType = eventType,
            Location = eventType == EventType.Social ? "Community Space" : "Main Workshop Room",
            IsPublished = true,
            // CreatedAt/UpdatedAt will be set by ApplicationDbContext.UpdateAuditFields()
        };
    }

    /// <summary>
    /// Helper method to create historical events with specific ID capture for cross-seeder references.
    /// Used for historical events that need comprehensive attendance data across multiple seeders.
    /// </summary>
    private Event CreateHistoricalEvent(
        out Guid id,
        string title,
        int daysFromNow,
        int startHour,
        decimal duration,
        int capacity,
        EventType eventType,
        string location,
        string shortDescription,
        string longDescription,
        string policies)
    {
        id = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(daysFromNow).Date.AddHours(startHour);
        var endDate = startDate.AddHours((double)duration);

        return new Event
        {
            Id = id,
            Title = title,
            ShortDescription = shortDescription,
            Description = longDescription,
            Policies = policies,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            Capacity = capacity,
            EventType = eventType,
            Location = location,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow.AddDays(daysFromNow - 5),
            UpdatedAt = DateTime.UtcNow.AddDays(daysFromNow)
        };
    }
}