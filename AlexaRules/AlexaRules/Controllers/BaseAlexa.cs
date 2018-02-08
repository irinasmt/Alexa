using AlexaRules.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;

namespace AlexaRules.Controllers
{
   
    public class BaseAlexa
    {
        public string FilePath;
        public BaseAlexa(string filePath)
        {
           FilePath = filePath;
        }
        public dynamic Index(AlexaRequest alexaRequest)
        {
            var totalSecons = (DateTime.UtcNow - alexaRequest.Request.Timestamp).TotalSeconds;
            if (totalSecons < 0 || totalSecons > 150)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }

            var request = new Request
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
                Slots = alexaRequest.Request.Intent.Slots,
                DateCreated = DateTime.UtcNow
            };

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

        public AlexaResponse IntentRequestHandler(Request request)
        {
            AlexaResponse response = null;

            switch (request.Intent)
            {
                case "BeginnerIntent":
                    response = new IntentRequestHandler(request, DifficultyLevelEnum.Beginner, FilePath).GetQuestion();
                    break;
                case "IntermediateIntent":
                    response = new IntentRequestHandler(request, DifficultyLevelEnum.Intermdiate,FilePath).GetQuestion();
                    break;
                case "AdvancedIntent":
                    response = new IntentRequestHandler(request, DifficultyLevelEnum.Advanced, FilePath).GetQuestion();
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
            AlexaResponse response = new AlexaResponse();
            response.Response.Card.Content = "Each week Alexa asks 5 new quesions. Just say: 'Alexa start C sharp question' and then pick a category from beginner and inetermediate.";
            response.Response.ShouldEndSession = false;
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.OutputSpeech.Ssml = "<speak>To use the C sharp test skill, you can say, Alexa, ask C sharp test for beginner, or you can say, Alexa, ask C sharp test for intermmediate. To repeat a question you can say, repeat. You can also say, Alexa, stop or Alexa, cancel, at any time to exit the c sharp test skill. For now, which one do you want to hear, beginner or intermediate questions?</speak>";
            return response;
        }

        private AlexaResponse CancelOrStopIntentHandler(Request request)
        {
            return new AlexaResponse("<speak>Thanks for listening, let's talk again soon.</speak>", true);
        }

        public AlexaResponse LaunchRequestHandler(Request request)
        {
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Ssml = "<speak>To use the C sharp test skill, you can say, Alexa, ask C sharp test for beginner, or you can say, Alexa, ask C sharp test for intermmediate. To repeat a question you can say, repeat. You can also say, Alexa, stop or Alexa, cancel, at any time to exit the c sharp test skill. For now, which one do you want to hear, beginner or intermediate questions?</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Session.MemberId = request.MemberId;
            response.Response.Reprompt.OutputSpeech.Ssml = "<speak>Please select one, beginner or intermediate?</speak>";
            response.Response.Card.Content = "Each week Alexa asks 5 new quesions. Just say: 'Alexa, start C Sharp Test' and then pick a category from beginner and inetermediate.";
            response.Response.ShouldEndSession = false;

            return response;
        }

        private AlexaResponse SessionEndedRequestHandler(Request request)
        {
            return null;
        }
        

    }

    public enum DifficultyLevelEnum
    {
        Beginner,
        Intermdiate,
        Advanced
    }

    public class IntentRequestHandler
    {
        private Request _request;
        private DifficultyLevelEnum _dificultyLevel;
        private string _filePath;

        public IntentRequestHandler(Request request, DifficultyLevelEnum dificultyLevel, string filePath)
        {
            _request = request;
            _dificultyLevel = dificultyLevel;
            _filePath = filePath;
        }

        public AlexaResponse GetQuestion()
        {
            AlexaResponse response = new AlexaResponse();

            var questions = GetTheQuestionsForThisWeek();
            Question nextQuestion = null;

            var firstSlotWithoutValue = _request.SlotsList.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Value));
            nextQuestion = questions.questions.Find(x => x.slotIdentifier == firstSlotWithoutValue.Key);

            var lastSlotWithValue = _request.SlotsList.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.Value));
            var previousQuestion = questions.questions.Find(x => x.slotIdentifier == lastSlotWithValue.Key);

           
            List<GetResponseRule> _rules = new List<GetResponseRule>();
            _rules.Add(new UserJustStartedTheSkillRule(nextQuestion));
            _rules.Add(new HasTheUserAskedForRepeatRule(lastSlotWithValue.Value, previousQuestion));
            _rules.Add(new IsAnswerValidRule(lastSlotWithValue.Value, previousQuestion));
            _rules.Add(new UserIsAtTheLastQuestionRule(previousQuestion, lastSlotWithValue.Value, questions.questions));
            _rules.Add(new UserIsInTheMiddleOfTheSkill(previousQuestion,nextQuestion, lastSlotWithValue.Value));

            foreach (var rule in _rules)
            {
                response = rule.CalculateResponseForAlexa(_request);
                if (response != null)
                {
                    return response;
                }
            }

            return null;
        }

       

        public QuestionsPerWeek GetTheQuestionsForThisWeek()
        {
            RootObject listOfQuestions = DeserializeJson();
            QuestionsPerWeek result = new QuestionsPerWeek();
            switch (_dificultyLevel)
            {
                case DifficultyLevelEnum.Beginner:
                    result = QuestionsPerWeek(listOfQuestions.beginner);
                    break;
                case DifficultyLevelEnum.Intermdiate:
                    result = QuestionsPerWeek(listOfQuestions.intermediate);
                    break;
                    //case DifficultyLevel.Advanced:
                    //    result = QuestionsPerWeek(listOfQuestions.advanced);
                    //    break;
            }
            return result;
        }

        private static QuestionsPerWeek QuestionsPerWeek(List<QuestionsPerWeek> listOfQuestions)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;
            string format = "dd/MM/yyyy";
            return listOfQuestions
                                    .Where(x => DateTime.ParseExact(x.startDate, format, provider).Date <= DateTime.Now.Date
                                            && DateTime.ParseExact(x.endDate, format, provider).Date >= DateTime.Now.Date).First();
        }

        private RootObject DeserializeJson()
        {
            RootObject items;
            string path = _filePath;

            using (StreamReader r = File.OpenText(path))
            {
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<RootObject>(json);
            }
            return items;

        }

    }
}