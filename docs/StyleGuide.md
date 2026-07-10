# SailScores UI Style Guide

## Purpose

This document is the beginning of a shared styling guide for SailScores UI work. It is meant to help agents and contributors keep the site visually consistent, especially in light and dark mode, and to follow the button patterns already used in the existing race pages.

## Core styling principles

- Prefer Bootstrap 5 components and utility classes over bespoke HTML and CSS where possible.
- Reuse existing site patterns before introducing new ones.
- Keep styling theme-aware so the same UI works in both light and dark mode.
- Favor clear hierarchy: primary actions should stand out, supporting actions should be quieter.
- When adding new UI, look at the current Race views as the reference pattern for layout, spacing, and button treatment.

## Light and dark mode support

SailScores already includes theme-aware styling for both light and dark mode. New UI should follow that approach.

- Use Bootstrap theme variables such as `--bs-body-bg`, `--bs-body-color`, `--bs-border-color`, and `--bs-primary` rather than hard-coded colors.
- Use existing site helpers and semantic surface classes when possible.
- The `bg-almost` class is used throughout the site for subtle panels and section backgrounds. It is already styled for both light and dark modes.
- Avoid forcing a light-only appearance. If a component needs a softer background, prefer `bg-almost`, `bg-body`, or `border` over hard-coded `#fff` or `#f8f9fa` values.
- Make sure interactive elements remain readable and distinct in both modes, especially text, borders, and button states.

## Buttons

Button styling should be simple, consistent, and easy to scan.

### Primary actions

Use a primary button for the main action on a form or page.

- Save, Create, and similar completion actions should use `btn btn-primary`.
- The Race Create page uses a primary button for the main submit action, and the Race Edit page uses a primary button for Save.

### Secondary and supporting actions

Use outline or secondary buttons for actions that should not compete with the primary action.

- Cancel actions should use `btn btn-outline-primary`.
- Supporting actions such as optional tools or alternate paths can use `btn btn-outline-secondary`.
- The Race Create and Edit pages use outline buttons for Cancel and related secondary actions.

### Small icon buttons

Small buttons may be icon-only when the action is compact and repeated.

- Use `btn btn-sm` for compact controls.
- Icon-only buttons are appropriate for dense list rows and repeated row actions.
- The Race Index page uses small icon buttons for Edit and Delete.

### Label rules

- Larger buttons should usually have a clear word label such as Save, Cancel, Create, or New Race.
- Extra small buttons may be icon-only if the action is obvious from context.
- Avoid mixing word labels and icons unnecessarily on larger buttons.

## Race page examples

The Race pages are a good reference for UI patterns across the site.

### Race create and edit

- Use a strong primary button for the main submit action.
- Keep Cancel as a secondary outline button.
- Keep form layout consistent with the existing page structure and spacing.
- Use the same action ordering and spacing as the current Race Create/Edit forms.

### Race index

- Use a larger text button for page-level actions such as New Race.
- Use small icon-only buttons for repeated inline actions such as Edit and Delete.
- Keep list rows visually lightweight, with clear spacing and a consistent action column.

## Minimalist page patterns

The cleanest minimalist pages on the site are the list-oriented pages that let the content breathe: Race/Index, Series/Index, and Regatta/Index. These pages work well as reference patterns when a screen should feel calm, focused, and easy to scan.

### What these pages do well

- Use a single clear page title or section heading.
- Keep action areas simple and restrained, often with one primary button or a compact action row.
- Rely on spacing, typography, and row separators instead of heavy cards or decorative containers.
- Keep secondary details muted and secondary in visual weight.
- Use icon-only controls sparingly for repeated inline actions, not for core primary actions.

### Practical guidance for new pages

- When designing a list page or content-heavy page, start with a minimalist layout before adding visual emphasis.
- Prefer a simple structure: heading, optional action row, then content list or table.
- Reserve heavier visual treatments for pages that truly need emphasis, such as forms or multi-step workflows.
- Use `bg-almost` or subtle borders for separation when needed, but avoid visual clutter.
- Use borders to group related items when a page contains multiple groups or sections, such as several cards, rows, or panels.
- Do not add borders when there is only one group on the page; in that case, rely on spacing, typography, and background contrast instead.
- When a page is mostly a collection of items, favor simple rows, clear action affordances, and minimal decoration.

### Recommended examples to reference

- `SailScores.Web/Views/Race/Index.cshtml` for compact row actions and lightweight list layout.
- `SailScores.Web/Views/Series/Index.cshtml` for content-first list presentation with a calm section hierarchy.
- `SailScores.Web/Views/Regatta/Index.cshtml` for a simple, low-noise page structure with minimal action styling.

## Implementation checklist

When adding or updating UI:

1. Check whether an existing pattern already exists on the site.
2. Prefer Bootstrap classes and current site helpers over new custom styles.
3. Make sure the UI works in both light and dark mode.
4. Use a primary button for the main action and an outline button for the supporting action.
5. Use icon-only buttons only for compact actions, not for larger primary controls.
