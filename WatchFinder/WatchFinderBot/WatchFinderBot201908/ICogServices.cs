using Microsoft.Azure.Search;
using Microsoft.Bot.Builder.AI.Luis;

namespace Microsoft.BotBuilderSamples
{
    public interface ICogServices
    {
        LuisRecognizer WatchFinderLuisRecognizer { get; }
        SearchIndexClient WatchFinderSearchIndexClient { get; }
    }
}
