# CMS Integration Research

*Generated on August 13, 2025*

## Overview
This document researches content management system (CMS) integration options for the WitchCityRope migration from Blazor Server to React, focusing on managing the community's content needs.

## Current WitchCityRope Content Structure

### Existing Content Management
- **Content Storage**: Database-driven content via `ContentPage` entity
- **Static Pages**: Multiple public content pages (About, FAQ, Terms, etc.)
- **Event Content**: Rich event descriptions and details
- **Educational Content**: Classes, workshops, resources
- **Legal Content**: Terms of Service, Privacy Policy, Code of Conduct

### Current Content Pages Inventory
```
Public Content:
├── About Us                    # Community overview and mission
├── Contact                     # Contact information and forms
├── FAQ                        # Frequently asked questions
├── Terms of Service          # Legal terms and conditions
├── Privacy Policy            # Privacy and data protection
├── Code of Conduct           # Community guidelines and rules
├── Classes Overview          # Educational program information
├── Workshops                 # Workshop descriptions and schedules
├── Private Lessons          # One-on-one instruction information
├── Resources                # Educational materials and links
├── Glossary                 # Rope bondage terminology
├── Consent Information      # Safety and consent guidelines
├── How to Join             # Membership application process
└── Incident Reporting      # Safety incident reporting
```

### Content Requirements Analysis
- **Static Content**: Terms, policies, educational materials
- **Dynamic Content**: Event listings, member resources
- **Rich Media**: Images, videos, educational diagrams
- **Structured Content**: Event details, member profiles
- **Legal Content**: Compliance and safety documentation
- **Community Content**: Guidelines, resources, announcements

## Content Management Strategy Options

### **Option 1: File-Based Content Management (Recommended)**

#### **Markdown/MDX Approach**
**Best for**: WitchCityRope's content-heavy, community-focused platform

**Implementation**:
```
content/
├── pages/
│   ├── about.mdx              # About page with interactive components
│   ├── faq.mdx               # FAQ with searchable content
│   ├── terms-of-service.md   # Legal content
│   ├── privacy-policy.md     # Privacy documentation
│   └── code-of-conduct.mdx   # Community guidelines
├── educational/
│   ├── classes/
│   │   ├── rope-basics.mdx   # Class descriptions
│   │   └── advanced-ties.mdx
│   ├── workshops/
│   └── resources/
├── legal/
│   ├── policies/
│   └── guidelines/
└── templates/
    ├── event-template.mdx    # Event page template
    └── class-template.mdx    # Class page template
```

**Technology Stack**:
```typescript
// Next.js with MDX support
import { MDXRemote } from 'next-mdx-remote';
import { serialize } from 'next-mdx-remote/serialize';

// Content processing
const getStaticProps = async ({ params }) => {
  const content = await getContentBySlug(params.slug);
  const mdxSource = await serialize(content.body, {
    mdxOptions: {
      remarkPlugins: [remarkGfm],
      rehypePlugins: [rehypeHighlight]
    }
  });
  
  return {
    props: {
      content: { ...content, body: mdxSource }
    }
  };
};

// Custom components for MDX
const mdxComponents = {
  EventCard: ({ title, date, description }) => (
    <div className="event-card">
      <h3>{title}</h3>
      <time>{date}</time>
      <p>{description}</p>
    </div>
  ),
  SafetyNotice: ({ children }) => (
    <div className="safety-notice">
      <Icon name="alert-triangle" />
      {children}
    </div>
  ),
  AgeVerificationBanner: () => (
    <div className="age-banner">
      <strong>21+ Community - Age Verification Required</strong>
    </div>
  )
};
```

#### **Advantages**:
- **Version Control**: Content changes tracked in Git
- **Developer Workflow**: Familiar markdown editing
- **Performance**: Static generation for optimal speed
- **Cost**: No additional CMS hosting costs
- **Security**: No CMS-specific vulnerabilities
- **Backup**: Content automatically backed up with code

#### **Disadvantages**:
- **Non-Technical Users**: Requires technical knowledge for content updates
- **Immediate Updates**: Changes require deployment
- **Rich Media Management**: Manual image/video handling
- **Workflow**: No visual editing interface

### **Option 2: Headless CMS Integration**

#### **Strapi (Self-Hosted, Open Source)**

**Implementation Example**:
```typescript
// Strapi content fetching
const strapiClient = {
  async getContent(slug: string) {
    const response = await fetch(`${STRAPI_URL}/api/pages?filters[slug][$eq]=${slug}&populate=*`);
    return response.json();
  },
  
  async getEvents() {
    const response = await fetch(`${STRAPI_URL}/api/events?populate=*&sort=startDate:asc`);
    return response.json();
  },
  
  async getEducationalContent() {
    const response = await fetch(`${STRAPI_URL}/api/educational-contents?populate=*`);
    return response.json();
  }
};

// Content types in Strapi
const contentTypes = {
  page: {
    title: 'text',
    slug: 'text',
    content: 'richtext',
    metaDescription: 'text',
    publishedAt: 'datetime'
  },
  event: {
    title: 'text',
    description: 'richtext',
    startDate: 'datetime',
    endDate: 'datetime',
    capacity: 'number',
    prerequisites: 'text',
    instructor: 'relation'
  },
  educationalContent: {
    title: 'text',
    category: 'enumeration',
    content: 'richtext',
    difficulty: 'enumeration',
    tags: 'relation'
  }
};
```

**Advantages**:
- **Open Source**: Full control and customization
- **Self-Hosted**: Data ownership and privacy control
- **API Flexibility**: REST and GraphQL support
- **Plugin Ecosystem**: Extensive plugin library
- **User Management**: Built-in role-based permissions
- **Cost**: Free (hosting costs only)

**Disadvantages**:
- **Infrastructure**: Requires server management
- **Maintenance**: Updates and security patches
- **Learning Curve**: Complex for non-technical users
- **Performance**: Additional API layer overhead

#### **Contentful (Managed Service)**

**Implementation Example**:
```typescript
import { createClient } from 'contentful';

const contentfulClient = createClient({
  space: process.env.CONTENTFUL_SPACE_ID,
  accessToken: process.env.CONTENTFUL_ACCESS_TOKEN
});

// Content fetching with TypeScript types
interface WCRPage {
  title: string;
  slug: string;
  content: Document;
  metaDescription: string;
  publishedDate: string;
}

const getPageBySlug = async (slug: string): Promise<WCRPage | null> => {
  const entries = await contentfulClient.getEntries<WCRPage>({
    content_type: 'page',
    'fields.slug': slug,
    limit: 1
  });
  
  return entries.items[0]?.fields || null;
};

// Rich text rendering
import { documentToReactComponents } from '@contentful/rich-text-react-renderer';

const renderContent = (content: Document) => {
  return documentToReactComponents(content, {
    renderNode: {
      [BLOCKS.EMBEDDED_ENTRY]: (node) => {
        const { target } = node.data;
        if (target.sys.contentType.sys.id === 'eventCard') {
          return <EventCard {...target.fields} />;
        }
        return null;
      }
    }
  });
};
```

**Advantages**:
- **Managed Service**: No infrastructure management
- **Scalability**: Enterprise-grade performance
- **CDN**: Global content delivery
- **User Interface**: Intuitive editing experience
- **Integrations**: Extensive third-party integrations

**Disadvantages**:
- **Cost**: Expensive for high usage ($489/month for team plan)
- **Vendor Lock-in**: Dependent on Contentful
- **Migration Complexity**: Difficult to migrate away
- **Customization Limits**: Less flexible than self-hosted options

#### **Sanity (Real-Time Collaboration)**

**Implementation Example**:
```typescript
import { createClient } from '@sanity/client';
import { PortableText } from '@portabletext/react';

const sanityClient = createClient({
  projectId: process.env.SANITY_PROJECT_ID,
  dataset: 'production',
  useCdn: true,
  apiVersion: '2023-05-03'
});

// GROQ queries for content
const queries = {
  getPageBySlug: (slug: string) => `
    *[_type == "page" && slug.current == "${slug}"][0]{
      title,
      slug,
      content,
      metaDescription,
      publishedAt
    }
  `,
  
  getEvents: () => `
    *[_type == "event" && startDate > now()] | order(startDate asc) {
      title,
      slug,
      description,
      startDate,
      endDate,
      capacity,
      "instructor": instructor->name
    }
  `
};

// Portable Text components
const portableTextComponents = {
  types: {
    callout: ({ value }) => (
      <div className="callout callout-${value.type}">
        {value.text}
      </div>
    ),
    eventCard: ({ value }) => (
      <EventCard {...value} />
    )
  },
  marks: {
    highlight: ({ children }) => (
      <span className="text-highlight">{children}</span>
    )
  }
};

const ContentRenderer = ({ content }) => (
  <PortableText value={content} components={portableTextComponents} />
);
```

**Advantages**:
- **Real-Time Collaboration**: Multiple editors simultaneously
- **Flexible Content Modeling**: Portable Text format
- **Developer Experience**: React-based editing interface
- **Customization**: Highly customizable studio
- **Performance**: Efficient content delivery

**Disadvantages**:
- **Learning Curve**: Complex for non-technical users
- **Limited Templates**: Fewer pre-built content types
- **Cost**: Can be expensive at scale
- **Complexity**: Requires developer expertise

### **Option 3: Hybrid Approach (Recommended for WitchCityRope)**

#### **Strategy**: Git-based Content + Database Events

**Implementation**:
```typescript
// Static content from markdown
const getStaticContent = async (slug: string) => {
  const content = await import(`../content/pages/${slug}.mdx`);
  return {
    content: content.default,
    metadata: content.metadata
  };
};

// Dynamic content from API
const getDynamicContent = async () => {
  const events = await fetch('/api/events').then(r => r.json());
  const announcements = await fetch('/api/announcements').then(r => r.json());
  return { events, announcements };
};

// Combined content strategy
const HomePage = ({ staticContent, dynamicContent }) => {
  return (
    <div>
      <MDXContent content={staticContent} />
      <EventsSection events={dynamicContent.events} />
      <AnnouncementsSection announcements={dynamicContent.announcements} />
    </div>
  );
};
```

**Content Distribution**:
- **Static Content**: Legal pages, educational content, guidelines (Markdown/MDX)
- **Dynamic Content**: Events, announcements, user-generated content (Database/API)
- **Media Assets**: Images, videos (CDN or cloud storage)
- **User Content**: Profiles, reviews, comments (Database)

## Content Workflow Recommendations

### **For Static Content (Legal, Educational)**
```markdown
# Content Creation Workflow
1. Create/edit Markdown files in `/content` directory
2. Use MDX for interactive components (forms, calculators, etc.)
3. Commit changes to Git repository
4. Automatic deployment triggers static regeneration
5. Content appears on site within minutes

# Example: Code of Conduct with interactive elements
---
title: "Community Code of Conduct"
lastUpdated: "2025-08-13"
category: "legal"
---

# WitchCityRope Community Code of Conduct

<AgeVerificationBanner />

## Core Principles

Our community is built on...

<SafetyNotice>
**Safety First**: All activities must prioritize the physical and emotional safety of all participants.
</SafetyNotice>

## Reporting Violations

<IncidentReportForm />
```

### **For Dynamic Content (Events, Announcements)**
```typescript
// Admin interface for event management
const EventManager = () => {
  const { data: events, mutate } = useSWR('/api/events', fetcher);
  
  const createEvent = async (eventData) => {
    await fetch('/api/events', {
      method: 'POST',
      body: JSON.stringify(eventData)
    });
    mutate(); // Refresh events list
  };
  
  return (
    <div>
      <EventForm onSubmit={createEvent} />
      <EventsList events={events} />
    </div>
  );
};
```

## SEO and Performance Considerations

### **Static Site Generation (SSG)**
```typescript
// Next.js static generation for content pages
export async function getStaticPaths() {
  const contentSlugs = await getContentSlugs();
  return {
    paths: contentSlugs.map(slug => ({ params: { slug } })),
    fallback: 'blocking'
  };
}

export async function getStaticProps({ params }) {
  const content = await getContentBySlug(params.slug);
  return {
    props: { content },
    revalidate: 3600 // Regenerate every hour
  };
}
```

### **Incremental Static Regeneration (ISR)**
```typescript
// Dynamic content with ISR
export async function getStaticProps() {
  const events = await getUpcomingEvents();
  return {
    props: { events },
    revalidate: 300 // Regenerate every 5 minutes
  };
}
```

## Content Security and Compliance

### **Sensitive Content Management**
```typescript
// Age-gated content handling
const ContentGate = ({ children, requiresAge = 21 }) => {
  const { user, isAgeVerified } = useAuth();
  
  if (!user || !isAgeVerified(requiresAge)) {
    return <AgeVerificationRequired minAge={requiresAge} />;
  }
  
  return children;
};

// Usage in content
<ContentGate requiresAge={21}>
  <AdvancedTechniquesContent />
</ContentGate>
```

### **Content Moderation**
```typescript
// Content approval workflow
const ContentApprovalSystem = {
  submitContent: async (content, userId) => {
    const submission = await ContentSubmission.create({
      content,
      authorId: userId,
      status: 'pending',
      submittedAt: new Date()
    });
    
    // Notify moderators
    await notifyModerators(submission);
    return submission;
  },
  
  approveContent: async (submissionId, moderatorId) => {
    const submission = await ContentSubmission.findById(submissionId);
    submission.status = 'approved';
    submission.approvedBy = moderatorId;
    submission.approvedAt = new Date();
    await submission.save();
    
    // Publish content
    await publishContent(submission.content);
  }
};
```

## Migration Strategy

### **Phase 1: Static Content Migration (Week 1-2)**
1. **Extract Current Content**: Export all static pages from database
2. **Convert to Markdown**: Transform HTML content to Markdown/MDX
3. **Set Up File Structure**: Organize content in logical directory structure
4. **Implement Rendering**: Set up MDX processing and component integration
5. **Deploy Static Pages**: Make static content available

### **Phase 2: Dynamic Content Integration (Week 3-4)**
1. **Event Content API**: Maintain current event management via API
2. **Hybrid Rendering**: Combine static and dynamic content
3. **Admin Interface**: Build content management for dynamic content
4. **Cache Strategy**: Implement appropriate caching for performance

### **Phase 3: Enhanced Content Features (Week 5-6)**
1. **Rich Media Support**: Image/video handling and optimization
2. **Search Functionality**: Full-text search across all content
3. **Content Versioning**: Track content changes and revisions
4. **Analytics Integration**: Content performance tracking

### **Phase 4: Content Optimization (Week 7-8)**
1. **SEO Optimization**: Meta tags, structured data, sitemaps
2. **Performance Tuning**: Image optimization, lazy loading
3. **Accessibility Audit**: Ensure WCAG 2.1 AA compliance
4. **User Experience**: Navigation, search, content discovery

## Recommended Technology Stack

### **Primary Recommendation: Hybrid File-Based + API**
```typescript
// Technology choices
const contentStack = {
  staticContent: 'MDX files with Git version control',
  dynamicContent: 'Existing WitchCityRope API',
  rendering: 'Next.js with ISR',
  media: 'Cloudinary or AWS S3',
  search: 'Algolia or Fuse.js',
  analytics: 'Google Analytics + custom content metrics'
};
```

### **Implementation Architecture**
```
Content Layer:
├── Static Content (Markdown/MDX)
│   ├── Legal pages (terms, privacy, conduct)
│   ├── Educational content (classes, resources)
│   └── Community guidelines
├── Dynamic Content (API)
│   ├── Events and registrations
│   ├── User-generated content
│   └── Real-time announcements
└── Media Assets (CDN)
    ├── Images and videos
    ├── Educational materials
    └── User uploads
```

## Performance Benchmarks

### **Expected Performance Improvements**
- **Static Pages**: Load time < 1 second (vs current 2-3 seconds)
- **Dynamic Content**: Cached responses < 500ms
- **SEO Score**: 95+ (vs current 75-80)
- **Accessibility**: WCAG 2.1 AA compliance
- **Mobile Performance**: 90+ Lighthouse score

## Conclusion

For WitchCityRope, the **hybrid file-based + API approach** is recommended:

1. **Cost-Effective**: No additional CMS hosting costs
2. **Performance**: Optimal static site generation for content pages
3. **Developer-Friendly**: Git-based workflow familiar to development team
4. **Flexible**: Maintains existing API for dynamic content
5. **Secure**: Reduced attack surface with static content
6. **Maintainable**: Version-controlled content with deployment automation

**Key Success Factors**:
- Preserve all existing content during migration
- Maintain SEO rankings with proper redirects
- Ensure content creation workflow doesn't disrupt community management
- Implement proper age-gating and content moderation
- Focus on performance and accessibility improvements

This approach provides the best balance of performance, maintainability, and cost-effectiveness while preserving WitchCityRope's unique content requirements and community focus.