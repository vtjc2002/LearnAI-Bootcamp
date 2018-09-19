using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Dialogs;
using PictureBot.Responses;
using PictureBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using Microsoft.Bot.Builder.Core.Extensions;

namespace PictureBot.Dialogs
{
    public class SearchDialog : DialogContainer
    {
        public const string Id = "searchPictures";

        public static SearchDialog Instance { get; } = new SearchDialog();

        // You can start this from the parent using the dialog's ID.
        public SearchDialog() : base(Id)
        {
            // add search dialog contents here
            // Define the conversation flow using a waterfall model.
            this.Dialogs.Add(Id, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    // Add state so we know if a user has told us what they want to search for
                    var state = UserState<UserData>.Get(dc.Context);
                    // Prompt the user for what they want to search for.
                    // Instead of using SearchResponses.ReplyWithSearchRequest,
                    // we're experimenting with using text prompts
                    if (state.searchTerms == "")
                        await dc.Prompt("textPrompt", "What would you like to search for?");
                    else
                        await next();
                },
                async (dc, args, next) =>
                {
                    // Add state so we know if a user has told us what they want to search for
                    var state = UserState<UserData>.Get(dc.Context);
                    if (state.searchTerms == "")
                        state.searchTerms = args["Value"] as string;
                    var searchText = state.searchTerms;
                    await SearchResponses.ReplyWithSearchConfirmation(dc.Context, searchText);
                    // Process the search request and send the results to the user
                    await StartAsync(dc.Context, searchText);

                    // end the dialog
                    await dc.End();

                }
            });
            // Define the prompts used in this conversation flow.
            this.Dialogs.Add("textPrompt", new TextPrompt());
        }
        // process search below
        public async Task StartAsync(ITurnContext context, string searchText)
        {
            ISearchIndexClient indexClientForQueries = CreateSearchIndexClient();
            // For more examples of calling search with SearchParameters, see
            // https://github.com/Azure-Samples/search-dotnet-getting-started/blob/master/DotNetHowTo/DotNetHowTo/Program.cs.  
            // Call the search service and store the results
            DocumentSearchResult results = await indexClientForQueries.Documents.SearchAsync(searchText);
            await SendResultsAsync(context, searchText, results);
        }

        public async Task SendResultsAsync(ITurnContext context, string searchText, DocumentSearchResult results)
        {
            IMessageActivity activity = context.Activity.CreateReply();
            // if the search returns no results
            if (results.Results.Count == 0)
            {
                await SearchResponses.ReplyWithNoResults(context, searchText);
            }
            else // this means there was at least one hit for the search
            {
                // create the response with the result(s) and send to the user
                SearchHitStyler searchHitStyler = new SearchHitStyler();
                searchHitStyler.Apply(
                    ref activity,
                    "Here are the results that I found:",
                    results.Results.Select(r => ImageMapper.ToSearchHit(r)).ToList().AsReadOnly());

                await context.SendActivity(activity);
            }
        }

        public ISearchIndexClient CreateSearchIndexClient()
        {
            // Configure the search service and establish a connection, call it in StartAsync()
            // replace "YourSearchServiceName" and "YourSearchServiceKey" with your search service values
            string searchServiceName = "YourSearchServiceName";
            string queryApiKey = "YourSearchServiceKey";
            string indexName = "images";
            // if you named your index "images" as instructed, you do not need to change this value

            SearchIndexClient indexClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(queryApiKey));
            return indexClient;
        }
    }
}