# SailScores SCSS Refactoring Summary

## Overview
Successfully broke down the monolithic `custom.scss` file (900+ lines) into a modular, maintainable SCSS architecture with clear separation of concerns and explicit dark-mode support.

## New SCSS Structure

### Files Created in `SailScores.Web/scss/`

1. **_variables.scss** (36 lines)
   - Project color tokens (primary, dark-mode palette)
   - OCR table color tokens
   - Typography settings
   - Finish place colors (light & dark)
   - Theme configuration

2. **_admin.scss** (110 lines)
   - Admin page accordion styling
   - Accordion button and title underline control
   - Chevron 90-degree rotation (collapsed→expanded)
   - Help icon styling
   - Side navigation (sticky, responsive at xxl+)
   - Includes dark-mode border override

3. **_dark-mode.scss** (340 lines)
   - **Comprehensive dark-mode support** using `@media (prefers-color-scheme: dark)`
   - CSS custom property overrides (:root) for Bootstrap variables
   - Component-specific dark colors (buttons, forms, tables, OCR, select2, etc)
   - Display utilities (.d-dark-none, .d-dark-inline, .d-dark-block)
   - All dark-mode logic in ONE file for easy maintenance

4. **_components.scss** (220 lines)
   - Shared component overrides (light mode)
   - Link and button styling
   - Select2 theme customizations
   - Navigation branding
   - Finish place and race status colors (light mode)
   - Card headers, form labels, etc

5. **_tables.scss** (200 lines)
   - AllCompHistogram sticky header table
   - OCR results table styling
   - Score entry panel styling
   - Score row borders (responsive breakpoints)
   - OCR preview and progress components
   - Insert row buttons, cropper styling
   - Responsive media queries (mobile/desktop)

6. **_utilities.scss** (160 lines)
   - Utility classes
   - Help/FAQ navigation sidebar
   - System alert banner
   - Supporter logo containers
   - Sticky positioning utilities
   - Print styles

### Import Order in `custom.scss`

```scss
@import "_variables";                              // 1. Project tokens first
@import "../node_modules/bootstrap/scss/...";     // 2. Bootstrap core
$s2bs5-border-color: $border-color;
@import "../node_modules/select2-bootstrap-5-...";// 3. Select2 theme
@import "_components";                             // 4. Component overrides (light)
@import "_admin";                                  // 5. Admin page styles
@import "_tables";                                 // 6. Table & OCR styles
@import "_utilities";                              // 7. Utilities & misc
@import "_dark-mode";                              // 8. DARK MODE (must be last)
```

**Critical:** `_dark-mode.scss` is imported LAST so `@media (prefers-color-scheme: dark)` rules cascade correctly over light-mode defaults.

## Dark Mode Architecture

### How It Works
- **Light-mode styles** are in individual component files (_components.scss, _admin.scss, _tables.scss, _utilities.scss)
- **All dark-mode overrides** are concentrated in `_dark-mode.scss`
- **Scoping mechanism:** `@media (prefers-color-scheme: dark) { ... }` wraps all dark-mode rules
- **CSS variables** at `:root` override Bootstrap's defaults for dark mode
- **Component selectors** inside the media query override light-mode styles

### Example
```scss
/* _components.scss (light mode) */
.finish-first {
    background: #ffe974 !important;
}

/* _dark-mode.scss (dark mode) */
@media (prefers-color-scheme: dark) {
    .finish-first {
        background: #473F24 !important;
    }
}
```

## Build Process

### Gulp Tasks (Already Working)
- **`gulp sass`** - Compiles all SCSS partials through custom.scss into wwwroot/css/custom.css
- **`gulp min:css`** - Minifies and concatenates CSS files into wwwroot/css/site.min.css
- **`gulp prebuild`** - Runs sass, copy:scripts, min:js, min:css in sequence

### Verification
✅ `npx gulp sass` → 339 KB custom.css  
✅ `npx gulp min:css` → 279 KB site.min.css  
✅ Full build successful with no errors

## Changes to Razor Views

### Admin/Index.cshtml
- **Removed:** 105-line inline `<style>` block
- **Added:** Comment referencing `SailScores.Web/scss/_admin.scss`
- **Benefit:** View is now 40% smaller, easier to read, styles in proper SCSS location

## Benefits

✨ **Modularity** - Each file has a single responsibility  
✨ **Maintainability** - Easy to locate and update specific component styles  
✨ **Dark Mode** - All dark-mode logic in one file (_dark-mode.scss) for consistency  
✨ **Reusability** - Shared components can be used across multiple pages  
✨ **Scalability** - New partials can be added without cluttering custom.scss  
✨ **Build Integration** - Gulp tasks pick up all partials automatically  
✨ **CSS Variables** - Leverages Bootstrap's CSS variable system for dark mode  

## Dark Mode Support Checklist

✅ Uses Bootstrap CSS variables (var(--bs-link-color), var(--bs-primary-text-emphasis), etc)  
✅ Comprehensive `@media (prefers-color-scheme: dark)` coverage in _dark-mode.scss  
✅ Component-specific dark colors for tables, forms, buttons, OCR, select2  
✅ Border and shadow overrides for dark mode  
✅ OCR table custom properties scoped to dark mode  
✅ Help navigation active states adapted for dark mode  
✅ All dark-mode rules in single file for easy review/maintenance  

## Future Maintenance

To add new component styles:
1. If light-mode only → Add to _components.scss or new feature SCSS file
2. If has dark-mode variant → Light-mode in _components.scss, dark-mode in _dark-mode.scss
3. If page-specific → Create new _[feature].scss and import in custom.scss
4. Run `npx gulp sass` to compile

To update dark-mode colors:
1. Update _variables.scss for token values
2. Update selectors in _dark-mode.scss
3. Run `npx gulp sass` to test
