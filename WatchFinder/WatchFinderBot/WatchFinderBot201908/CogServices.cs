using Microsoft.Azure.Search;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotBuilderSamples
{
    public class CogServices : ICogServices
    {
        public CogServices(IConfiguration configuration)
        {
            WatchFinderLuisRecognizer = new LuisRecognizer(
                new LuisApplication(
                    configuration["LuisAppId"], configuration["LuisEndpointKey"],
                    $"https://{configuration["LuisRegion"]}.api.cognitive.microsoft.com")
                );

            WatchFinderSearchIndexClient = new SearchIndexClient(
                configuration["AzureSearchServiceName"], configuration["AzureSearchIndexName"],
                new SearchCredentials(configuration["AzureSearchApiKey"])
                );
        }
        public LuisRecognizer WatchFinderLuisRecognizer { get; private set; }
        public SearchIndexClient WatchFinderSearchIndexClient { get; private set; }
    }

}
