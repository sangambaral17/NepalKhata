# ShopPro UI Design System

> **Philosophy**: Apple meets Enterprise — clean, confident, breathable.  
> Every pixel must feel intentional. No clutter, no noise.

---

## Color Palette

### Brand Colors
| Token | Hex | Usage |
|-------|-----|-------|
| Primary | `#3B5BDB` | Navigation, buttons, links, active states |
| Primary Light | `#748FFC` | Hover states |
| Primary Dark | `#2F4AC0` | Pressed states, login gradient |
| Accent | `#51CF66` | Success, positive values, healthy stock |
| Warning | `#FF922B` | Low stock alerts, pending states |
| Danger | `#FA5252` | Errors, deletions, critical stock |
| Info | `#339AF0` | Informational elements |
| Purple | `#CC5DE8` | Revenue/customers card accents |

### Light Theme
| Token | Hex | Usage |
|-------|-----|-------|
| Background | `#F8F9FA` | Page background |
| Surface | `#FFFFFF` | Cards, panels, top bar |
| Surface2 | `#F1F3F5` | Secondary surfaces, input backgrounds |
| Border | `#E9ECEF` | Subtle dividers |
| Text Primary | `#212529` | Headings, body text |
| Text Secondary | `#868E96` | Labels, hints, dates |
| Sidebar BG | `#FFFFFF` | Sidebar background |
| Sidebar Active | `#EEF2FF` | Active nav item background |

### Dark Theme
| Token | Hex |
|-------|-----|
| Background | `#0F1117` |
| Surface | `#1A1D27` |
| Surface2 | `#22263A` |
| Border | `#2C3050` |
| Text Primary | `#F1F3F5` |
| Text Secondary | `#909BBD` |
| Sidebar BG | `#1A1D27` |
| Sidebar Active | `#2A3159` |

---

## Typography

| Level | Size | Weight | Font | Usage |
|-------|------|--------|------|-------|
| H1 | 24px | 600 | Segoe UI Variable | Page titles |
| H2 | 18px | 600 | Segoe UI Variable | Section headings |
| H3 | 15px | 600 | Segoe UI Variable | Card titles |
| Body | 14px | 400 | Segoe UI Variable | General text |
| Body Small | 12px | 400 | Segoe UI Variable | Secondary text |
| Label | 11px | 500 | Segoe UI Variable | Uppercase labels, 0.8px letter-spacing |
| Monospace | 13px | 400 | Cascadia Code / Consolas | SKU, GSTIN codes |

---

## Spacing System (8px Grid)

| Token | Value | Usage |
|-------|-------|-------|
| XS | 4px | Tight gaps |
| SM | 8px | Between related items |
| MD | 16px | Standard padding |
| LG | 24px | Section spacing, card padding |
| XL | 32px | Large section gaps |
| XXL | 48px | Top-level page padding |

---

## Corner Radii

| Element | Radius |
|---------|--------|
| Cards / Panels | 16px |
| Buttons | 10px |
| Input Fields | 10px |
| Badges / Tags | 6px |
| Tooltips | 8px |
| Pills / Avatars | 50% (circle) |

---

## Shadows (Light Theme Only)

| Token | Value |
|-------|-------|
| Card | `0 1px 3px rgba(0,0,0,0.06), 0 4px 16px rgba(0,0,0,0.06)` |
| Elevated Card | `0 4px 24px rgba(59,91,219,0.10)` |
| Button | `0 2px 8px rgba(59,91,219,0.25)` |

> Dark theme uses border emphasis instead of shadows.

---

## Component Styles

### Buttons
- **Primary**: Gradient background (#3B5BDB → #5C7CFA), white text, shadow, scale on hover/press
- **Secondary**: Transparent, 1.5px primary border, primary text
- **Danger**: Light red (#FFF5F5) background, red border and text
- **Icon**: 36×36px, transparent, hover shows Surface2 background

### Navigation
- Items: 44px height, 16px left padding, 10px corner radius
- Active: Sidebar Active bg + 3px primary left border + primary text (SemiBold)
- Hover: Surface2 background, 150ms transition
- Group labels: 11px uppercase, Text Secondary color

### Data Grids
- Header: Surface2 background, 11px uppercase SemiBold, 40px height
- Rows: 52px height, transparent bg, hover → Surface2, selected → Primary 8% opacity
- SKU/GSTIN columns: Monospace font, Text Secondary color
- Price columns: Right-aligned, SemiBold

### Stat Cards
- 120px height, 16px corner radius, Surface background, Card Shadow
- 44px icon circle with 10% opacity tint of the icon color
- Value: 28px, Bold | Label: 13px, Text Secondary

### Input Fields
- Height: 48px, Surface2 background, 1px Border
- Focus: 2px solid Primary border
- Icon inside left, 16px padding

---

## File Structure

```
Styles/
├── DesignSystem.xaml    — Colors, typography, spacing, shadows
└── Controls.xaml        — Button, input, nav, card, badge styles
```

Both dictionaries are merged in `App.xaml` after MaterialDesign2.Defaults.
Theme switching is handled in `MainViewModel.ApplyTheme()` which swaps all dynamic brushes.
