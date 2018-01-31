using System;
using Newtonsoft.Json;
using static AlexaSkill.Data.AlexaRequest.RequestAttributes;
using System.Collections.Generic;

namespace AlexaSkill.Data
{
    [JsonObject]
    public class AlexaResponse
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("sessionAttributes")]
        public SessionAttributes Session { get; set; }

        [JsonProperty("response")]
        public ResponseAttributes Response { get; set; }

        public AlexaResponse()
        {
            Version = "1.0";
            Session = new SessionAttributes();
            Response = new ResponseAttributes();
        }

        public AlexaResponse(string outputSpeechText)
            : this()
        {
            Response.Card.Content = outputSpeechText;
        }

        public AlexaResponse(string outputSpeechText, bool shouldEndSession)
            : this()
        {
            Response.ShouldEndSession = shouldEndSession;

            Response.Card = null;
        }

        public AlexaResponse(string outputSpeechText, string cardContent)
            : this()
        {
            Response.Card.Content = cardContent;
        }

        [JsonObject("sessionAttributes")]
        public class SessionAttributes
        {
            [JsonProperty("memberId")]
            public int MemberId { get; set; }
        }

        [JsonObject("response")]
        public class ResponseAttributes
        {
            [JsonProperty("shouldEndSession")]
            public bool ShouldEndSession { get; set; }

            [JsonProperty("outputSpeech")]
            public OutputSpeechAttributes OutputSpeech { get; set; }

            [JsonProperty("card")]
            public CardAttributes Card { get; set; }

            [JsonProperty("reprompt")]
            public RepromptAttributes Reprompt { get; set; }

            [JsonProperty("directives")]
            public List<DirectivesAttributes> Directives { get; set; }

            public ResponseAttributes()
            {
                ShouldEndSession = true;
                OutputSpeech = new OutputSpeechAttributes();
                Card = new CardAttributes();
                Reprompt = new RepromptAttributes();
                Directives = new List<DirectivesAttributes>();
            }

            [JsonObject("outputSpeech")]
            public class OutputSpeechAttributes
            {
                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("text")]
                public string Text { get; set; }

                [JsonProperty("ssml")]
                public string Ssml { get; set; }

                public OutputSpeechAttributes()
                {
                    Type = "SSML";
                }
            }

            [JsonObject("card")]
            public class CardAttributes
            {
                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("title")]
                public string Title { get; set; }

                [JsonProperty("content")]
                public string Content { get; set; }

                public CardAttributes()
                {
                    Type = "Simple";
                    Title = "C# test. Another week another question.";
                }
            }

            [JsonObject("reprompt")]
            public class RepromptAttributes
            {
                [JsonProperty("outputSpeech")]
                public OutputSpeechAttributes OutputSpeech { get; set; }

                public RepromptAttributes()
                {
                    OutputSpeech = new OutputSpeechAttributes();
                }
            }

           
            public class DirectivesAttributes
            {
                public DirectivesAttributes()
                {
                    Type = null;
                    SlotToElicit = null;
                    UpdatedIntentAttributes = new IntentAttributes();
                }

                [JsonProperty("type")]
                public string Type { get; set; }

                [JsonProperty("slotToElicit")]
                public string SlotToElicit { get; set; }

                [JsonProperty("updatedIntent")]
                public IntentAttributes UpdatedIntentAttributes { get; set; }
            }
        }

        public void HelpIntentHandler(Request request)
        {
            throw new NotImplementedException();
        }
    }
}