using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Services;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class MatchingServiceTests
{
    private readonly MatchingService _service = new MatchingService();


    [Fact]
    public void GetSuggestions_ExactMatch()
    {
        var comps = new List<Competitor>
         {
             new Competitor { Id = Guid.NewGuid(), SailNumber = "123" },
             new Competitor { Id = Guid.NewGuid(), SailNumber = "456" }
         };

        var suggestions = _service.GetSuggestions("123", comps).ToList();
        Assert.NotEmpty(suggestions);
        Assert.Equal("123", suggestions.First().MatchedText);
        Assert.Equal(1.0, suggestions.First().Confidence);
    }

    [Fact]
    public void GetSuggestions_SuffixPriority()
    {
        var comps = new List<Competitor>
         {
             new Competitor { Id = Guid.NewGuid(), SailNumber = "181" },
             new Competitor { Id = Guid.NewGuid(), SailNumber = "1181" }
         };

        var suggestions = _service.GetSuggestions("81", comps).ToList();
        Assert.NotEmpty(suggestions);
        // Best suggestion should be competitor with sailnumber181 because completeness is higher
        Assert.Equal("181", suggestions.First().Competitor.SailNumber);
    }

    [Theory]
    [InlineData("ABC-123", "ABC123")]
    [InlineData("  ABC 123  ", "ABC123")]
    public void GetSuggestions_CleansInput(string input, string expectedSailNumber)
    {
        var comps = new List<Competitor>
         {
             new Competitor { Id = Guid.NewGuid(), SailNumber = expectedSailNumber }
         };
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.NotEmpty(suggestions);
        Assert.Equal(expectedSailNumber, suggestions.First().Competitor.SailNumber);
    }

    [Theory]
    [InlineData("XYZ-999", "ABC123")]
    public void GetSuggestions_NoMatch(string input, string sailNumber)
    {
        var comps = new List<Competitor>
         {
             new Competitor { Id = Guid.NewGuid(), SailNumber = sailNumber }
         };
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.Empty(suggestions);
    }

    [Theory]
    [InlineData("Boat 1234", "")]
    public void GetSuggestions_NoMatchFromHarriet(string input, string expectedSailNumber)
    {
        var comps = GetHarriet15s();
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.Empty(suggestions.Where(s => s.Confidence > .5));
    }

    private IEnumerable<Competitor> GetHarriet15s() => new List<Competitor>
        {
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 44" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 51" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 327" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 334" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 339" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 340" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 341" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA 343" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "IRL 1020" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "IRL 334" },

        };

    [Theory]
    [InlineData("44", "USA 44")]
    [InlineData("327", "USA 327")]
    public void GetSuggestions_HarrietM15s_IgnoresCountryCode(string input, string expectedSailNumber)
    {
        var comps = GetHarriet15s();
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.NotEmpty(suggestions);
        Assert.Equal(expectedSailNumber, suggestions.First().Competitor.SailNumber);
    }

    private IEnumerable<Competitor> GetHarrietMCs() => new List<Competitor>
        {
            new Competitor { Id = Guid.Parse("a959330e-807e-470b-a0e8-00e1719091d0"), Name = "Weingartner, Dave", SailNumber = "TC07", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("27bcd73b-b5da-4abc-af69-0507aa90315b"), Name = "Seymour, Chris", SailNumber = "2091", AlternativeSailNumber = "2556", BoatName = "Prima III" },
            new Competitor { Id = Guid.Parse("f83fd5ef-99f4-4107-9241-116efb272387"), Name = "Fuller, Peter", SailNumber = "TC87", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("1255b1e5-b052-405d-a01c-142b1e32ebae"), Name = "Buchanan, Brad", SailNumber = "TC27", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("12011a0c-ee33-42bb-aa0a-15bb32b2dd88"), Name = "Reed, John", SailNumber = "TC34", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("52145bc6-9fad-4c5a-9cf3-17225e8edf20"), Name = "Prochaska, Mark", SailNumber = "TC26", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("232b1be7-a6f3-4e64-be43-1bddd439d6e0"), Name = "Loscheider/Marquardt, Steve/Jim", SailNumber = "1765", AlternativeSailNumber = null, BoatName = "Spastic" },
            new Competitor { Id = Guid.Parse("935c0c04-be52-4b76-a138-2194477d6938"), Name = "Alman, Jim", SailNumber = "2887", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("4db6deb0-de02-4ed9-abf9-21d3ed157216"), Name = "Corbishley, Alex", SailNumber = "TC81", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("4fdddfc6-d047-4173-8a3c-24cf5bc7c8d2"), Name = "Koller, Adam", SailNumber = "TC67", AlternativeSailNumber = "264", BoatName = null },
            new Competitor { Id = Guid.Parse("65b71032-9baf-414a-822f-329df0879466"), Name = "Coyne, Justin", SailNumber = "TC65", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("59d8a698-1015-4a24-aa30-37e1a1374182"), Name = "Elder, Jack", SailNumber = "TC63", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("4cfbe8ca-14e0-475f-a4b4-3d93f5f82a14"), Name = "Tennis, Jerry", SailNumber = "TC12", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("52b7a018-dac3-4c28-9be5-3f004b8a7215"), Name = "Ott, Chuck", SailNumber = "2322", AlternativeSailNumber = null, BoatName = "Sea Otter" },
            new Competitor { Id = Guid.Parse("16f6c88a-02a4-4246-bc99-43352e08e6c1"), Name = "Lees, John", SailNumber = "TC79", AlternativeSailNumber = "1", BoatName = null },
            new Competitor { Id = Guid.Parse("61586653-83f4-4d90-8559-46f4c2f30b77"), Name = "Coffey, Jeff", SailNumber = "TC29", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("6c1b1770-079d-4b66-abaa-4e4f4a9f39d2"), Name = "Morical, Keith", SailNumber = "2711", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("61cc4ac1-6383-4c76-bead-5092814c7c18"), Name = "Guidinger, Dan", SailNumber = "2653", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("888b2fa8-05d6-4248-a64e-532910d3abce"), Name = "Anderson, Mark", SailNumber = "1935", AlternativeSailNumber = null, BoatName = "Mde Oma" },
            new Competitor { Id = Guid.Parse("99677fda-6ec7-4626-b061-5b360c3d30da"), Name = "Katics, John", SailNumber = "1847", AlternativeSailNumber = null, BoatName = "Beautiful Obsession" },
            new Competitor { Id = Guid.Parse("35b745f7-4798-440d-9a80-5c518088085e"), Name = "Garcia, Joe", SailNumber = "TC76", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("5759b70b-bd62-4e9e-933d-5d0f7b0151cc"), Name = "Armstrong, \"Aru\"chunan", SailNumber = "1664", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("28b97b72-267b-4450-9f86-623f111882cf"), Name = "Poelen, Jorrit", SailNumber = "TC85", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("8ea36199-d361-468f-9af7-64bc004e122f"), Name = "Klingner, Ellen ", SailNumber = "TC84", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("7b8299dc-c91f-4655-84e1-6b2604359ca7"), Name = "Skipper Y", SailNumber = "TC?2", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("4b5ab819-3a31-4574-b79f-6d715be468b1"), Name = "Lorig, Meredith", SailNumber = "TC30", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("da754adb-40a1-4770-ba30-6e1ccd6da719"), Name = "Brunmeier, Bob", SailNumber = "TC28", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("2b33de24-04b1-4cc7-a27e-6e3b7251d8c0"), Name = "Miller, Mike", SailNumber = "2644", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("de1ee203-4142-4858-be97-7185cf893a3c"), Name = "Himelson, Adam", SailNumber = "TC38", AlternativeSailNumber = "2338", BoatName = "Happy Birthday!" },
            new Competitor { Id = Guid.Parse("1f3e12f9-f6a9-40de-9032-765718a1be15"), Name = "Pone, Imants", SailNumber = "TC11", AlternativeSailNumber = "16", BoatName = null },
            new Competitor { Id = Guid.Parse("969620f0-9575-4f8e-bd47-76af510280ea"), Name = "Berger, Kevin", SailNumber = "TC36", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("d79f5448-0a7b-4bab-a799-7d4de0a1cf0d"), Name = "Christie, Bob", SailNumber = "1746", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("368b697d-6d3b-427f-96bc-7d5f210be23f"), Name = "Macphail, Stuart", SailNumber = "1280", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("5b30071e-e04d-44e8-a139-7fec574d3c17"), Name = "Litsey, Trevor", SailNumber = "TC82", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("cec8647f-394c-46c1-8f24-847c4fc3f9a0"), Name = "Wean, Matt", SailNumber = "2674", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("1ca5082f-7b07-419a-bc92-8a5eb1df5ba3"), Name = "Franchett, Audrey", SailNumber = "TC77", AlternativeSailNumber = "214", BoatName = null },
            new Competitor { Id = Guid.Parse("2568bfe7-9292-415b-bf08-8dfd1838d16c"), Name = "Grosch, Ryan", SailNumber = "2169", AlternativeSailNumber = null, BoatName = "Black Cat" },
            new Competitor { Id = Guid.Parse("f477f96b-e20a-4536-8bba-9bc689718c02"), Name = "Fricton, Joe", SailNumber = "2511", AlternativeSailNumber = null, BoatName = "Minnehaha" },
            new Competitor { Id = Guid.Parse("0df58d3c-6961-403d-b019-a0b49e2104ce"), Name = "Silver, Laura", SailNumber = "TC05", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("510b5f58-9e9f-477b-9330-a5fd02937727"), Name = "Rob Byer", SailNumber = "TC88", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("84a06346-794b-4f41-a828-b10e2e9cae5a"), Name = "Neuman, Noel", SailNumber = "2717", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("8da11853-88be-4455-820d-b537954fcb16"), Name = "Katics/Getsinger, John/John", SailNumber = "1847", AlternativeSailNumber = null, BoatName = "Beautiful Obsession" },
            new Competitor { Id = Guid.Parse("41911f57-f548-42dc-831e-b64af9c5fab7"), Name = "Hemstad, Erik", SailNumber = "1010", AlternativeSailNumber = null, BoatName = "Naiad" },
            new Competitor { Id = Guid.Parse("75f82a59-4ed5-43e6-bf92-c3ba3d3ecb9a"), Name = "Anderson, Sarah Lindsey", SailNumber = "TC83", AlternativeSailNumber = "987", BoatName = null },
            new Competitor { Id = Guid.Parse("3520dbda-f7dc-4975-bd67-c7079d51f592"), Name = "Heinrich, Steve", SailNumber = "TC44", AlternativeSailNumber = "264", BoatName = null },
            new Competitor { Id = Guid.Parse("f9bd2f2b-363e-48e3-b12d-cedb2d49e57b"), Name = "Wadman, Shannon", SailNumber = "TC35", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("5041d936-bf11-4aa0-8ad6-cfffda914dc5"), Name = "Tanner, Ben", SailNumber = "1229", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("7e1f13ec-56bd-4edf-98be-d41269ebd6d2"), Name = "O'Toole, Phillip", SailNumber = "TC42", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("a7f4d19d-6855-46ef-a8b4-d7101efffcc1"), Name = "Skipper B", SailNumber = "TC?5", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("c1f46d7b-9c27-4c03-a776-d84951930a90"), Name = "Berg, John", SailNumber = "1850", AlternativeSailNumber = null, BoatName = "Andy" },
            new Competitor { Id = Guid.Parse("2bf12382-b105-4637-910d-d9970b8c6a28"), Name = "Newman, Darin", SailNumber = "TC25", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("4e7204cf-459e-4aab-89b6-d9f4abdcd5ff"), Name = "Wellmann, Eric", SailNumber = "2652", AlternativeSailNumber = null, BoatName = " Zut alors" },
            new Competitor { Id = Guid.Parse("1ab7a095-2638-4aaf-b262-e33ff8b1582d"), Name = "O'Brien, Joe", SailNumber = "TC37", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("9d4d026e-7847-4ca5-b69c-e81399cd9322"), Name = "Grimm, Nick", SailNumber = "1687", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("55a28490-4c6c-41db-bc9a-ea05d1399f5e"), Name = "Forrest, Whit", SailNumber = "TC74", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("011dbd1b-c116-4ebc-be16-ec07cd734e50"), Name = "Gross, Rich", SailNumber = "TC60", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("bada729c-f708-4ce3-ae5c-ef2230c85d92"), Name = "Skipper Z", SailNumber = "TC?3", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("1f308eea-ebd8-41b4-a92b-f0dee04dbfd4"), Name = "Crews-Hill, Marley", SailNumber = "TC71", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("d0463a4b-58ad-4fce-8e9d-f267effd86bf"), Name = "Skipper A", SailNumber = "TC?4", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("3ee3f69b-a049-4b4c-bee0-f2bb70a2acb1"), Name = "Grzybek, John", SailNumber = "2824", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("1d8f5bc7-5202-4975-a82f-f6d012561fb4"), Name = "Feyen, Gary", SailNumber = "TC72", AlternativeSailNumber = null, BoatName = "-various-" },
            new Competitor { Id = Guid.Parse("1321bef4-5158-4cd1-a22f-f80619126952"), Name = "Skipper X", SailNumber = "TC??", AlternativeSailNumber = null, BoatName = null },
            new Competitor { Id = Guid.Parse("0f8825ba-e6b9-4e22-8593-fc39acaafa46"), Name = "Curtis, Jason", SailNumber = "1933", AlternativeSailNumber = null, BoatName = "Humdinger" },
            new Competitor { Id = Guid.Parse("DF5A913A-9532-4B39-88F2-5FFC093C8D3A"), Name = "Zero, Bob", SailNumber = "0", AlternativeSailNumber = null, BoatName = "Zero" }
        };



    [Theory]
    [InlineData("044", "USA 44")]
    public void GetSuggestions_IgnoresLeadingZero(string input, string expectedSailNumber)
    {
        var comps = GetHarriet15s();
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.NotEmpty(suggestions);
        Assert.Equal(expectedSailNumber, suggestions.First().Competitor.SailNumber);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    public void GetSuggestions_EmptyInput(string input, int expectedCount)
    {
        var comps = new List<Competitor>
         {
             new Competitor { Id = Guid.NewGuid(), SailNumber = "123" }
         };
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.Equal(expectedCount, suggestions.Count);
    }


    [Theory]
    [InlineData("05/", "USA 51")]
    [InlineData("044", "USA 44")]
    [InlineData("327", "USA 327")]
    [InlineData("339", "USA 339")]
    [InlineData("3341", "USA 334")]
    public void GetSuggestions_HarrietHandwriting_Matches(string input, string topMatch)
    {
        var comps = GetHarriet15s();
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.Equal(topMatch, suggestions.First().Competitor.SailNumber);
    }


    [Theory]
    [InlineData("2652", "2652")]
    [InlineData("25", "TC25")]
    [InlineData("2556", "2091")]
    [InlineData("184.7", "1847")]
    [InlineData("214", "TC77")]
    [InlineData("2457", "")]
    [InlineData("2169", "2169")]
    [InlineData("1765", "1765")]
    [InlineData("2322", "2322")]
    [InlineData("264", "TC67")]
    [InlineData("935", "1935")]
    [InlineData("1280", "1280")]
    [InlineData("2126", "")]
    [InlineData("1664", "1664")]
    [InlineData("2095", "")]
    [InlineData("126", "")]
    [InlineData("0", "0")]
    public void GetSuggestions_HarrietMCHandwriting_Matches(string input, string topMatch)
    {
        var comps = GetHarrietMCs();
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        if(String.IsNullOrEmpty(topMatch))
        {
            Assert.Empty(suggestions);
        } else
        {
            Assert.Equal(topMatch, suggestions.First().Competitor.SailNumber);
        }
    }


    [Theory]
    [InlineData("123", 0)]
    public void GetSuggestions_EmptyCompetitors(string input, int expectedCount)
    {
        var comps = new List<Competitor>();
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.Equal(expectedCount, suggestions.Count);
    }
    [Theory]
    [InlineData(null, 0)]
    public void GetSuggestions_NullCompetitors(string input, int expectedCount)
    {
        List<Competitor>? comps = null;
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.Equal(expectedCount, suggestions.Count);
    }
    [Theory]
    [InlineData("Boat ABC-1234 XYZ", "ABC1234")]
    public void GetSuggestions_MultipleCandidates(string input, string expectedSailNumber)
    {
        var comps = new List<Competitor>
         {
             new Competitor { Id = Guid.NewGuid(), SailNumber = expectedSailNumber }
         };
        var suggestions = _service.GetSuggestions(input, comps).ToList();
        Assert.NotEmpty(suggestions);
        Assert.Equal(expectedSailNumber, suggestions.First().Competitor.SailNumber);
    }
}
