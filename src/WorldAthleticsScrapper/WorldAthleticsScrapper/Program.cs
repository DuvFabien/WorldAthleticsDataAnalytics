using HtmlAgilityPack;
using System.Globalization;
using System.Text;
using System.Web;
using static System.Net.WebRequestMethods;

namespace WorldAthleticsScrapper

{
    internal partial class Program
    {

        private static readonly List<Discipline> _disciplines = new()
        {
            new Discipline("Men's 800m", "800-metres", "middle-long"), //worldathletics.org/records/toplists/middle-long/800-metres)utdoor/men/senior/2023
            new Discipline("Men's 1500m", "1500-metres", "middle-long"), //worldathletics.org/records/toplists/middle-long/1500-metres/outdoor/men/senior/2023
            new Discipline("Men's 5000m", "5000-metres", "middle-long"), //worldathletics.org/records/toplists/middle-long/5000-metres/outdoor/men/senior/2023
            new Discipline("Men's 5km Road Race", "5-kilometres", "road-running"), //worldathletics.org/records/toplists/road-running/5-kilometres/outdoor/men/senior/2023
            new Discipline("Men's 10,000m", "10000-metres", "middle-long"), //worldathletics.org/records/toplists/middle-long/10000-metres/outdoor/men/senior/2023
            new Discipline("Men's 10km Road Race", "10-kilometres", "road-running"), //worldathletics.org/records/toplists/road-running/10-kilometres/outdoor/men/senior/2023
            new Discipline("Men's Half Marathon", "half-marathon", "road-running"), //worldathletics.org/records/toplists/road-running/half-marathon/outdoor/men/senior/2023
            new Discipline("Men's Marathon", "marathon", "road-running") //worldathletics.org/records/toplists/road-running/marathon/outdoor/men/senior/2023
        };

        private static readonly List<int> _years = new List<int>
        {
            2004,
            2005,
            2006,
            2007,
            2008,
            2009,
            2010,
            2011,
            2012,
            2013,
            2014,
            2015,
            2016,
            2017,
            2018,
            2019,
            2020,
            2021,
            2022,
            2023
        };

        public static void Main(string[] args)
        {
            var baseUrl = "https://worldathletics.org/records/toplists/";
            foreach (var year in _years)
            {
                foreach (var discipline in _disciplines)
                {
                    for (int pageIndex = 0; pageIndex < 15; pageIndex++)
                    {
                        var url = baseUrl + discipline.Category + "/" + discipline.Code + "/outdoor/men/senior/" + year + "?regionType=world&page=" + pageIndex + "&bestResultsOnly=false";
                        var web = new HtmlWeb();
                        var doc = web.Load(url);

                        HtmlNodeCollection rows = doc.DocumentNode.SelectNodes("//tbody/tr");
                        if (rows != null && rows.Count > 0)
                        {
                            var scrappingData = ExtractScrappingData(rows, discipline);
                            foreach (var athletePerformance in scrappingData.AthletePerformances)
                            {
                                Console.WriteLine($"Athlète : {scrappingData.Athletes.First(a => a.Id == athletePerformance.AthleteId).FullName}, Performance : {athletePerformance.Mark.ToShortTimeString()}, Discipline : {discipline.Name}, Year : {year}");
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Done");
            Console.ReadKey();
        }

        private static ScrappingData ExtractScrappingData(HtmlNodeCollection rows, Discipline discipline)
        {
            List<AthletePerformance> athletePerformances = new();
            var athletes = new List<Athlete>();
            foreach (var row in rows)
            {
                var columns = row.SelectNodes("td");

                if (columns != null && columns.Count >= 9)
                {
                    Athlete athlete = GetAthleteInformation(columns);
                    athletes.Add(athlete);

                    var mark = ConvertToTimeOnly(RemoveNewLinesAndSpaces(columns[1].InnerText));
                    var venue = RemoveNewLinesAndSpaces(columns[7].InnerText);
                    var date = DateOnly.Parse(columns[8].InnerText);

                    var athletePerformance = new AthletePerformance(mark, athlete.Id, venue, date, discipline.Code);
                    athletePerformances.Add(athletePerformance);
                }
            }
            ScrappingData scrappingData = new(athletes, athletePerformances);

            return scrappingData;
        }

        private static Athlete GetAthleteInformation(HtmlNodeCollection columns)
        {
            var athleteName = RemoveDiacritics(HttpUtility.HtmlDecode(RemoveNewLinesAndSpaces(columns[2].SelectSingleNode("a").InnerText)));
            string athleteUrlName = athleteName.ToLower().Replace("  ", " ").Replace(" ", "-").Replace(".", "").Replace("ø", "o").Replace("ł", "l").Replace("đ", "d").Replace("'", "").Replace("’", "").Replace("ß", "").Replace("æ", "ae").Trim('-');
            var athleteIdContent = columns[2].SelectSingleNode("a").Attributes["href"].Value;
            foreach (var country in Nationality.CountryCodes)
            {
                athleteIdContent = athleteIdContent.Replace("/athletes/" + country.Value.ToLowerInvariant().Replace(" ", "-") + "/", "");
            }

            string athleteIdString = athleteIdContent
                            .Replace("/athletes/athlete=", "")
                            .Replace("/athletes/great-britain-ni/", "")
                            .Replace("/athletes/turkey/", "")
                            .Replace("/athletes/athlete-refugee-team/", "")
                            //.Replace("/athletes/france/", "")
                            //.Replace("/athletes/spain/", "")
                            //.Replace("/athletes/russia/", "")
                            //.Replace("/athletes/united-states/", "")
                            //.Replace("/athletes/ukraine/", "")
                            .Replace(athleteUrlName, "")
                            .Replace("masashishirotake", "")
                            .Replace("vtaly-marinich-", "")
                            .Replace("-", "")
                            .Replace(".", "")
                            .Replace("ø", "o")
                            .Replace("anatolyorzhekhovskiy", "");
                            
            var athleteId = int.Parse(athleteIdString);

            string dateOfBirthColumn = RemoveNewLinesAndSpaces(columns[3].InnerText);
            DateOnly? athleteDateOfBirth;
            if (dateOfBirthColumn.Length == 4)
            {
                athleteDateOfBirth = new DateOnly(int.Parse(dateOfBirthColumn), 1, 1);
            }
            else if (dateOfBirthColumn.Length == 0)
            {
                athleteDateOfBirth = null;
            }
            else
            {
                athleteDateOfBirth = DateOnly.Parse(dateOfBirthColumn);
            }

            var nationality = columns[4].SelectSingleNode("img").GetAttributeValue("alt", "");

            var athlete = new Athlete(athleteId, athleteName, athleteDateOfBirth, nationality);
            return athlete;
        }

        private static string RemoveNewLinesAndSpaces(string innerText)
        {
            return innerText.Replace("\n", "").TrimEnd().TrimStart();
        }

        private static TimeOnly ConvertToTimeOnly(string input)
        {
            input = input.Replace("h", "0");
            string withNoMilisecondFormat = "mm:ss";
            string withSingleMilisecondFormat = "mm:ss.f";
            string withMilisecondsFormat = "mm:ss.ff";
            string noLeadingZeroWithMilisecondsFormat = "m:ss.ff";
            if (TimeOnly.TryParse(input, out TimeOnly result))
            {
                return result;
            }
            else if (TimeOnly.TryParseExact(input, withMilisecondsFormat, out result))
            {
                return result;
            }
            else if (TimeOnly.TryParseExact(input, withSingleMilisecondFormat, out result))
            {
                return result;
            }
            else if (TimeOnly.TryParseExact(input, noLeadingZeroWithMilisecondsFormat, out result))
            {
                return result;
            }
            else if (TimeOnly.TryParseExact(input, withNoMilisecondFormat, out result))
            {
                return result;
            }
            else
            {
                throw new FormatException("Le format de la chaîne d'entrée n'est pas valide.");
            }
        }
        private static string RemoveDiacritics(string input)
        {
            // Utilise la classe StringNormalization pour remplacer les caractères spéciaux.
            string normalizedString = input.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }
    }
}