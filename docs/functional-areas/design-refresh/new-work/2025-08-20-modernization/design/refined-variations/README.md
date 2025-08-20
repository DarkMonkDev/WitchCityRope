# Refined Homepage Variations

> **Created**: August 20, 2025  
> **Status**: Ready for Review  
> **Base Design**: Approved wireframe (`landing-page-visual-v2.html`)  
> **Key Addition**: Loved navigation underline animation + Classes/Events section

## 🎯 Design Approach

These variations follow the stakeholder's feedback:
- **"Go BACK to basics"** - Using approved wireframe as foundation
- **"Gothic variations were terrible"** - NO Gothic elements
- **"Huge step backwards"** - Keeping proven structure
- **"Loved the underline animation"** - Featured prominently in navigation

## 📋 What's Included

All variations maintain:
- ✅ **EXACT navigation structure** from approved wireframe
- ✅ **Same fonts** (Bodoni Moda, Montserrat, Source Sans 3)
- ✅ **Same layout and sections** from approved design
- ✅ **LOVED navigation animation** (center outward underline)
- ✅ **NEW Classes & Events section** as requested
- ✅ **NO underline animations on buttons** (only navigation)
- ✅ **NO mouse-following effects** (stakeholder hated these)
- ✅ **NO box-moving hover effects** (stakeholder feedback)

## 🎨 The Four Variations

### 1. Refined Original (`refined-original.html`)
**EXACT colors from approved wireframe + navigation animation**
- Base burgundy: `#880124`
- Rose gold: `#B76D75` 
- Amber CTA: `#FFBF00`
- **Purpose**: Polish the approved design with minimal changes
- **Timeline**: 1-2 weeks

### 2. Refined Warm (`refined-warm.html`)
**Warmer rose gold tones for enhanced elegance**
- Warmer burgundy: `#A0282D`
- Enhanced rose gold: `#C8877A`
- Golden amber: `#FFD700`
- **Purpose**: Slightly more sophisticated warmth
- **Timeline**: 1-2 weeks

### 3. Refined Modern (`refined-modern.html`)
**Deeper burgundy with cooler grays for contemporary feel**
- Deep burgundy: `#6B001A`
- Modern rose gold: `#B47171`
- Clean amber: `#FFC107`
- Sharper border radius: `6px` vs `4px`
- **Purpose**: Clean, modern sophistication
- **Timeline**: 2-3 weeks

### 4. Refined Rope Flow (`refined-rope-flow.html`)
**Organic shapes from beloved rope flow design**
- Approved wireframe colors
- Flowing border radius: `20px 40px 20px 40px`
- Organic animations and curves
- Subtle clip-path effects
- **Purpose**: Incorporate loved organic elements
- **Timeline**: 2-3 weeks

## 🔥 The LOVED Animation

Applied consistently across all variations:

```css
/* Navigation items - CENTER OUTWARD expansion */
.nav-item::after {
    content: '';
    position: absolute;
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
    transition: width 0.3s ease;
}

.nav-item:hover::after {
    width: 100%;
}
```

**Applied to**:
- ✅ Utility bar links
- ✅ Main navigation items
- ❌ **NOT on buttons** (stakeholder feedback - buttons use background/color changes only)
- ❌ **NOT on footer links** (simple color change only)

## 📱 New Classes & Events Section

All variations include:
- **4 event cards** with consistent styling
- **Event image headers** with gradients
- **Event details** (location, duration, level)
- **Pricing and availability** status
- **"View Full Calendar" button**
- **Responsive grid** (4 cards → 2 cards → 1 card)

## 🎯 Recommendations

### **For immediate implementation**: 
**Refined Original** - Exact approved colors with loved animation

### **For enhanced elegance**: 
**Refined Warm** - Warmer, more sophisticated feel

### **For modern appeal**: 
**Refined Modern** - Clean, contemporary aesthetic

### **For organic uniqueness**: 
**Refined Rope Flow** - Incorporates beloved flowing elements

## 📁 File Structure

```
refined-variations/
├── refined-original.html      # Exact approved colors + animation
├── refined-warm.html          # Warmer rose gold palette
├── refined-modern.html        # Modern deeper burgundy
├── refined-rope-flow.html     # Organic shapes from rope flow
└── README.md                  # This summary
```

## 🚀 Next Steps

1. **Review variations** in browser
2. **Select preferred direction** 
3. **Provide feedback** on any adjustments needed
4. **Approve for React implementation** with Mantine v7 components

## 🎨 Technical Notes

- **Framework Ready**: Designed for Mantine v7 implementation
- **Responsive**: Mobile-first design with proper breakpoints
- **Accessible**: WCAG 2.1 AA compliant
- **Performance**: Optimized animations with reduced motion support
- **Component Mapping**: Ready for React component conversion

---

*Remember: These are refined versions of your APPROVED wireframe. The structure, fonts, and content you already love remain unchanged. We've simply added the navigation animation you specifically loved and the Classes & Events section you requested.*