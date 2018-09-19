using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using PictureBot.Models;
using PictureBot.Responses;
using PictureBot.Dialogs;
using System.Linq;
using Microsoft.Bot.Builder.Ai.LUIS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Search;

namespace PictureBot
{
    public class PictureBot : IBot
    {
        private const string RootDialog = "rootDialog";
        private DialogSet _dialogs { get; } = ComposeMainDialog();

        /// <summary>
        /// Every Conversation turn for our bot calls this method. 
        /// </summary>
        /// <param name="context">The current turn context.</param>        
        public async Task OnTurn(ITurnContext context)
        {

            if (context.Activity.Type is ActivityTypes.Message)
            {
                // Get the user and conversation state from the turn context.
                var state = UserState<UserData>.Get(context);
                var conversationInfo = ConversationState<ConversationInfo>.Get(context);

                // Establish dialog state from the conversation state.
                var dc = _dialogs.CreateContext(context, conversationInfo);

                // Continue any current dialog.
                await dc.Continue();
                // Every turn sends a response, so if no response was sent,
                // then there no dialog is currently active.
                if (!context.Responded)
                {
                    // Greet them if we haven't already
                    if (state.Greeted == "not greeted")
                    {
                        await RootResponses.ReplyWithGreeting(context);
                        await RootResponses.ReplyWithHelp(context);
                        state.Greeted = "greeted";
                    }
                    else
                    {
                        await dc.Begin(RootDialog);
                    }
                }
            }
        }

        /// <summary>
        /// Composes a main dialog for our bot.
        /// </summary>
        /// <returns>A new main dialog.</returns>
        private static DialogSet ComposeMainDialog()
        {
            var dialogs = new DialogSet();

            dialogs.Add(RootDialog, new WaterfallStep[]
            {
                // Duplicate the following row if your dialog will have 
                // multiple turns. In this case, we just have one
                async (dc, args, next) =>
                {
                    // Get the state of the conversation 
                    var conversation = ConversationState<ConversationInfo>.Get(dc.Context);
                    var state = UserState<UserData>.Get(dc.Context);
                    // clear out state.searchTerms for future searches
                    state.searchTerms = "";
                    // If Regex picks up on anything, store it
                    var recognizedIntents = dc.Context.Services.Get<IRecognizedIntents>();
                    // Based on the recognized intent, direct the conversation
                    switch (recognizedIntents.TopIntent?.Name)
                    {
                            case "search":
                                // switch to SearchDialog
                                await dc.Begin(SearchDialog.Id);
                                break;
                            case "share":
                                // respond that you're sharing the photo
                                await RootResponses.ReplyWithShareConfirmation(dc.Context);
                                break;
                            case "order":
                                // respond that you're ordering
                                await RootResponses.ReplyWithOrderConfirmation(dc.Context);
                                break;
                            case "help":
                                // show help
                                await RootResponses.ReplyWithHelp(dc.Context);
                                break;
                            default:
                            // adding app logic when Regex doesn't find an intent - consult LUIS
                            var result = dc.Context.Services.Get<RecognizerResult>(LuisRecognizerMiddleware.LuisRecognizerResultKey);
                            var topIntent = result?.GetTopScoringIntent();

                            switch ((topIntent != null) ? topIntent.Value.intent : null)
                            {
                                case null:
                                    // Add app logic when there is no result.
                                    await RootResponses.ReplyWithConfused(dc.Context);
                                    break;
                                case "None":
                                    await RootResponses.ReplyWithConfused(dc.Context);
                                    await RootResponses.ReplyWithLuisScore(dc.Context, topIntent.Value.intent, topIntent.Value.score);
                                    break;
                                case "Greeting":
                                    await RootResponses.ReplyWithGreeting(dc.Context);
                                    await RootResponses.ReplyWithHelp(dc.Context);
                                    await RootResponses.ReplyWithLuisScore(dc.Context, topIntent.Value.intent, topIntent.Value.score);
                                    break;
                                case "OrderPic":
                                    await RootResponses.ReplyWithOrderConfirmation(dc.Context);
                                    await RootResponses.ReplyWithLuisScore(dc.Context, topIntent.Value.intent, topIntent.Value.score);
                                    break;
                                case "SharePic":
                                    await RootResponses.ReplyWithShareConfirmation(dc.Context);
                                    await RootResponses.ReplyWithLuisScore(dc.Context, topIntent.Value.intent, topIntent.Value.score);
                                    break;
                                case "SearchPics":
                                    // Check if LUIS has identified the search term that we should look for.  
                                    var entity = result?.Entities;
                                    var obj = JObject.Parse(JsonConvert.SerializeObject(entity)).SelectToken("facet");
                                    // if no entities are picked up on by LUIS, go through SearchDialog
                                    if (obj == null)
                                    {
                                        await dc.Begin(SearchDialog.Id);
                                        await RootResponses.ReplyWithLuisScore(dc.Context, topIntent.Value.intent, topIntent.Value.score);
                                    }
                                    // if entities are picked up by LUIS, skip SearchDialog and process the search
                                    else
                                    {
                                        // move to initialize at beginning of Dialog set
                                        state.searchTerms = obj.ToString().Replace("\"", "").Trim(']', '[').Trim();
                                        var searchText = state.searchTerms;
                                        await RootResponses.ReplyWithLuisScore(dc.Context, topIntent.Value.intent, topIntent.Value.score);
                                        await dc.Begin(SearchDialog.Id);
                                        break;
                                    }
                                    break;
                                default:
                                    await RootResponses.ReplyWithConfused(dc.Context);
                                    break;
                            }
                            break;
                    }
                }
            });

            // Add our child dialogs (in this case just one)
            dialogs.Add(SearchDialog.Id, SearchDialog.Instance);

            return dialogs;
        }

    }
}