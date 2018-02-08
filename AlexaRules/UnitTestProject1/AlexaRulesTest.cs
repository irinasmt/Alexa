using System;
using NUnit.Framework;
using AlexaRules.Helpers;
using AlexaRules.Controllers;

using System.Collections.Generic;
using System.Linq;

namespace UnitTestProject1
{
    [TestFixture]
    public class AlexaRulesTest
    {
        private Request _request = new Request();
        private IntentRequestHandler _intentHandler;
        private QuestionsPerWeek _questions;
        private string filePath = @"C:\RubyQuestions.json";

        [SetUp]
        public void TestMethod1()
        {
            _request.AppId = "amzn1.ask.skill.9306ae18-b178-432f-b89f-0d22663b65af";
            _request.DateCreated = DateTime.Now;
            _request.Timestamp = DateTime.Now.AddMinutes(-1);

            _request.SlotsList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("QuestionFive", ""),
                new KeyValuePair<string, string>("QuestionOne", ""),
                new KeyValuePair<string, string>("QuestionTwo", ""),
                new KeyValuePair<string, string>("QuestionThree", ""),
                new KeyValuePair<string, string>("QuestionFour", "")
            };

        }

        [Test]
        public void WhenuserJustStartedTheSkill_ReturnFirstQuestion_Beginner()
        {
            _request.IsNew = true;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner, filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();

            var currentQuestion = _questions.questions.First(x=>x.slotIdentifier == "QuestionFive");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(currentQuestion.text ));

        }

        [Test]
        public void WhenuserAskedForBeginner_ReturnFirstQuestion_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();

            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(currentQuestion.text));

        }

        [Test]
        public void WhenUserAsksforRepeat_RepeatTheQuestion_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var index =_request.SlotsList.FindIndex(x =>  x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive","repeat");
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(currentQuestion.text));

        }

        [Test]
        public void WhenUserEnetrsAWrongInput_TellTheUserToPickANumber_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
           
            var index = _request.SlotsList.FindIndex(x => x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive", "five");
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("pick a number"));

        }

        [Test]
        public void WhenUserenetrsAWrongAnser_TellUserTheCorrectAnswer_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var index = _request.SlotsList.FindIndex(x => x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive", (Convert.ToInt32(currentQuestion.correctAnswerIndex)-1).ToString());
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("The correct answer is"));

        }

        [Test]
        public void WhenUserEntersThecorrectAnswer_TellUserThatTheAnswerIsCorrect_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var index = _request.SlotsList.FindIndex(x => x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive", currentQuestion.correctAnswerIndex);
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("Your answer is correct"));

        }

        [Test]
        public void TellUserTheNextQuestion_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            _request.SlotsList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("QuestionFive", "1"),
                new KeyValuePair<string, string>("QuestionOne", "2"),
                new KeyValuePair<string, string>("QuestionTwo", ""),
                new KeyValuePair<string, string>("QuestionThree", ""),
                new KeyValuePair<string, string>("QuestionFour", "")
            };

            var questionTwo = _questions.questions.First(x => x.slotIdentifier == "QuestionTwo");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(questionTwo.text));

        }

        [Test]
        public void WhenUserIsAtTheLastQuestion_TellUserTheCorectScore_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();

            _request.SlotsList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("QuestionFive", "1"),
                new KeyValuePair<string, string>("QuestionOne", "2"),
                new KeyValuePair<string, string>("QuestionTwo", "3"),
                new KeyValuePair<string, string>("QuestionThree", "4"),
                new KeyValuePair<string, string>("QuestionFour", "1")
            };

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("Your total score is"));
        }

        [Test]
        public void TellUserTheLastQuestion_Beginner()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Beginner,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            _request.SlotsList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("QuestionFive", "1"),
                new KeyValuePair<string, string>("QuestionOne", "2"),
                new KeyValuePair<string, string>("QuestionTwo", "3"),
                new KeyValuePair<string, string>("QuestionThree", "1"),
                new KeyValuePair<string, string>("QuestionFour", "")
            };

            var questionfour = _questions.questions.First(x => x.slotIdentifier == "QuestionFour");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(questionfour.text));

        }


        [Test]
        public void WhenuserJustStartedTheSkill_ReturnFirstQuestion_Intermediate()
        {
            _request.IsNew = true;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();

            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(currentQuestion.text));

        }

        [Test]
        public void WhenUserAsksforRepeat_RepeatTheQuestion_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var index = _request.SlotsList.FindIndex(x => x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive", "repeat");
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(currentQuestion.text));

        }

        [Test]
        public void WhenUserEnetrsAWrongInput_TellTheUserToPickANumber_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();

            var index = _request.SlotsList.FindIndex(x => x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive", "five");
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("pick a number"));

        }

        [Test]
        public void WhenUserenetrsAWrongAnser_TellUserTheCorrectAnswer_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");
            var correctAnswer = Convert.ToInt32(currentQuestion.correctAnswerIndex);
            var wrongAnswer = (correctAnswer == 1 ? 2 : correctAnswer - 1).ToString();

            var index = _request.SlotsList.FindIndex(x => x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive",wrongAnswer );
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("The correct answer is"));

        }

        [Test]
        public void WhenUserEntersThecorrectAnswer_TellUserThatTheAnswerIsCorrect_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var index = _request.SlotsList.FindIndex(x => x.Key == "QuestionFive");
            _request.SlotsList[index] = new KeyValuePair<string, string>("QuestionFive", currentQuestion.correctAnswerIndex);
            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("Your answer is correct"));

        }

        [Test]
        public void TellUserTheNextQuestion_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            _request.SlotsList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("QuestionFive", "1"),
                new KeyValuePair<string, string>("QuestionOne", "2"),
                new KeyValuePair<string, string>("QuestionTwo", ""),
                new KeyValuePair<string, string>("QuestionThree", ""),
                new KeyValuePair<string, string>("QuestionFour", "")
            };

            var questionTwo = _questions.questions.First(x => x.slotIdentifier == "QuestionTwo");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(questionTwo.text));

        }

        [Test]
        public void WhenUserIsAtTheLastQuestion_TellUserTheCorectScore_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();

            _request.SlotsList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("QuestionFive", "1"),
                new KeyValuePair<string, string>("QuestionOne", "2"),
                new KeyValuePair<string, string>("QuestionTwo", "3"),
                new KeyValuePair<string, string>("QuestionThree", "4"),
                new KeyValuePair<string, string>("QuestionFour", "1")
            };

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains("Your total score is"));
        }

        [Test]
        public void TellUserTheLastQuestion_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();
            _request.SlotsList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("QuestionFive", "1"),
                new KeyValuePair<string, string>("QuestionOne", "2"),
                new KeyValuePair<string, string>("QuestionTwo", "3"),
                new KeyValuePair<string, string>("QuestionThree", "1"),
                new KeyValuePair<string, string>("QuestionFour", "")
            };

            var questionfour = _questions.questions.First(x => x.slotIdentifier == "QuestionFour");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(questionfour.text));

        }

        [Test]
        public void WhenuserAskedForIntermediate_ReturnFirstQuestion_Intermediate()
        {
            _request.IsNew = false;
            _intentHandler = new IntentRequestHandler(_request, DifficultyLevelEnum.Intermdiate,filePath);
            _questions = _intentHandler.GetTheQuestionsForThisWeek();

            var currentQuestion = _questions.questions.First(x => x.slotIdentifier == "QuestionFive");

            var response = _intentHandler.GetQuestion();

            Assert.True(response.Response.OutputSpeech.Ssml.Contains(currentQuestion.text));

        }

    }
}
