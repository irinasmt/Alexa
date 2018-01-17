using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlexaSkill.Data
{
    public class Question
    {
        public string answers { get; set; }
        public string correctAnswer { get; set; }
        public string correctAnswerIndex { get; set; }
        public string slotIdentifier { get; set; }
        public string text { get; set; }
    }

    public class QuestionsPerWeek
    {
        public QuestionsPerWeek()
        {
            questions = new List<Question>();
        }
        public List<Question> questions { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
    }

    public class RootObject
    {
        public List<QuestionsPerWeek> beginner { get; set; }
        public List<QuestionsPerWeek> advanced { get; set; }
        public List<QuestionsPerWeek> intermediate { get; set; }
    }
}
