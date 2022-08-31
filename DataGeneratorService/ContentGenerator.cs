using System.Globalization;

namespace DataGeneratorService
{
    internal class ContentGenerator
    {
        private string[] _nouns = new string[] { "time", "year", "people", "way", "day", "man", "thing", "woman", "life", "child", "world", "school", "state", "family", "student", "group", "country", "problem", "hand", "part", "place", "case", "week", "company", "system", "program", "question", "work", "government", "number", "night", "point", "home", "water", "room", "mother", "area", "money", "story", "fact", "month", "lot", "right", "study", "book", "eye", "job", "word", "business", "issue", "side", "kind", "head", "house", "service", "friend", "father", "power", "hour", "game", "line", "end", "member", "law", "car", "city", "community", "name", "president", "team", "minute", "idea", "kid", "body", "information", "back", "parent", "face", "others", "level", "office", "door", "health", "person", "art", "war", "history", "party", "result", "change", "morning", "reason", "research", "girl", "guy", "moment", "air", "teacher", "force", "education" };
        private string[] _verbs = new string[] { "is", "has", "does", "says", "goes", "can", "gets", "would", "makes", "knows", "will", "thinks", "takes", "sees", "comes", "could", "wants", "looks", "uses", "finds", "gives", "tells", "works", "may", "should", "calls", "tries", "asks", "needs", "feels", "becomes", "leaves", "puts", "means", "keeps", "lets", "begins", "seems", "helps", "talks", "turns", "starts", "might", "shows", "hears", "plays", "runs", "moves", "likes", "lives", "believes", "holds", "brings", "happens", "must", "writes", "provides", "sits", "stands", "loses", "pays", "meets", "includes", "continues", "sets", "learns", "changes", "lead", "understands", "watches", "follows", "stops", "creates", "speaks", "reads", "allows", "adds", "spends", "grows", "opens", "walks", "wins", "offers", "remembers", "loves", "considers", "appears", "buys", "waits", "serves", "dies", "sends", "expects", "builds", "stays", "falls", "cuts", "reaches", "kills", "remains" };
        private string[] _conjunctions = new string[] { "and", "or", "if", "because" };
        private string[] _prepositions = new string[] { "to", "of", "in", "for", "on", "with", "at", "by", "from", "out", "into", "now", "over", "after" };
        private string[] _articles = new string[] { "the", "a", "every", "some" };

        private Random _random = new Random();

        public string MakeContent()
        {
            int seed = _random.Next(1_000_000_000, 2_000_000_000);
            string firstArticle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_articles[seed % _articles.Length].ToLower());

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string firstNoun = _nouns[seed % _nouns.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string firstVerb = _verbs[seed % _verbs.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string firstPreposition = _prepositions[seed % _prepositions.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string secondArticle = _articles[seed % _articles.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string secondNoun = _nouns[seed % _nouns.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string conjunction = _conjunctions[seed % _conjunctions.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string thirdArticle = _articles[seed % _articles.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string thirdNoun = _nouns[seed % _nouns.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string secondVerb = _verbs[seed % _verbs.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string secondPreposition = _prepositions[seed % _prepositions.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string fourthArticle = _articles[seed % _articles.Length];

            seed = _random.Next(1_000_000_000, 2_000_000_000);
            string fourthNoun = _nouns[seed % _nouns.Length];

            return firstArticle + " " + firstNoun + " " + firstVerb + " " + firstPreposition + " " + secondArticle + " " + secondNoun + ", " + conjunction + " " + thirdArticle + " " + thirdNoun + " " + secondVerb + " " + secondPreposition + " " + fourthArticle + " " + fourthNoun + ".";
        }
    }
}
