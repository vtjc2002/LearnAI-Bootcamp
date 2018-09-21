## 2_Bing_Search:
Estimated Time: 45-60 minutes

Before we get into the lab, let's back up and talk about the Bing Search APIs. The Bing Search APIs add intelligent search to your app, combining hundreds of billions of webpages, images, videos, and news to provide relevant results with no ads.  

The Bing Search APIs fall into the "search" category of [Azure Cognitive Services](https://docs.microsoft.com/en-us/azure/cognitive-services/), and there are currently eight of them:  
* [Bing News Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-news-search/search-the-web): Returns a list of news articles that Bing determines are relevant to a user's query
* [Bing Web Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-web-search/overview): Returns similar results as Bing, can include web pages, images, videos, and more
* [Bing Video Search](https://docs.microsoft.com/en-us/azure/cognitive-services/Bing-Video-Search/search-the-web): Returns videos that Bing determines are relevant to a query
* [Bing Autosuggest](https://docs.microsoft.com/en-us/azure/cognitive-services/Bing-Autosuggest/get-suggested-search-terms): Lets you send a partial search query to Bing and get back a list of suggested queries that other users have search on. For example, as the user enters each character of their search term, you'd call this API and populate the search box's drop-down list with the suggested query strings.
* [Bing Entity Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-entities-search/search-the-web): Returns information about entities that Bing determines are relevant, including entities (well-known people, places or things) and places (restaurants, hotels, local businesses).
* [Bing Image Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-image-search/overview): Returns images that Bing determines are relevant to a user's query
* [Bing Visual Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-visual-search/overview): Returns insights about an image such as visually similar images, shopping sources for products found in the image, and related searches. Here's a [blog post](https://azure.microsoft.com/en-us/blog/bing-visual-search-and-entity-search-apis-for-video-apps/) and [video](https://www.youtube.com/watch?time_continue=1&v=fj1BX2INbZE).
* [Bing Custom Search](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-custom-search/overview): Specify the domains, subsites, and webpages that Bing searches, customizing search experiences for different topics  

Take a few minutes to explore some the capabilities of the various service (each link should take you to more information about the service). Discuss with your neighbor at least one things you learned that the Bing Search APIs can do that you did not know.  

Now that you have an overview of the various services that are available around search, you will implement just one of the services, to see how it might be done. The same API key can be used to access all services and they are called in similar ways, so once you can add Bing Image Search to your applications, calling other Bing Search APIs *should* be fairly straightforward.  

This lab had four parts:  

1. Create a Bing Search service
2. Update `SearchResponses`
3. Update `SearchDialog` to call Bing Image Search
4. (optional) Republish your bot


### Lab 2.1: Create a Bing Image Search service

Within the Azure Portal, click **Create a resource**, enter "bing search" in the search bar, and click **Bing Search v7->Create**.

![Bing Search: Azure Portal](./resources/assets/aportal.png)

Once you click this, you'll have to fill out a few fields as you see fit. For this lab, the "F0" free tier is sufficient. You are only able to have one Free Bing Search instance per subscription, so if you or another member on your subscription have already done this, you will need to use the "S1" pricing tier. Use the same resource group as previous labs. You can enable [Bing Statistics](https://www.bingapistatistics.com/) if you want, but we won't be exploring it in this lab. Click Create.  

![Create Bing Search](./resources/assets/aportal2.png)  


Once creation is complete, open the panel for your new Bing Search service, find the key, and store with your list of keys.  

## Lab 2.2: Update `SearchResponses`  

Before you update the dialog and the calls to Bing Search, you need to prep some messages that the bot will send to the user. The general idea is after you return the results from Azure Search, you'll give users a choice to search Bing as well. Depending on their response, you'll either confirm their search and return the results or end the dialog so they can make new searches or other requests.  

Open your PictureBot.sln in Visual Studio and navigate to `SearchResponses.cs. The first thing is to ask the user if they want to view the web results from Bing.  

Recall, in "lab02.2-building_bots", you used [prompts from the dialog library](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-prompts?view=azure-bot-service-4.0&tabs=csharp) to ask the user what they want to search for. If you don't remember, review the first part of your `SearchDialog`.  

In this lab, we'll explore using instead. Suggested actions basically allow users to click (or tap depending on device) quick replies using buttons, without having to type out a full response. There are different arguments for using suggested actions vs [cards](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-add-media-attachments?view=azure-bot-service-4.0&tabs=csharp#send-a-hero-card), but a key difference is with a suggested action, after the user makes a selection, the buttons go away and with cards they don't. This makes it so later on in the conversation, the user can't select a button that is no longer relevant to the current conversation.  

> We recommend reading more about [suggested actions](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-add-suggested-actions?view=azure-bot-service-4.0&tabs=csharp), [prompts](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-prompts?view=azure-bot-service-4.0&tabs=csharp), and [other media](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-add-media-attachments?view=azure-bot-service-4.0&tabs=csharp).  

Within `SearchResponses`, create a three new responses:
1. `ReplyWithAskBing` that uses suggested actions to ask the user if they want to view the web results from Bing.
2. `ReplyWithBingConfirmation` that tells the user you are searching Bing with what they wanted to search for. 
3. `ReplyWithBingResults` which you'll send to the user once we get the results, basically saying here are the top five results for what you searched for.
> Hint 1: [This reference may help you.](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-add-suggested-actions?view=azure-bot-service-4.0&tabs=csharp)  
> Hint 2: 2 and 3 are similar in structure to `ReplyWithSearchConfirmation`.  

If you get stuck, the solution is under **resources > code**.

## Lab 2.3: Update `SearchDialog` to call Bing Image Search
Now that you've got your responses ready, you can update our SearchDialog to ask the user if they want to search Bing, make a search to Bing if they say yes, and return the top five results from Bing in a message.  

Open `SearchDialog` and locate where the dialog ends. Replace:
```csharp
                    // end the dialog
                    await dc.End();
```
with
```csharp
                    // Asking the user if they also want to search Bing for results.
                    // You could also use ChoicePrompt from the prompt library, but we're
                    // giving you a glimpse of using Cards
                    await SearchResponses.ReplyWithAskBing(dc.Context);
```
Instead of ending the dialog, we are going to continue the dialog by asking them if they want to search Bing. Next, we have to create another step in the dialog by adding the following shell:
```csharp
                ,
                async (dc, args, next) =>
                {

                }
```
Hopefully, you've noticed by now that when we add another turn to a dialog, we can use the above shell.  

The first things we want to do when we get a response is get what the user searched for in Azure Search and get what the user responded to "Would you like to view the web results from Bing?". Add this within the `{ }`:
```csharp
                    // Add state so you know what they searched for
                    var state = UserState<UserData>.Get(dc.Context);
                    var searchText = state.searchTerms;
                    // Grab the response of if they want to search Bing
                    var searchbing = dc.Context.Activity.Text;
                    // Build a case statement to search Bing if they said Yes
                    // and end the dialog if they say No
```
Before you try to fill in the case statement, let's create a task called "SearchBing" above the commented line `//process search async`:
```csharp
        public async Task SearchBing(ITurnContext context, string searchText)
        {

        }
```
The first thing you have to do is initialize the client. Add the following to the SearchBing task:
```csharp
            //IMPORTANT: replace this variable with your Cognitive Services subscription key.
            string subscriptionKey = "YourKeyHere";
            //initialize the client
            var client = new ImageSearchAPI(new ApiKeyServiceClientCredentials(subscriptionKey));
```
Replace "YourKeyHere" with your Bing Search API key. This is where you may get some errors. You'll need to add the following to your `using` statements:
```csharp
using Microsoft.Azure.CognitiveServices.Search.ImageSearch;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models;
```
More errors? Can you fix it? Have you installed the NuGet package for ImageSearch?  

Once you've fixed the errors, the next thing is to try to call the Bing Service and store the results. If there is an error, throw an exception. Add the following:
```csharp
            //images to be returned by the Bing Image Search API
            Images imageResults = null;
            try
            {
                // Call the API and store the results
                imageResults = client.Images.SearchAsync(query: searchText).Result; //search query
            }
            catch (Exception ex)
            {
                // If there's an exception, return the error in the chat window
                await context.SendActivity("Encountered exception. " + ex.Message);
            }
``` 
Review the code so far in the SearchBing task and confirm you understand what is happening.  

Now that you have received the results from Bing, you need to parse out the top five images and put them into an attachment. For this lab, you'll use a simple media attachment, but there are [other options](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-add-media-attachments?view=azure-bot-service-4.0&tabs=csharp)). Add and review the following code:
```csharp
            // If the API returns images
            if (imageResults != null)
            {
                // Parse out the first five images from ImageResults
                // Add the content URL to a simple message attachment
                var activity = MessageFactory.Attachment(new Attachment[]
                {
                    new Attachment { ContentUrl = imageResults.Value[0].ContentUrl, ContentType = "image/png" },
                    new Attachment { ContentUrl = imageResults.Value[1].ContentUrl, ContentType = "image/png" },
                    new Attachment { ContentUrl = imageResults.Value[2].ContentUrl, ContentType = "image/png" },
                    new Attachment { ContentUrl = imageResults.Value[3].ContentUrl, ContentType = "image/png" },
                    new Attachment { ContentUrl = imageResults.Value[4].ContentUrl, ContentType = "image/png" }
                });
                // Send the activity to the user.
                await SearchResponses.ReplyWithBingResults(context, searchText);
                await context.SendActivity(activity);
            }
            else // If the API doesn't return any images
            {
                await SearchResponses.ReplyWithNoResults(context, searchText);
            }
```
The only thing missing is addressing that comment that we left unfinished earlier:
```csharp
                    // Build a case statement to search Bing if they said Yes
                    // and end the dialog if they say No

```
Add the needed code, run the bot, and test in the emulator. Your result should be something like this:

![Sample Bing Search for Dogs](./resources/assets/dogs.png)  

>Here are some hints:  
> * This will be similar in structure to how you switched between topics in the RootDialog (in PictureBot.cs)
> * Start with the structure, and fill in the yes, no, and default with comments of what you think should happen for each case. Then, try to fill it in with the responses and tasks you've created in earlier parts of the lab
> * If you get stuck, you can always check the solution file under **resources > code**.

## Lab 2.4: (optional) Republish your bot

After you're happy with your bot, you can republish it.  

Make sure the bot is stopped. Right click on the project "PictureBot" and select Publish, just as if you were going to publish the bot for the first time. Your publish profile should be there, and you can simply select "Publish" again.  

Now, you should be able to interact with your enhanced bot in WebChat.

## More resources for Bing Search APIs:
- [Bing Search .NET Samples](https://github.com/Azure-Samples/cognitive-services-dotnet-sdk-samples/tree/master/BingSearchv7)
- [Bing Web Search app Tutorial](https://docs.microsoft.com/en-us/azure/cognitive-services/bing-web-search/tutorial-bing-web-search-single-page-app)
- [ConferenceBuddy bot application that uses Bing Search and other Cognitive Services](https://github.com/Azure/ConferenceBuddy)