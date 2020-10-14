# SailScores


The code for [sailscores.com][1], a free service for sharing club sailing scores.

## History

I've been keeping scores for our club for a few years and decided to fix the
weaknesses with available options for scorekeeping. I'm not aware of any other scoring
software that: 1) has a modern interface, 2) supports mobile devices, 3) is inexpensive or free, and
4) supports club series scoring as well as regatta scoring.

## Features
 - easy to navigate and bookmark.
 - fast, particularly for entering new results.
 - web and mobile friendly.
 - Accurate Appendix A results, including proper rounding and tie-breaking.
 - Supports [large series][2] as well as one evening events.
 - Various niceties for scoring, like automatic weather, but not at the expense of ease-of-use.
 - Additional scoring system options. Systems based on [High Point Percentage][4] and
   [Appendix A][3] are currently supported, with options for custom calculations and score codes.
 - An open REST API: client software can use the public, straightforward API.
 - Stats for competitors and clubs.

### Technologies:
 - .NET 5
 - ASP.NET 5
 - Entity Framework Core 5
 - MS-SQL
 - Production is running on Azure App Service Linux

### Getting Started with development
_...Coming soon..._

### License

Licensed with Mozilla Public License Version 2.0 : You may use this software, but
share the source for modifications that you distribute.

## Contributing

Contributions welcome. Feedback requested: I'm trying to keep the github issues list
current with features I'm working on; but it's nice to have help prioritizing. Even
thumbs-ups for features you would like to see are tremendously useful.


Sail fast...

jamie@widernets.com

[1]: https://sailscores.com
[2]: https://sailscores.com/LHYC/2019/Wednesday%20Evenings
[3]: https://www.racingrulesofsailing.org/rules?part_id=53
[4]: https://www.ussailing.org/competition/rules-officiating/racing-rules/scoring-a-long-series/
