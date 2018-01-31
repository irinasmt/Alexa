using AlexaSkill.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web.Http;
using static AlexaSkill.Data.AlexaRequest.RequestAttributes;
using static AlexaSkill.Data.AlexaResponse.ResponseAttributes;

namespace AlexaSkill.Controllers
{
    public class AlexaController : ApiController
    {

        private const string ApplicationID = "amzn1.ask.skill.9306ae18-b178-432f-b89f-0d22663b65af";

        [HttpPost, Route("api/alexa/demo")]
        public dynamic Pluralsight(AlexaRequest alexaRequest)
        {
            if (alexaRequest.Session.Application.ApplicationId != ApplicationID)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }

            var totalSecons = (DateTime.UtcNow - alexaRequest.Request.Timestamp).TotalSeconds;
            if (totalSecons < 0 || totalSecons > 150)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }

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
            AlexaResponse response = new AlexaResponse();
            response.Response.OutputSpeech.Ssml = "<speak>Hello, each week I ask 5 questions from C Sharp. Which level would you want the questions to be from: " +
                "Beginner, Intermediate</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Session.MemberId = request.MemberId;
            response.Response.Reprompt.OutputSpeech.Ssml = "<speak>Please select one, beginner or intermediate?</speak>";
            response.Response.Card.Content = "Each week Alexa asks 5 new quesions. Just say: 'Alexa, start C Sharp Test' and then pick a category from beginner and inetermediate.";
            response.Response.ShouldEndSession = false;

            return response;
        }

        private AlexaResponse IntentRequestHandler(AlexaRequest request)
        {
            AlexaResponse response = null;

            switch (request.Request.Intent.Name)
            {
                case "BeginnerIntent":
                    response = GetQuestion(request, DifficultyLevel.Beginner);
                    break;
                case "IntermediateIntent":
                    response = GetQuestion(request, DifficultyLevel.Intermdiate);
                    break;
                case "AdvancedIntent":
                    response = GetQuestion(request, DifficultyLevel.Advanced);
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
            AlexaResponse response = new AlexaResponse(); 
            response.Response.Card.Content = "Each week Alexa asks 5 new quesions. Just say: 'Alexa start C sharp question' and then pick a category from beginner and inetermediate.";
            response.Response.ShouldEndSession = false;
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.OutputSpeech.Ssml = "<speak>To use the C sharp test skill, you can say, Alexa, ask C sharp test for beginner, or you can say, Alexa, ask C sharp test for intermmediate. To repeat a question you can say, repeat. You can also say, Alexa, stop or Alexa, cancel, at any time to exit the c sharp test skill. For now, which one do you want to hear, beginner or intermediate questions?</speak>";
            return response;
        }

        private AlexaResponse GetQuestion(AlexaRequest request, DifficultyLevel level)
        {
            var questions = GetTheQuestionsForThisWeek(level);
            AlexaResponse response = new AlexaResponse();

            if (UserJustStartedTheQuestions(request.Request.Intent))
            {
                GetTheNextQuestion(response, request, questions.questions.First(), "", "");
                CreateCardContent(questions.questions.First(), response);
            }
            else
            {
                var slots = request.Request.Intent.GetSlots();
                var currentQuestion = slots.Last().Key;
                QuestionsIndexEnum questionEnum = (QuestionsIndexEnum)Enum.Parse(typeof(QuestionsIndexEnum), currentQuestion);
                var currentQuestionIndex = (int)questionEnum;
                int userInput;
                 var userString =slots.Last().Value;
                if (userString == "repeat" || userString == "replay")
                {
                    GetTheNextQuestion(response, request, questions.questions[currentQuestionIndex], "", "");
                }
                else
                {
                    ValidateInput(request, questions.questions[currentQuestionIndex], response, slots, out userInput, currentQuestionIndex);
                    if (!string.IsNullOrEmpty(response.Response.OutputSpeech.Ssml))
                    {
                        return response;
                    }
                    var correctAnswerString = "";
                   
                    correctAnswerString = GetTheCorrectAnswer(questions.questions[currentQuestionIndex], userInput.ToString());

                    if (UserIsAtTheLastQuestion(request.Request.Intent))
                    {
                        var score = GetScore(questions.questions, slots);
                        response.Response.OutputSpeech.Ssml += "<speak>" + correctAnswerString + "<break time='1s'/> Your total score is " + score + " out of 5. Geek" + "</speak>";
                        response.Response.OutputSpeech.Type = "SSML";
                        response.Response.ShouldEndSession = true;

                    }
                    else
                    {
                        GetTheNextQuestion(response, request, questions.questions[currentQuestionIndex + 1], correctAnswerString, " <break time='1s'/>The next question is<break time='1s'/>");
                    }

                }
               

                CreateCardContent(questions.questions[currentQuestionIndex], response);

            }
            
            return response;
        }

        private void CreateCardContent(Question question, AlexaResponse response)
        {
            response.Response.Card.Content = "Question: " + question.text.Replace("<break time='1s'/>", " ") + "\n Please choose from: \n" + 
                question.answers.Replace("<break time='1s'/>", " ").Replace(".", "\n") + 
                "\n Correct answer: " + question.correctAnswer ;
        }

        private int GetScore(List<Question> questions, List<KeyValuePair<string, string>> slots)
        {
            var score = 0;
            questions.ForEach(x => score += x.correctAnswerIndex == slots.First(s=>s.Key == "Question" + x.slotIdentifier).Value ? 1 : 0);

            return score;

        }

        private void ValidateInput(AlexaRequest request, Question question, AlexaResponse response, List<KeyValuePair<string, string>> slots, out int userInput, int index)
        {  
            Int32.TryParse(slots[slots.Count()-1].Value, out userInput);
            if (userInput< 1 || userInput > 4)
            {
                TellUserToPickANumber(response, request, question, slots);
            }
        }
       
        private void TellUserToPickANumber(AlexaResponse response, AlexaRequest request, Question question, List<KeyValuePair<string, string>> slots)
        { 
            response.Response.OutputSpeech.Ssml = "<speak>Please pick a number from  1 to 4.</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.ShouldEndSession = false;
            var o = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(request.Request.Intent.Slots[slots.Last().Key].ToString());
            o.Property("value").Remove();
            request.Request.Intent.Slots[slots.Last().Key] = o;
            DirectivesAttributes directive = CreateDirectiveWithSlot(request, question.slotIdentifier);
            response.Response.Directives.Add(directive);
        }

        private void GetTheNextQuestion(AlexaResponse response, AlexaRequest request, Question question, string correctAnswerForPreviousQuestion, string headerNextQuestion)
        {
            var text = "";
            if(!string.IsNullOrWhiteSpace(correctAnswerForPreviousQuestion))
            {
                text = "<emphasis level=\"moderate\">" + correctAnswerForPreviousQuestion + "</emphasis> ";
            }
            response.Response.OutputSpeech.Ssml = "<speak>" + text+  headerNextQuestion + question.text + "<break time='1s'/>" + question.answers +"</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.ShouldEndSession = false;
            DirectivesAttributes directive = CreateDirectiveWithSlot(request, question.slotIdentifier);
            response.Response.Directives.Add(directive);
        }

        private  string GetTheCorrectAnswer(Question question, string usersAnswer)
        {
            var theCorrectAnswer="";
            if (question != null)
            {
                if(question.correctAnswerIndex != usersAnswer)
                {
                    theCorrectAnswer = "The correct answer is: " + question.correctAnswer;
                }
                else
                {
                    theCorrectAnswer = "Your answer is correct ";
                }
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
        #endregion

        private RootObject DeserializeJson()
        {
            RootObject items;
            string path = @"C:\inetpub\api_sonictexture_root\Scripts\Questions.json";

            using (StreamReader r = File.OpenText(path))
            {
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<RootObject>(json);
            }
            return items;

        }

        private AlexaResponse CancelOrStopIntentHandler(AlexaRequest request)
        {
            return new AlexaResponse("<speak>Thanks for listening, let's talk again soon.</speak>", true);
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

        enum QuestionsIndexEnum
        {
            QuestionFive = 4,
            QuestionFour = 3,
            QuestionThree =2,
            QuestionTwo =  1,
            QuestionOne =  0
        }

    }
}
