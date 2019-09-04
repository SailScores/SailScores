# SailScores


The code for the site hosted at [sailscores.com][1], a free site hosting club sailing scores.
The site is written on ASP.NET Core with a REST API.

It is optimized for a range of screen sizes and fast entry of results. The site went
live in spring of 2019 and is regularly used for [large series][2] as well as one evening events.

I've been keeping scores for our club for a few years and decided to fix the
weaknesses with available options for scorekeeping. I'm not aware of any other scoring
software that has a modern interface, supports mobile devices, is inexpensive or free, and
supports club series scoring as much as regatta scoring. SailScores scratches those itches.

SailScores top priorities:
- easy to navigate and bookmark.
- fast to use, particularly for entering new results.
- web and mobile friendly.
- Accurate Appendix A results, including proper rounding and tie-breaking.
- Additional scoring system options. Systems based on [High Point Percentage][4] and
[Appendix A][3] are currently supported, with options for custom calculations and score codes.
- Open REST API: I have plans for rich client software which will use the existing public,
straightforward API.
- Free for the foreseeable future. (At some point I might ask for help covering expenses.)
- On the roadmap: analytics for race and competitor data.

This code supports multiple tenants, including visible and hidden clubs. There is currently
no automatic creation of clubs, so you'll need to contact me for the initial setup.
Enhancements and fixes are coming weekly. Some bigger features will get worked on over the 2019-2020 winter.

Licensed with Mozilla Public License Version 2.0 : You may use this software, but
share the source for modifications that you distribute.

Contributions welcome. Feedback requested: I'm trying to keep the github issues list current with features I'm working
on; but it's nice to have help prioritizing.


Sail fast...

jamie@widernets.com

[1]: https://sailscores.com
[2]: https://sailscores.com/LHYC/2019/Wednesday%20Evenings
[3]: https://www.racingrulesofsailing.org/rules?part_id=53
[4]: https://www.ussailing.org/competition/rules-officiating/racing-rules/scoring-a-long-series/
