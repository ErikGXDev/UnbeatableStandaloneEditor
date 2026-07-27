using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace osu.Game.Rulesets.UMania.Edit.Setup;

public class UbExportHelper
{
    // Quotes! Quotes of the day!

    public class Quote
    {
        public string quoteAuthor;
        public string quoteText;

        public Quote(string author, string text)
        {
            quoteAuthor = author;
            quoteText = text;
        }
    }
    
    private static List<Quote>? quotes = JsonConvert.DeserializeObject<List<Quote>>("[{\"quoteText\":\"To see things in the seed, that is genius.\",\"quoteAuthor\":\"Lao Tzu\"},{\"quoteText\":\"Whoever is happy will make others happy, too.\",\"quoteAuthor\":\"Mark Twain\"},{\"quoteText\":\"Be as you wish to seem.\",\"quoteAuthor\":\"Socrates\"},{\"quoteText\":\"The heart has eyes which the brain knows nothing of.\",\"quoteAuthor\":\"Charles Perkhurst\"},{\"quoteText\":\"You can't stop the waves, but you can learn to surf.\",\"quoteAuthor\":\"Jon Kabat-Zinn\"},{\"quoteText\":\"Be great in act, as you have been in thought.\",\"quoteAuthor\":\"William Shakespeare\"},{\"quoteText\":\"Imagination is the highest kite one can fly.\",\"quoteAuthor\":\"Lauren Bacall\"},{\"quoteText\":\"I have done my best: that is about all the philosophy of living one needs.\",\"quoteAuthor\":\"Lin-yutang\"},{\"quoteText\":\"I'm not afraid of storms, for I'm learning how to sail my ship.\",\"quoteAuthor\":\"Louisa Alcott\"},{\"quoteText\":\"As you think, so shall you become.\",\"quoteAuthor\":\"Bruce Lee\"},{\"quoteText\":\"Our passion is our strength.\",\"quoteAuthor\":\"Billie Armstrong\"},{\"quoteText\":\"What we see depends mainly on what we look for.\",\"quoteAuthor\":\"John Lubbock\"},{\"quoteText\":\"Learning is a treasure that will follow its owner everywhere\",\"quoteAuthor\":\"Chinese proverb\"},{\"quoteText\":\"Talk doesn't cook rice.\",\"quoteAuthor\":\"Chinese proverb\"},{\"quoteText\":\"A gem cannot be polished without friction, nor a man perfected without trials.\",\"quoteAuthor\":\"Chinese proverb\"},{\"quoteText\":\"Good actions give strength to ourselves and inspire good actions in others.\",\"quoteAuthor\":\"Plato\"},{\"quoteText\":\"The greatest way to live with honour in this world is to be what we pretend to be.\",\"quoteAuthor\":\"Socrates\"},{\"quoteText\":\"Wisdom begins in wonder.\",\"quoteAuthor\":\"Socrates\"},{\"quoteText\":\"From wonder into wonder existence opens.\",\"quoteAuthor\":\"Lao Tzu\"},{\"quoteText\":\"He who deliberates fully before taking a step will spend his entire life on one leg.\",\"quoteAuthor\":\"Chinese proverb\"},{\"quoteText\":\"Great talent finds happiness in execution.\",\"quoteAuthor\":\"Johann Wolfgang von Goethe\"},{\"quoteText\":\"If one does not know to which port is sailing, no wind is favorable.\",\"quoteAuthor\":\"Seneca\"},{\"quoteText\":\"Mountains cannot be surmounted except by winding paths.\",\"quoteAuthor\":\"Johann Wolfgang von Goethe\"},{\"quoteText\":\"Kindness is the language which the deaf can hear and the blind can see.\",\"quoteAuthor\":\"Mark Twain\"},{\"quoteText\":\"When you realize there is nothing lacking, the whole world belongs to you.\",\"quoteAuthor\":\"Lao Tzu\"},{\"quoteText\":\"Logic will get you from A to B. Imagination will take you everywhere.\",\"quoteAuthor\":\"Albert Einstein\"},{\"quoteText\":\"At the center of your being you have the answer; you know who you are and you know what you want.\",\"quoteAuthor\":\"Lao Tzu\"},{\"quoteText\":\"Most folks are about as happy as they make up their minds to be.\",\"quoteAuthor\":\"Abraham Lincoln\"},{\"quoteText\":\"No act of kindness, no matter how small, is ever wasted.\",\"quoteAuthor\":\"Aesop\"},{\"quoteText\":\"Du bist was du isst oder so\",\"quoteAuthor\":\"feloex\"},{\"quoteText\":\"Wisdom is the supreme part of happiness.\",\"quoteAuthor\":\"Sophocles\"},{\"quoteText\":\"Be kind whenever possible. It is always possible.\",\"quoteAuthor\":\"Dalai Lama\"}]");

    public static string[] GetQuoteOfTheDay()
    {
        
        if (quotes == null || quotes.Count == 0)
            return [];
        
        int index = DateTime.Now.Day;
        
        var quote = quotes[Math.Abs(index) % quotes.Count];
        
        // Split quote into multiple lines if it exceeds 50 chars
        List<string> lines = new List<string>();
        string[] words = quote.quoteText.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            if (currentLine.Length + word.Length + 1 <= 50)
            {
                currentLine += word + " ";
            }
            else
            {
                lines.Add(currentLine.Trim());
                currentLine = word + " ";
            }
        }

        lines.Add(currentLine.Trim());
        
        return new [] {"// Quote of the day:","// \"" + string.Join("\n// ", lines)+"\"","// - " + quote.quoteAuthor, "" };
    }
}