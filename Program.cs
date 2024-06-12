using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        // Download and parse JSON data
        var httpClient = new HttpClient();
        var jsonUrls = new[] {
            "https://raw.githubusercontent.com/Mirza-Glitch/simple-english-dictionary-api/main/meaningsJson/meanings1.json",
            "https://raw.githubusercontent.com/Mirza-Glitch/simple-english-dictionary-api/main/meaningsJson/meanings2.json"
        };

        var dictionaryData = new Dictionary<string, WordData>();

        foreach (var url in jsonUrls)
        {
            var json = await httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<Data>(json);

            if (data?.Words != null)
            {
                foreach (var entry in data.Words)
                {
                    dictionaryData[entry.Key] = entry.Value;
                }
            }
            else
            {
                Console.WriteLine($"Failed to deserialize data from URL: {url}");
            }
        }

        // Preprocess dictionary data
        var wordDictionary = PreprocessDictionaryData(dictionaryData);

        // Train the sentence construction model
        var trainingFiles = new[]
        {
            "books/book.txt",
            "books/pg11.txt",
            "books/pg55.txt",
            "books/pg100.txt",
            "books/pg11.txt",
            "books/pg514.txt",
            "books/pg844.txt",
            "books/pg1342.txt",
            "books/pg1513.txt",
            "books/pg1661.txt",
            "books/pg1727.txt",
            "books/pg2591.txt",
            "books/pg2701.txt",
            "books/pg8800.txt",
            "books/pg27558.txt",
            "books/pg64317.txt",
            "books/pg67098.txt",
            "books/pg73748.txt",
            "books/pg73753.txt",
            "books/pg73754.txt",
            "books/pg73755.txt",
            "books/pg73757.txt",
            "books/pg73758.txt",
            "books/pg73759.txt",
            "books/pg73760.txt",
            "books/pg73761.txt"
        }; // Specify the paths to your training files
        
        var sentenceTrainer = new SentenceTrainer(trainingFiles);
        sentenceTrainer.Train();
        var sentence = sentenceTrainer.GenerateSentence(10);
        // Implement sentence construction algorithm
        var naiveSentence = ConstructSentence(wordDictionary);

        // Perform sentiment analysis
        var sentimentScore = AnalyzeSentiment(sentence, wordDictionary);

        // Display results
        Console.WriteLine("Generated Sentence: " + sentence);
        Console.WriteLine("Default Generated Sentence: " + naiveSentence);
        Console.WriteLine("Sentiment Score: " + sentimentScore);
    }
    
    static Dictionary<string, List<WordData>> PreprocessDictionaryData(Dictionary<string, WordData> dictionaryData)
    {
        var wordDictionary = new Dictionary<string, List<WordData>>();

        foreach (var entry in dictionaryData)
        {
            var word = entry.Key.ToLower();

            if (!wordDictionary.ContainsKey(word))
            {
                wordDictionary[word] = new List<WordData>();
            }

            wordDictionary[word].Add(entry.Value);
        }

        return wordDictionary;
    }

    static string ConstructSentence(Dictionary<string, List<WordData>> wordDictionary)
    {
        var random = new Random();
        var sentence = string.Empty;

        // Define the sentence structure
        var sentenceStructure = new[] { "Noun", "Verb", "Adjective", "Noun" };

        foreach (var partOfSpeech in sentenceStructure)
        {
            // Get a random word with the specified part of speech
            var wordsWithPartOfSpeech = wordDictionary.Values
                .SelectMany(entries => entries)
                .SelectMany(entry => entry.Meanings)
                .Where(meaning => meaning.PartsOfSpeech.Equals(partOfSpeech, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (wordsWithPartOfSpeech.Count > 0)
            {
                var randomIndex = random.Next(0, wordsWithPartOfSpeech.Count);
                var meaning = wordsWithPartOfSpeech[randomIndex];
                var word = meaning.Definition ?? string.Empty;

                // Add the word to the sentence
                sentence += word + " ";
            }
        }

        // Capitalize the first letter and add a period at the end
        sentence = sentence.Trim();
        if (!string.IsNullOrEmpty(sentence))
        {
            sentence = char.ToUpper(sentence[0]) + sentence.Substring(1) + ".";
        }

        return sentence;
    }

    static double AnalyzeSentiment(string sentence, Dictionary<string, List<WordData>> wordDictionary)
    {
        var words = sentence.Split(' ');
        var sentimentScore = 0.0;
        var sentimentWords = 0;

        foreach (var word in words)
        {
            var lowercaseWord = word.TrimEnd('.');

            if (wordDictionary.TryGetValue(lowercaseWord, out var entries))
            {
                foreach (var entry in entries)
                {
                    foreach (var meaning in entry.Meanings)
                    {
                        var definition = meaning.Definition?.ToLower() ?? string.Empty;
                        var score = 0.0;

                        if (definition.Contains("positive"))
                        {
                            score = 1.0;
                        }
                        else if (definition.Contains("negative"))
                        {
                            score = -1.0;
                        }

                        sentimentScore += score;
                        sentimentWords++;
                    }
                }
            }
        }

        if (sentimentWords > 0)
        {
            return sentimentScore / sentimentWords;
        }
        else
        {
            return 0.0;
        }
    }
}

class SentenceTrainer
{
    private readonly string[] _trainingFiles;
    private readonly Dictionary<string, Dictionary<string, int>> _ngramCounts;

    public SentenceTrainer(string[] trainingFiles)
    {
        _trainingFiles = trainingFiles;
        _ngramCounts = new Dictionary<string, Dictionary<string, int>>();
    }

    public void Train()
    {
        foreach (var file in _trainingFiles)
        {
            var text = File.ReadAllText(file);
            var sentences = SplitIntoSentences(text);

            foreach (var sentence in sentences)
            {
                var words = Tokenize(sentence);
                UpdateNgramCounts(words);
            }
        }
    }

    private string[] SplitIntoSentences(string text)
    {
        // Use regular expressions to split the text into sentences
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");

        // Remove any empty sentences
        sentences = sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        return sentences;
    }

    private string[] Tokenize(string sentence)
    {
        // Tokenize the sentence into words
        var words = Regex.Split(sentence, @"\W+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => w.ToLower())
            .ToArray();

        return words;
    }

    private void UpdateNgramCounts(string[] words)
    {
        // Update the counts of n-gram sequences
        // Example: for bigrams
        for (int i = 0; i < words.Length - 1; i++)
        {
            var bigram = $"{words[i]} {words[i + 1]}";
            if (!_ngramCounts.ContainsKey(words[i]))
            {
                _ngramCounts[words[i]] = new Dictionary<string, int>();
            }
            if (!_ngramCounts[words[i]].ContainsKey(words[i + 1]))
            {
                _ngramCounts[words[i]][words[i + 1]] = 0;
            }
            _ngramCounts[words[i]][words[i + 1]]++;
        }
    }

    public string GenerateSentence(int maxLength)
    {
        // Generate a sentence using the trained language model
        // Example: using bigrams
        var words = new List<string>();
        var currentWord = _ngramCounts.Keys.ElementAt(new Random().Next(_ngramCounts.Count));
        words.Add(currentWord);

        while (words.Count < maxLength)
        {
            if (!_ngramCounts.ContainsKey(currentWord))
            {
                break;
            }
            var nextWords = _ngramCounts[currentWord];
            var totalCount = nextWords.Values.Sum();
            var randomCount = new Random().Next(totalCount);
            var cumulativeCount = 0;
            foreach (var entry in nextWords)
            {
                cumulativeCount += entry.Value;
                if (cumulativeCount > randomCount)
                {
                    currentWord = entry.Key;
                    words.Add(currentWord);
                    break;
                }
            }
        }

        return string.Join(" ", words);
    }
}

public class Data
{
    [JsonPropertyName("data")]
    public Dictionary<string, WordData> Words { get; set; }
}

public class Meaning
{
    [JsonPropertyName("partsOfSpeech")]
    public string PartsOfSpeech { get; set; }

    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [JsonPropertyName("relatedWords")]
    public List<string> RelatedWords { get; set; }

    [JsonPropertyName("exampleSentence")]
    public List<string> ExampleSentence { get; set; }
}

public class WordData
{
    [JsonPropertyName("WORD")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("MEANINGS")]
    public List<Meaning> Meanings { get; set; } = new List<Meaning>();

    [JsonPropertyName("ANTONYMS")]
    public List<string> Antonyms { get; set; } = new List<string>();

    [JsonPropertyName("SYNONYMS")]
    public List<string> Synonyms { get; set; } = new List<string>();
}