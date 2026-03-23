# Database Schema Documentation Summary

This document summarizes all database schema documentation available for the SailScores project.

## Documentation Files

### 1. **docs/DatabaseSchema.md** (Primary Reference)
The comprehensive database schema documentation.

**Contents**:
- **Mermaid ER Diagram**: Visual representation of all tables and relationships
- **Table Definitions**: Complete details for 9+ tables including:
  - Column names, types, constraints
  - Foreign key relationships
  - Usage notes for each table
- **Enum Types**: All enum definitions with values and descriptions
- **Validation & Constraints**: Business rules, date constraints, uniqueness rules
- **Common Query Patterns**: 5 ready-to-use SQL queries for frequent tasks
- **Schema Validation Queries**: Automated queries to verify schema accuracy

**When to Use**:
- You need complete details about a table
- You want to understand relationships
- You need to build a database query
- You're onboarding and learning the schema
- You want to see what queries are typical

**Size**: ~500 lines of detailed documentation

---

### 2. **docs/DatabaseSchema-QuickRef.md** (Fast Lookup)
Quick reference guide for fast lookups.

**Contents**:
- **Table Quick Reference**: One-line description of each table
- **Key Relationships Diagram**: ASCII diagram of entity hierarchy
- **Common Lookups**: How to solve 5 typical queries
- **Enums at a Glance**: Enum values in compact table format
- **Critical Constraints**: Business rules that affect development
- **Multi-Fleet Series Explanation**: Deep dive on series with multiple fleets
- **Performance Tips**: Optimization guidance
- **Validation Queries**: Schema verification SQL

**When to Use**:
- You need a quick answer
- You want to copy-paste a common query
- You're checking an enum value
- You need to remember a constraint
- You're optimizing a query

**Size**: ~200 lines of concise reference

---

### 3. **docs/SCHEMA_ENHANCEMENTS.md** (What's New)
Explains what was added to schema documentation.

**Contents**:
- **What Was Added**: Summary of each enhancement
- **Benefits**: Why each addition matters
- **How to Use These Enhancements**: For development, maintenance, and AI assistance
- **Future Enhancements**: Ideas for further documentation improvements

**When to Use**:
- You want to understand the documentation structure
- You need to know what information is available
- You're maintaining or updating the documentation
- You want to understand the rationale behind the docs

**Size**: ~200 lines

---

### 4. **.github/copilot-instructions.md** (Project Instructions)
Main copilot instructions file for the project.

**Database Schema Section Contents**:
- References to all three schema documentation files
- Quick summary of core tables for immediate context
- Key relationships and business rules
- Important notes (table name is "Races" not "Race", etc.)

**When to Use**:
- GitHub Copilot references this automatically
- You're setting up a new development environment
- You want the schema in context of other project rules

---

## Quick Navigation

### "I need to know about table X"
→ `docs/DatabaseSchema.md` - Full table definition section

### "What's the quick way to query Y?"
→ `docs/DatabaseSchema-QuickRef.md` - Common Lookups section

### "How do I find multi-fleet series?"
→ Both:
  - `docs/DatabaseSchema.md` - Multi-Fleet Series section (full details)
  - `docs/DatabaseSchema-QuickRef.md` - What Are Multi-Fleet Series section (quick answer)

### "What enum values are valid?"
→ `docs/DatabaseSchema-QuickRef.md` - Enums at a Glance

### "How are tables related?"
→ Either:
  - `docs/DatabaseSchema.md` - Mermaid ER Diagram (visual)
  - `docs/DatabaseSchema-QuickRef.md` - Key Relationships diagram (text)

### "I need a query pattern"
→ `docs/DatabaseSchema.md` - Common Query Patterns section

### "What are the rules I need to follow?"
→ `docs/DatabaseSchema-QuickRef.md` - Critical Constraints section

### "Can AI help with database questions?"
→ `docs/SCHEMA_ENHANCEMENTS.md` - For AI Assistance section

---

## Key Information Highlights

### Critical Facts
1. **Table Name**: It's `Races`, not `Race`
2. **Series Junction**: Use `SeriesRace` table to query races in a series
3. **Multi-Fleet**: A single series CAN contain races from different fleets
4. **Default Fleet**: When implementing, must handle multi-fleet series specially
5. **Series Hierarchy**: Summary series (Type=1) can contain child series via `SeriesToSeriesLink`

### Most Important Queries

**Find multi-fleet series** (for your "Default Fleet" feature assessment):
```sql
SELECT sr.SeriesId, COUNT(DISTINCT r.FleetId) AS FleetCount
FROM SeriesRace sr
INNER JOIN Race r ON sr.RaceId = r.Id
GROUP BY sr.SeriesId
HAVING COUNT(DISTINCT r.FleetId) > 1
```

**Get all races in a series** (most common operation):
```sql
SELECT r.* FROM SeriesRace sr
INNER JOIN Race r ON sr.RaceId = r.Id
WHERE sr.SeriesId = @SeriesId
```

**Find series with multiple fleets - detailed**:
```sql
SELECT s.Id, s.Name, COUNT(DISTINCT r.FleetId) AS FleetCount
FROM Series s
LEFT JOIN SeriesRace sr ON s.Id = sr.SeriesId
LEFT JOIN Race r ON sr.RaceId = r.Id
GROUP BY s.Id, s.Name
HAVING COUNT(DISTINCT r.FleetId) > 1
```

---

## Using with GitHub Copilot

When asking Copilot questions about the database:

1. **Reference specific files**: "Looking at docs/DatabaseSchema.md..."
2. **Ask about patterns**: Copilot will reference Common Query Patterns
3. **Clarify constraints**: Mention "multi-fleet series" and Copilot understands the complexity
4. **Query generation**: Copilot uses the documented patterns as examples
5. **Relationship questions**: The Mermaid diagram provides visual context

Example prompts:
- "Based on docs/DatabaseSchema.md, write a query to..."
- "I need to handle multi-fleet series (see DatabaseSchema-QuickRef.md). How should I..."
- "Following the Common Query Patterns in DatabaseSchema.md, create..."

---

## Maintenance Guide

### Keeping Documentation Current

**Quarterly (Every 3 months)**:
- Run Schema Validation Queries
- Check for schema drift
- Update if any tables were added/modified

**When Adding Features**:
- Add new table definitions if needed
- Add new Common Query Patterns
- Update Enum Definitions if adding new enum values
- Update the Mermaid ER Diagram

**When Database Changes**:
- Update corresponding table definition
- Run validation queries to verify
- Update any related Common Query Patterns
- Update Business Rules if constraints changed

### Adding New Content

1. **New Query Pattern**: Add to DatabaseSchema.md "Common Query Patterns" section
2. **New Table**: Add to DatabaseSchema.md "Table Definitions" + update Mermaid diagram
3. **New Enum**: Add to DatabaseSchema.md "Enum Types" + reference in QuickRef.md
4. **New Constraint**: Add to DatabaseSchema.md "Validation & Constraints"

---

## File Locations

All documentation files are in the `docs/` directory:

```
docs/
├── DatabaseSchema.md              (Main reference)
├── DatabaseSchema-QuickRef.md     (Quick lookup)
├── SCHEMA_ENHANCEMENTS.md         (What's new)
└── ... other project docs
```

Also referenced in:
```
.github/
└── copilot-instructions.md        (References schema docs)
```

---

## Summary

| Document | Purpose | Size | Audience |
|----------|---------|------|----------|
| **DatabaseSchema.md** | Complete reference | ~500 lines | Everyone |
| **DatabaseSchema-QuickRef.md** | Fast lookup | ~200 lines | Developers |
| **SCHEMA_ENHANCEMENTS.md** | Meta-documentation | ~200 lines | Maintainers |
| **.github/copilot-instructions.md** | Project context | Summary | Copilot + Team |

All together, they provide:
- ✅ Visual ER diagrams
- ✅ Complete table reference
- ✅ Enum definitions
- ✅ Business rules and constraints
- ✅ Ready-to-use query patterns
- ✅ Quick reference guide
- ✅ Schema validation tools
- ✅ Performance tips
- ✅ Context for AI assistance

---

## Questions?

- **"How do I query X?"** → DatabaseSchema-QuickRef.md Common Lookups
- **"What columns does table Y have?"** → DatabaseSchema.md Table Definitions
- **"What are the business rules?"** → DatabaseSchema-QuickRef.md Critical Constraints
- **"Can I see example queries?"** → DatabaseSchema.md Common Query Patterns
- **"How are relationships structured?"** → DatabaseSchema.md Mermaid Diagram

Happy querying! 🎯
