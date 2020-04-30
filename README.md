# SailScores


The code for the site hosted at [sailscores.com][1], a free service for hosting club sailing scores.
Written on ASP.NET Core MVC with a REST API.

It's optimized for a range of screen sizes and fast entry of results. The site went
live in spring of 2019 and is regularly used for [large series][2] as well as one evening events.

I've been keeping scores for our club for a few years and decided to fix the
weaknesses with available options for scorekeeping. I'm not aware of any other scoring
software that: 1) has a modern interface, 2) supports mobile devices, 3) is inexpensive or free, and
4) supports club series scoring as well as regatta scoring. SailScores scratches those itches.

SailScores top priorities:
- easy to navigate and bookmark.
- fast to use, particularly for entering new results.
- web and mobile friendly.
- Accurate Appendix A results, including proper rounding and tie-breaking.
- Free for the foreseeable future.
- Various niceties for scoring, like automatically including weather, but not at the expense of ease-of-use.
- Additional scoring system options. Systems based on [High Point Percentage][4] and
[Appendix A][3] are currently supported, with options for custom calculations and score codes.
- Open REST API: I have plans for rich client software which will use the existing public,
straightforward API.
- Competitor Stats.

This code supports multiple tenants, including visible and hidden clubs. This winter includes
work on some bigger features and a client app.

## License

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
