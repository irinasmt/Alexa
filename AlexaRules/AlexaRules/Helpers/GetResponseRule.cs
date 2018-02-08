using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static AlexaRules.Helpers.AlexaResponse.ResponseAttributes;

namespace AlexaRules.Helpers
{
    public abstract class GetResponseRule
    {
        public abstract AlexaResponse CalculateResponseForAlexa(Request request);

        public DirectivesAttributes CreateDirectiveWithSlot(Request request, string slotNumber)
        {
            var directive = new DirectivesAttributes();
            directive.SlotToElicit = slotNumber;
            directive.Type = "Dialog.ElicitSlot";
            directive.UpdatedIntentAttributes.Name = request.Intent;
            directive.UpdatedIntentAttributes.Slots = request.Slots;
            return directive;
        }

        public string GetTheCorrectAnswer(Question question, string usersAnswer)
        {
            var theCorrectAnswer = "";
            if (question != null)
            {
                if (question.correctAnswerIndex != usersAnswer)
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

        public int GetScore(List<Question> questions, List<KeyValuePair<string, string>> slots)
        {
            var score = 0;
            questions.ForEach(x => score += x.correctAnswerIndex == slots.First(s => s.Key == x.slotIdentifier).Value ? 1 : 0);

            return score;

        }

        protected void CreateCardContent(Question question, AlexaResponse response)
        {
            response.Response.Card.Content = "Question: " + question.text.Replace("<break time='1s'/>", " ") + "\n Please choose from: \n" +
                                             question.answers.Replace("<break time='1s'/>", " ").Replace(".", "\n") +
                                             "\n Correct answer: " + question.correctAnswer;
        }
    }

    public class UserJustStartedTheSkillRule : GetResponseRule
    {       
        private Question _currentQuestion;
        public UserJustStartedTheSkillRule(Question question)
        {
            _currentQuestion = question;
        }
        public override AlexaResponse CalculateResponseForAlexa(Request request)
        {
            if(request.IsNew || request.SlotsList.Count(x=>string.IsNullOrEmpty(x.Value)) == 5)
            {
                AlexaResponse response = new AlexaResponse();
                response.Response.OutputSpeech.Ssml = "<speak>" + _currentQuestion.text + "<break time='1s'/>" + _currentQuestion.answers + "</speak>";
                response.Response.OutputSpeech.Type = "SSML";
                response.Response.ShouldEndSession = false;
                DirectivesAttributes directive = CreateDirectiveWithSlot(request, _currentQuestion.slotIdentifier);
                response.Response.Directives.Add(directive);

                CreateCardContent(_currentQuestion, response);
                return response;
            }

            return null;
        }
    }

    public class HasTheUserAskedForRepeatRule : GetResponseRule
    {
        private string userInput;
        private Question currentQuestion;

        public HasTheUserAskedForRepeatRule(string userinput, Question question)
        {
            userInput = userinput;
            currentQuestion = question;
        }

        public override AlexaResponse CalculateResponseForAlexa(Request request)
        {
            if (userInput == "repeat" || userInput == "replay")
            {
                AlexaResponse response = new AlexaResponse();
                response.Response.OutputSpeech.Ssml = "<speak>" + currentQuestion.text + "<break time='1s'/>" + currentQuestion.answers + "</speak>";
                response.Response.OutputSpeech.Type = "SSML";
                response.Response.ShouldEndSession = false;
                DirectivesAttributes directive = CreateDirectiveWithSlot(request, currentQuestion.slotIdentifier);
                response.Response.Directives.Add(directive);

                return response;

            }
            return null;

        }
    }

    public class IsAnswerValidRule : GetResponseRule
    {
        private string userInput;
        private Question previousQuestion;

        public IsAnswerValidRule(string userinput, Question previousQuestion)
        {
            userInput = userinput;
            this.previousQuestion = previousQuestion;
        }

        public override AlexaResponse CalculateResponseForAlexa(Request request)
        {
            int number;
            Int32.TryParse(userInput, out number);

            if(number<= 0 || number > 4)
            {
                var response = new AlexaResponse();
                response.Response.OutputSpeech.Ssml = "<speak>Please pick a number from  1 to 4.</speak>";
                response.Response.OutputSpeech.Type = "SSML";
                response.Response.ShouldEndSession = false;
                //var o = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(request.Slots);
                //o.Property("value").Remove();
               // request.Request.Intent.Slots[slots.Last().Key] = o;
                DirectivesAttributes directive = CreateDirectiveWithSlot(request, previousQuestion.slotIdentifier);
                response.Response.Directives.Add(directive);
                return response;
            }
            return null;           
        }
    }

    public class UserIsAtTheLastQuestionRule : GetResponseRule
    {
        private string correctAnswer;
        private string userInput;
        private Question previousQuestion;
        private List<Question> listOfAllQuestions;

        public UserIsAtTheLastQuestionRule(Question previousQuestion, string input, List<Question> questions)
        {
            userInput = input;
            this.previousQuestion = previousQuestion;
            listOfAllQuestions = questions;
        }

        public override AlexaResponse CalculateResponseForAlexa(Request request)
        {
            var countOfEmptyslots = request.SlotsList.Count(x => string.IsNullOrEmpty(x.Value));
            if(countOfEmptyslots == 0)
            {
                var response = new AlexaResponse();
                correctAnswer = GetTheCorrectAnswer(previousQuestion, userInput);
                var score = GetScore(listOfAllQuestions, request.SlotsList);
                response.Response.OutputSpeech.Ssml += "<speak>" + correctAnswer + "<break time='1s'/> Your total score is " + score + " out of 5.  < emphasis level =\"moderate\">Geek</emphasis> </speak>";
                response.Response.OutputSpeech.Type = "SSML";
                response.Response.ShouldEndSession = true;

                CreateCardContent(previousQuestion, response);
                return response;

            }
            return null;
        }
    }

    public class UserIsInTheMiddleOfTheSkill : GetResponseRule
    {
        private string correctAnswer;
        private string userInput;
        private Question previousQuestion;
        private Question nextQuestion;

        public UserIsInTheMiddleOfTheSkill(Question previousQuestion, Question nextQuestion, string input)
        {
            userInput = input;
            this.previousQuestion = previousQuestion;
            this.nextQuestion = nextQuestion;
        }

        public override AlexaResponse CalculateResponseForAlexa(Request request)
        {
            var response = new AlexaResponse();

            var headerNextQuestion = " <break time='1s'/>The next question is<break time='1s'/>";
            correctAnswer = GetTheCorrectAnswer(previousQuestion, userInput);
            var text = "<emphasis level=\"moderate\">" + correctAnswer + "</emphasis> ";
            
            response.Response.OutputSpeech.Ssml = "<speak>" + text + headerNextQuestion + nextQuestion.text + "<break time='1s'/>" + nextQuestion.answers + "</speak>";
            response.Response.OutputSpeech.Type = "SSML";
            response.Response.ShouldEndSession = false;
            DirectivesAttributes directive = CreateDirectiveWithSlot(request, nextQuestion.slotIdentifier);
            response.Response.Directives.Add(directive);

            CreateCardContent(nextQuestion, response);
            return response;
        }
    }

}