﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using WeatherBot.Model;

namespace WeatherBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {



            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient();

                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                var userMessage = activity.Text;

                string endOutput = "Hello";

                //calculate something for us to return
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hello again";
                }
                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }


                bool isWeatherRequest = true;

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    isWeatherRequest = false;
                }




                if (userMessage.Length > 9)
                {
                    if (userMessage.ToLower().Substring(0, 8).Equals("set home"))
                    {
                        string homeCity = userMessage.Substring(9);
                        userData.SetProperty<string>("HomeCity", homeCity);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        endOutput = homeCity;
                        isWeatherRequest = false;
                    }
                }


                if (userMessage.ToLower().Equals("home"))
                {
                    string homecity = userData.GetProperty<string>("HomeCity");
                    if (homecity == null)
                    {
                        endOutput = "Home city not assigned";
                        isWeatherRequest = false;
                    }
                    else
                    {
                        activity.Text = homecity;
                    }
                }



                if (userMessage.ToLower().Equals("msa"))
                {
                    Activity replyToConversation = activity.CreateReply("MSA information");

                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(
                        new CardImage(
                            url:
                            "https://cdn2.iconfinder.com/data/icons/ios-7-style-metro-ui-icons/512/MetroUI_iCloud.png"));

                    List<CardAction> cardButtons = new List<CardAction>();

                    CardAction plButton = new CardAction()
                    {
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "MSA Website"
                    };
                    cardButtons.Add(plButton);

                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Visit MSA",
                        Subtitle = "The MSA Website is here",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();

                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }



                if (!isWeatherRequest)
                {
                    // return our reply to the user
                    Activity infoReply = activity.CreateReply(endOutput);

                    await connector.Conversations.ReplyToActivityAsync(infoReply);
                }

                else
                {

                    WeatherObject.RootObject rootObject;
                    HttpClient client = new HttpClient();
                    string x =
                        await
                            client.GetStringAsync(
                                new Uri("http://api.openweathermap.org/data/2.5/weather?q=" + activity.Text +
                                        "&units=metric&APPID=ff44342b8aee174ea33f2c9344a61bff"));

                    rootObject = JsonConvert.DeserializeObject<WeatherObject.RootObject>(x);

                    string cityName = rootObject.name;
                    string temp = rootObject.main.temp + "°C";
                    string pressure = rootObject.main.pressure + "hPa";
                    string humidity = rootObject.main.humidity + "%";
                    string wind = rootObject.wind.speed + "";

                    // added fields
                    string icon = rootObject.weather[0].icon;
                    int cityId = rootObject.id;

                    // return our reply to the user
                    Activity weatherReply = activity.CreateReply($"Current weather for {cityName}");
                    weatherReply.Recipient = activity.From;
                    weatherReply.Type = "message";
                    weatherReply.Attachments = new List<Attachment>();

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://openweathermap.org/img/w/" + icon + ".png"));

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://openweathermap.org/city/" + cityId,
                        Type = "openUrl",
                        Title = "More Info"
                    };
                    cardButtons.Add(plButton);

                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = cityName + "Weather",
                        Subtitle = "Temperature " + temp + ", pressure " + pressure + ", humidity " + humidity
                                   + ", wind speeds of " + wind,
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    weatherReply.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(weatherReply);
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}