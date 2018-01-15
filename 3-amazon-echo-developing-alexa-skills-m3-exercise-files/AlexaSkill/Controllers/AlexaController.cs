using AlexaSkill.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Http;
using static AlexaSkill.Data.AlexaRequest.RequestAttributes;
using static AlexaSkill.Data.AlexaResponse.ResponseAttributes;

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
                    response = IntentRequestHandler(alexaRequest);
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

        private AlexaResponse IntentRequestHandler(AlexaRequest request)
        {
            AlexaResponse response = null;

            switch (request.Request.Intent.Name)
            {
                case "BeginnerIntent":
                    response = GetNextQuestion(request, DifficultyLevel.Beginner);
                    break;
                case "IntermediateIntent":
                    response = GetNextQuestion(request, DifficultyLevel.Intermdiate);
                    break;
                case "AdvancedIntent":
                    response = GetNextQuestion(request, DifficultyLevel.Advanced);
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

        private AlexaResponse HelpIntent(AlexaRequest request)
        {
            var response = new AlexaResponse("To use the Plural sight skill, you can say, Alexa, ask Plural sight for top courses, to retrieve the top courses or say, Alexa, ask Plural sight for the new courses, to retrieve the latest new courses. You can also say, Alexa, stop or Alexa, cancel, at any time to exit the Plural sight skill. For now, do you want to hear the Top Courses or New Courses?", false);
            response.Response.Reprompt.OutputSpeech.Text = "Please select one, top courses or new courses?";
            return response;
        }

        private AlexaResponse GetNextQuestion(AlexaRequest request, DifficultyLevel level)
        {
            var questions = GetTheQuestionsForThisWeek(level);
            AlexaResponse response = new AlexaResponse();
            var slots = request.Request.Intent.GetSlots();
           
            if (UserJustStartedTheQuestions(request.Request.Intent))
            {
                var firstQuestion = questions.questions.First();
                response.Response.OutputSpeech.Text = firstQuestion.text + firstQuestion.answers;
                DirectivesAttributes directive = CreateDirective(request,"one");
                response.Response.Directives.Add(directive);
            }
            else
            {
                var usersAnswer = slots[0].Value;
                var questionsIndex = slots[0].Key.Remove(7);
                if(IsTheAnswerCorrect(questions.questions, questionsIndex, usersAnswer))
                {

                }
                else
                {

                }
            }

         
            
            return response;
        }

        #region Helper Methods for Getting thw next question
        private  DirectivesAttributes CreateDirective(AlexaRequest request, string slotNumber)
        {
            var directive = new DirectivesAttributes();
            directive.SlotToElicit = "Question"+ slotNumber;
            directive.Type = "Dialog.ElicitSlot";
            directive.UpdatedIntentAttributes.Name = request.Request.Intent.Name;
            directive.UpdatedIntentAttributes.Slots = request.Request.Intent.Slots;
            return directive;
        }

        private bool UserJustStartedTheQuestions(IntentAttributes intent)
        {
            return intent.GetSlots().Count == 0;

        }

        private bool IsTheAnswerCorrect(List<Question> questions, string index, string usersAnswer)
        {
            return questions.First(x => x.index == index).correctAnswer == usersAnswer;

        }

        private string GetTheCorrectAnswer(List<Question> questions, string index)
        {
            return questions.First(x => x.index == index).correctAnswer;
        }

        private QuestionsPerWeek GetTheQuestionsForThisWeek(DifficultyLevel level)
        {
            RootObject listOfQuestions = DeserializeJson();
            QuestionsPerWeek result = new QuestionsPerWeek();
            switch (level)
            {
                case DifficultyLevel.Beginner:
                    result = listOfQuestions.beginner
                        .Where(x =>Convert.ToDateTime(x.startDate) <= DateTime.Now 
                                && Convert.ToDateTime(x.endDate) >= DateTime.Now).First();
                    break;
                case DifficultyLevel.Intermdiate:
                    result =  listOfQuestions.intermediate
                       .Where(x => Convert.ToDateTime(x.startDate) <= DateTime.Now
                               && Convert.ToDateTime(x.endDate) >= DateTime.Now).First();
                    break;
                case DifficultyLevel.Advanced:
                    result= listOfQuestions.advanced
                       .Where(x => Convert.ToDateTime(x.startDate) <= DateTime.Now
                               && Convert.ToDateTime(x.endDate) >= DateTime.Now).First();
                    break;
            }
            return result;
        }
        #endregion

        private RootObject DeserializeJson()
        {
            RootObject items;
            string path = @"D:\repos\Alexa\3-amazon-echo-developing-alexa-skills-m3-exercise-files\AlexaSkill\Scripts\Questions.json";

            using (StreamReader r = File.OpenText(path))
            {
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<RootObject>(json);
            }
            return items;

        }

        private AlexaResponse CancelOrStopIntentHandler(AlexaRequest request)
        {
            return new AlexaResponse("Thanks for listening, let's talk again soon.", true);
        }

        private AlexaResponse SessionEndedRequestHandler(Request request)
        {
            return null;
        }

        enum DifficultyLevel
        {
            Beginner,
            Intermdiate,
            Advanced
        }

    }
}
