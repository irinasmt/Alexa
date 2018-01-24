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
            response.Response.Reprompt.OutputSpeech.Ssml = "Please pick one, Beginner, Intermediate, Advanced ? ";
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
            response.Response.Reprompt.OutputSpeech.Ssml = "Please select one, top courses or new courses?";
            return response;
        }

        private AlexaResponse GetNextQuestion(AlexaRequest request, DifficultyLevel level)
        {
            var questions = GetTheQuestionsForThisWeek(level);
            AlexaResponse response = new AlexaResponse();
            var slots = request.Request.Intent.GetSlots();

            if (UserJustStartedTheQuestions(request.Request.Intent))
            {
                GetTheNextQuestion(response, request, questions.questions.First(), "");
            }
            else
            {
                int userInput;
                ValidateInput(request, questions.questions[4], response, slots, out userInput);
                if (userInput == 0)
                {
                    return response;
                }
                var correctAnswerString = "";

                if (UserIsAtTheLastQuestion(request.Request.Intent))
                {
                    correctAnswerString = GetTheCorrectAnswer(questions.questions[4], userInput.ToString());
                    response.Response.OutputSpeech.Ssml += "<speak>"+correctAnswerString +"<break time='1s'/> Your total score is 5 out of 5. what a smart girl you are" + "</speak>";
                    response.Response.ShouldEndSession = true;

                }
                else
                {
                    var currentQuestionIndex = slots.Count - 1;
                    correctAnswerString = GetTheCorrectAnswer(questions.questions[currentQuestionIndex], userInput.ToString());
                    GetTheNextQuestion(response, request, questions.questions[currentQuestionIndex + 1], correctAnswerString);
                }

            }
            
            return response;
        }

        private void ValidateInput(AlexaRequest request, Question question, AlexaResponse response, List<KeyValuePair<string, string>> slots, out int userInput)
        {  
            Int32.TryParse(slots.Last().Value, out userInput);
            if (userInput == 0)
            {
                TellUserToPickANumber(response, request, question, slots);
            }
        }
       
        private void TellUserToPickANumber(AlexaResponse response, AlexaRequest request, Question question, List<KeyValuePair<string, string>> slots)
        {
            response.Response.OutputSpeech.Text = "Please pick a number between from 1 to 4.";
            response.Response.OutputSpeech.Type = "TEXT";
            response.Response.ShouldEndSession = false;
            var o = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(request.Request.Intent.Slots[slots.Last().Key].ToString());
            o.Property("value").Remove();
            request.Request.Intent.Slots[slots.Last().Key] = o;
            DirectivesAttributes directive = CreateDirectiveWithSlot(request, question.slotIdentifier);
            response.Response.Directives.Add(directive);
        }

        private void GetTheNextQuestion(AlexaResponse response, AlexaRequest request, Question question, string correctAnswerForPreviousQuestion)
        {
            response.Response.OutputSpeech.Ssml = "<speak><emphasis level=\"moderate\">" + correctAnswerForPreviousQuestion + "</emphasis><break time='1s'/> The next question is <break time='1s'/>" + question.text + "<break time='1s'/>" + question.answers +"</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.ShouldEndSession = false;
            DirectivesAttributes directive = CreateDirectiveWithSlot(request, question.slotIdentifier);
            response.Response.Directives.Add(directive);
        }

        private  string GetTheCorrectAnswer(Question question, string usersAnswer)
        {
            var theCorrectAnswer="";
            if (question != null && question.correctAnswerIndex == usersAnswer)
            {
                theCorrectAnswer = "The correct answer is: " + question.correctAnswer ;
            }

            return theCorrectAnswer;
        }

        #region Helper Methods for Getting thw next question
        private  DirectivesAttributes CreateDirectiveWithSlot(AlexaRequest request, string slotNumber)
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

        private bool UserIsAtTheLastQuestion(IntentAttributes intent)
        {
            return intent.GetSlots().Count == 5;
        }

        private QuestionsPerWeek GetTheQuestionsForThisWeek(DifficultyLevel level)
        {
            RootObject listOfQuestions = DeserializeJson();
            QuestionsPerWeek result = new QuestionsPerWeek();
            switch (level)
            {
                case DifficultyLevel.Beginner:
                    result = QuestionsPerWeek(listOfQuestions.beginner);
                    break;
                case DifficultyLevel.Intermdiate:
                    result = QuestionsPerWeek(listOfQuestions.intermediate);
                    break;
                case DifficultyLevel.Advanced:
                    result = QuestionsPerWeek(listOfQuestions.advanced);
                    break;
            }
            return result;
        }

        private static QuestionsPerWeek QuestionsPerWeek(List<QuestionsPerWeek> listOfQuestions)
        {
            return listOfQuestions
                                    .Where(x => Convert.ToDateTime(x.startDate).Date <= DateTime.Now.Date
                                            && Convert.ToDateTime(x.endDate).Date >= DateTime.Now.Date).First();
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
