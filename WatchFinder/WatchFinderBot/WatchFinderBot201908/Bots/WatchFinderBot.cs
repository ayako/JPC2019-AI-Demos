// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WatchFinderBot;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Microsoft.BotBuilderSamples
{
    public class WatchFinderBot : ActivityHandler
    {
        private BotState _userState;
        private Bot.Builder.ConversationState _conversationState;
        private ILogger<WatchFinderBot> _logger;
        private ICogServices _cogServices;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public WatchFinderBot(Bot.Builder.UserState userState, Bot.Builder.ConversationState conversationState, ICogServices cogServices, ILogger<WatchFinderBot> logger, IHttpContextAccessor httpContextAccessor)
        {
            _userState = userState;
            _conversationState = conversationState;
            _logger = logger;
            _cogServices = cogServices;

            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userStateAccecor = _userState.CreateProperty<UserState>(nameof(UserState));
            var conversationStateAccecor = _conversationState.CreateProperty<ConversationState>(nameof(BotBuilderSamples.ConversationState));
            var userState = await userStateAccecor.GetAsync(turnContext, () => new UserState());
            var conversationState = await conversationStateAccecor.GetAsync(turnContext, () => new ConversationState());

            if (userState.SearchProcessStarted == false)
            {
                userState.SearchProcessStarted = true;
                await turnContext.SendActivityAsync(MessageFactory.Text($"どのような時計をお探しですか？"), cancellationToken);
            }
            else
            {
                var recognizerResult = await _cogServices.WatchFinderLuisRecognizer.RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizerResult.GetTopScoringIntent();
                await DispatchByTopIntentAsync(turnContext, topIntent.intent, recognizerResult, conversationState, cancellationToken);

            }

            await _userState.SaveChangesAsync(turnContext);
            await _conversationState.SaveChangesAsync(turnContext);

        }


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {

                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"こんにちは！Swatch Bot です。時計を探すお手伝いをします。"), cancellationToken);
                    //await turnContext.SendActivityAsync(MessageFactory.Text($"途中で分からなくなったら「ヘルプ」、最初から検索し直したいときは「リセット」と入力してくださいね。"), cancellationToken);

                    var response = MessageFactory.Text($"途中で分からなくなったら「ヘルプ」、最初から検索し直したいときは「リセット」と入力してくださいね。");
                    response.SuggestedActions = new SuggestedActions()
                    {
                        Actions = new List<CardAction>()
                        {
                            new CardAction() { Title = "OK", Type = ActionTypes.ImBack, Value = "OK" },
                        },
                    };
                    await turnContext.SendActivityAsync(response, cancellationToken);
                }
            }
        }

        private async Task DispatchByTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent, RecognizerResult recognizerResult, ConversationState conversationState, CancellationToken cancellationToken)
        {
            switch (intent)
            {
                case "Greeting":
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Swatch Finder Bot です。今日は何をお探しですか？"));
                    break;
                case "Reset":
                    await turnContext.SendActivityAsync(MessageFactory.Text($"それでは検索条件をリセットします。"));
                    await _conversationState.ClearStateAsync(turnContext);
                    break;
                case "Help":
                    await turnContext.SendActivityAsync(MessageFactory.Text($"ヘルプメニューです。"), cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"最初から検索し直したいときは「リセット」と入力してください。"), cancellationToken);
                    break;
                case "Find":
                    await FindItemAsync(turnContext, recognizerResult, conversationState, cancellationToken);
                    break;
                case "Select":
                    await SelectItemAsync(turnContext, recognizerResult, conversationState, cancellationToken);
                    break;
                case "Reserve":
                    await ReserveItemAsync(turnContext, recognizerResult, conversationState, cancellationToken);
                    await _conversationState.ClearStateAsync(turnContext);
                    break;
                default:
                    await turnContext.SendActivityAsync(MessageFactory.Text($"申し訳ありません、分かりませんでした。別の言葉で言い換えていただけますか？"), cancellationToken);
                    break;
            }
        }

        private async Task FindItemAsync(ITurnContext<IMessageActivity> turnContext, RecognizerResult recognizerResult, ConversationState conversationState, CancellationToken cancellationToken)
        {
            var gender = recognizerResult.Entities.GetValue("Gender")?.ToObject<List<List<string>>>()[0][0];
            var colors = GetResultItemList(recognizerResult.Entities.GetValue("Colors")?.ToObject<List<List<string>>>());
            var bands = GetResultItemList(recognizerResult.Entities.GetValue("Bands")?.ToObject<List<List<string>>>());
            var impressions = GetResultItemList(recognizerResult.Entities.GetValue("Impressions")?.ToObject<List<List<string>>>());
            var scenes = GetResultItemList(recognizerResult.Entities.GetValue("Scenes")?.ToObject<List<List<string>>>());

            var userPreference = conversationState.UserPreference;
            if (userPreference == null) userPreference = new UserPreference()
            {
                Gender = string.Empty,
                Colors = new List<string>(),
                Bands = new List<string>(),
                Imressions = new List<string>(),
                Scenes = new List<string>(),
            };
            if (gender != null) userPreference.Gender = gender;
            if (colors != null) userPreference.Colors.AddRange(colors);
            if (bands != null) userPreference.Bands.AddRange(bands);
            if (impressions != null) userPreference.Imressions.AddRange(impressions);
            if (scenes != null) userPreference.Scenes.AddRange(scenes);

            var searchFilter = SetSearchFilter(
                userPreference.Gender,
                userPreference.Colors,
                userPreference.Bands
                );
            var searchText = SetSearchText(
                userPreference.Imressions,
                userPreference.Scenes
                );

            var searchParamaeters = new SearchParameters() { Filter = searchFilter, IncludeTotalResultCount = true, };
            var searchResults = await _cogServices.WatchFinderSearchIndexClient.Documents.SearchWithHttpMessagesAsync<WatchProduct>(searchText, searchParamaeters);

            var searchResultsProducts = searchResults.Body.Results as List<SearchResult<WatchProduct>>;
            conversationState.UserPreference = userPreference;

            switch (searchResultsProducts.Count)
            {
                case 0:
                    await turnContext.SendActivityAsync(MessageFactory.Text($"検索結果は0件です。条件を変えて再度お伺いできますか？"));
                    break;
                case 1:
                    await SendSingleCardAsync(turnContext, searchResultsProducts, cancellationToken);
                    break;
                case int n when n <= 5:
                    await SendCarouselCardAsync(turnContext, searchResultsProducts, cancellationToken);
                    break;
                case int n when n > 5:
                    await turnContext.SendActivityAsync(MessageFactory.Text(n + $"件のお勧めがあります。絞込条件を追加してください: "));
                    if (userPreference.Gender.Length == 0)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("どなたがご利用になる時計ですか？"));
                    }
                    else if (userPreference.Colors.Count == 0)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text($"ご希望の色はありますか？"));
                    }
                    else if (userPreference.Bands.Count == 0)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text($"ご希望のバンド素材はありますか？"));
                    }
                    else if (userPreference.Imressions.Count == 0)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text($"どのようなイメージのものをお探しですか？"));
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task SelectItemAsync(ITurnContext<IMessageActivity> turnContext, RecognizerResult recognizerResult, ConversationState conversationState, CancellationToken cancellationToken)
        {
            var productName = (recognizerResult.Entities.GetValue("ProductName")?.ToObject<List<string>>()[0]).Replace("_", " ");

            var searchParamaeters = new SearchParameters() { Filter = "", IncludeTotalResultCount = true, };
            var searchText = "ProductName eq \'" + productName + "\'";
            var searchResults = await _cogServices.WatchFinderSearchIndexClient.Documents.SearchWithHttpMessagesAsync<WatchProduct>(searchText, searchParamaeters);
            var productImage = searchResults.Body.Results[0].Document.ProductImage;

            conversationState.SelectedProductName = productName;
            conversationState.SelectedProductImage = productImage;

            await turnContext.SendActivityAsync(MessageFactory.Text($"有難うございます！\n" +
                            $"店舗にてお取り置きしておきますので、携帯電話番号をお伺いできますか。"), cancellationToken);

        }

        private async Task ReserveItemAsync(ITurnContext<IMessageActivity> turnContext, RecognizerResult recognizerResult, ConversationState conversationState,CancellationToken cancellationToken)
        {
            var phoneNumber = recognizerResult.Text;
            var productName = conversationState.SelectedProductName;
            var productImage = conversationState.SelectedProductImage;

            conversationState.PhoneNumber = phoneNumber;


            await turnContext.SendActivityAsync($"お取り置きを承りました。\n" +
                $"店舗にてご予約票をご提示ください。");

            var webhostName = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
            var reservationUrl = webhostName
                + "/reservation.html?productName=" + productName
                + "&productImage=" + productImage
                + "&phoneNumber=" + phoneNumber;

            var response = MessageFactory.Attachment(
                new Attachment[]
                {
                    new HeroCard(
                        buttons: new CardAction[]
                        {
                            new CardAction(title: "ご予約票", type: ActionTypes.OpenUrl, value: reservationUrl),
                        }
                    ).ToAttachment(),
                }
            );
            await turnContext.SendActivityAsync(response, cancellationToken);

        }

        private List<string> GetResultItemList(List<List<string>> resultList)
        {
            if (resultList != null)
            {
                var resultItemList = new List<string>();
                for (int i = 0; i < resultList.Count; i++)
                {
                    resultItemList.Add(resultList[i][0]);
                }
                return resultItemList;
            }
            else
            {
                return null;
            }
        }

        private string SetSearchFilter(string gender, List<string> colors, List<string> bands)
        {
            var filterList = new List<string>();

            if (gender.Length > 0)
            {
                var filter = string.Empty;
                switch (gender)
                {
                    case "Mens":
                        filter = "(CaseDimensions/width ge 34)";
                        break;
                    case "Womens":
                        filter = "(CaseDimensions/width le 34)";
                        break;
                }
                filterList.Add(filter);
            }

            if (colors.Count > 0)
            {
                var filter = string.Empty;
                foreach (var item in colors)
                {
                    filter += " or Color eq \'" + item + "\'";
                }
                filter = "(" + filter.Substring(4) + ")";
                filterList.Add(filter);
            }

            if (bands.Count > 0)
            {
                var filter = string.Empty;
                foreach (var item in bands)
                {
                    filter += " or StrapMaterial eq \'" + item + "\'";
                }
                filter = "(" + filter.Substring(4) + ")";
                filterList.Add(filter);
            }

            var searchFilter = string.Empty;
            if (filterList.Count > 0)
            {
                foreach (var item in filterList)
                {
                    searchFilter += " and " + item;
                }
                searchFilter = searchFilter.Substring(5);
            }

            return searchFilter;
        }

        private string SetSearchText(List<string> impressions, List<string> scenes)
        {
            var textList = new List<string>();
            if (impressions != null) textList.AddRange(impressions);
            if (scenes != null) textList.AddRange(scenes);

            var searchText = string.Empty;
            if (textList.Count > 0)
            {
                foreach (var item in textList)
                {
                    searchText += " " + item;
                }
                searchText = searchText.TrimStart();
            }
            else
            {
                searchText = "*";
            }

            return searchText;
        }

        private async Task SendSingleCardAsync(ITurnContext<IMessageActivity> turnContext, List<SearchResult<WatchProduct>> searchResults, CancellationToken cancellationToken)
        {
            var response = MessageFactory.Attachment(
                new Attachment[]
                {
                    new HeroCard(
                        title: searchResults[0].Document.ProductName,
                        images: new CardImage[] { new CardImage(url: searchResults[0].Document.ProductImage) },
                        buttons: new CardAction[]
                        {
                            new CardAction(title: "予約", type: ActionTypes.ImBack, value: (searchResults[0].Document.ProductName).Replace(" ","_") + " を予約"),
                            new CardAction(title: "詳細", type: ActionTypes.OpenUrl, value: searchResults[0].Document.ProductUrl)
                        }
                    ).ToAttachment(),
                });

            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        private async Task SendCarouselCardAsync(ITurnContext<IMessageActivity> turnContext, List<SearchResult<WatchProduct>> searchResults, CancellationToken cancellationToken)
        {
            var attachments = new Attachment[searchResults.Count];
            for (int i = 0; i < searchResults.Count; i++)
            {
                attachments[i] = new HeroCard(
                    title: searchResults[i].Document.ProductName,
                    images: new CardImage[] { new CardImage(url: searchResults[i].Document.ProductImage) },

                    buttons: new CardAction[]
                    {
                        new CardAction(title: "予約", type: ActionTypes.ImBack, value: (searchResults[i].Document.ProductName).Replace(" ","_") + " を予約"),
                        new CardAction(title: "詳細", type: ActionTypes.OpenUrl, value: searchResults[i].Document.ProductUrl)
                    }
                ).ToAttachment();
            }

            var response = MessageFactory.Carousel(attachments);
            await turnContext.SendActivityAsync(response, cancellationToken);
        }
    }
}