using AlexaSkill.Data;
using System;
using System.Linq;
using System.Text;
using System.Web.Http;

namespace AlexaSkill.Controllers
{
    public class AlexaController : ApiController
    {
        [HttpPost, Route("api/alexa/demo")]
        public dynamic Pluralsight(AlexaRequest alexaRequest)
        {
            var request = new Requests().Create(new Data.Request
            {
                MemberId = (alexaRequest.Session.Attributes == null) ? 0 : alexaRequest.Session.Attributes.MemberId,
                Timestamp = alexaRequest.Request.Timestamp,
                Intent = (alexaRequest.Request.Intent == null) ? "" : alexaRequest.Request.Intent.Name,
                AppId = alexaRequest.Session.Application.ApplicationId,
                RequestId = alexaRequest.Request.RequestId,
                SessionId = alexaRequest.Session.SessionId,
                UserId = alexaRequest.Session.User.UserId,
                IsNew = alexaRequest.Session.New,
                Version = alexaRequest.Version,
                Type = alexaRequest.Request.Type,
                Reason = alexaRequest.Request.Reason,
                SlotsList = alexaRequest.Request.Intent.GetSlots(),
                DateCreated = DateTime.UtcNow
            });

            AlexaResponse response = null;

            switch (request.Type)
            {
                case "LaunchRequest":
                    response = LaunchRequestHandler(request);
                    break;
                case "IntentRequest":
                    response = IntentRequestHandler(request);
                    break;
                case "SessionEndedRequest":
                    response = SessionEndedRequestHandler(request);
                    break;
            }

            return response;
        }

        private AlexaResponse LaunchRequestHandler(Request request)
        {
            var response = new AlexaResponse("Hello, each week I ask 5 questions from dot net. Which level would you want the questions to be from: " +
                "Beginner, Intermediate, Advanced");
            response.Session.MemberId = request.MemberId;
            response.Response.Card.Title = "Dot Net";
            response.Response.Card.Content = "Dot net start";
            response.Response.Reprompt.OutputSpeech.Text = "Please pick one, Beginner, Intermediate, Advanced ? ";
            response.Response.ShouldEndSession = false;

            return response;
        }

        private AlexaResponse IntentRequestHandler(Request request)
        {
            AlexaResponse response = null;

            switch (request.Intent)
            {
                case "BeginnerIntent":
                    response = GetNextQuestion(request, DficultyLevel.Beginner);
                    break;
                case "IntermediateIntent":
                    response = GetNextQuestion(request, DficultyLevel.Intermdiate);
                    break;
                case "AdvancedIntent":
                    response = GetNextQuestion(request, DficultyLevel.Advanced);
                    break;
                case "AMAZON.CancelIntent":
                case "AMAZON.StopIntent":
                    response = CancelOrStopIntentHandler(request);
                    break;
                case "AMAZON.HelpIntent":
                    response = HelpIntent(request);
                    break;
            }

            return response;
        }

        private AlexaResponse HelpIntent(Request request)
        {
            var response = new AlexaResponse("To use the Plural sight skill, you can say, Alexa, ask Plural sight for top courses, to retrieve the top courses or say, Alexa, ask Plural sight for the new courses, to retrieve the latest new courses. You can also say, Alexa, stop or Alexa, cancel, at any time to exit the Plural sight skill. For now, do you want to hear the Top Courses or New Courses?", false);
            response.Response.Reprompt.OutputSpeech.Text = "Please select one, top courses or new courses?";
            return response;
        }

        private AlexaResponse GetNextQuestion(Request request, DficultyLevel level)
        {
            var response = new AlexaResponse("What is the difference between 1 and 2?",false);
 
            response.Response.Directives.SlotToElicit = "Number";
            response.Response.Directives.Type = "Dialog.ElicitSlot";
            response.Response.Directives.UpdatedIntentAttributes.Name = request.Intent;
            response.Response.Directives.UpdatedIntentAttributes.Slots = request.Slots;
            return response;
        }

        private AlexaResponse CancelOrStopIntentHandler(Request request)
        {
            return new AlexaResponse("Thanks for listening, let's talk again soon.", true);
        }

        private AlexaResponse SessionEndedRequestHandler(Request request)
        {
            return null;
        }


        enum DficultyLevel
        {
            Beginner,
            Intermdiate,
            Advanced
        }
    }
}
