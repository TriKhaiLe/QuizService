using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace QuizService.Models
{
    [FirestoreData]
    public class FirestoreQuiz
    {
        [FirestoreProperty]
        public string QuizId { get; set; }

        [FirestoreProperty]
        public string Topic { get; set; }

        [FirestoreProperty]
        public List<FirestoreQuestion> Questions { get; set; } = new List<FirestoreQuestion>();

        [FirestoreProperty]
        public string CreatedAt { get; set; }

        [FirestoreProperty]
        public string CreatedBy { get; set; }

        public Quiz ToQuiz()
        {
            var quiz = new Quiz
            {
                QuizId = QuizId,
                Topic = Topic,
                CreatedAt = CreatedAt,
                CreatedBy = CreatedBy
            };
            foreach (var q in Questions)
            {
                quiz.Questions.Add(q.ToQuestion());
            }
            return quiz;
        }

        public static FirestoreQuiz FromQuiz(Quiz quiz)
        {
            var firestoreQuiz = new FirestoreQuiz
            {
                QuizId = quiz.QuizId,
                Topic = quiz.Topic,
                CreatedAt = quiz.CreatedAt,
                CreatedBy = quiz.CreatedBy
            };
            foreach (var q in quiz.Questions)
            {
                firestoreQuiz.Questions.Add(FirestoreQuestion.FromQuestion(q));
            }
            return firestoreQuiz;
        }
    }

    [FirestoreData]
    public class FirestoreQuestion
    {
        [FirestoreProperty]
        public string QuestionId { get; set; }

        [FirestoreProperty]
        public string Content { get; set; }

        [FirestoreProperty]
        public List<string> Options { get; set; } = new List<string>();

        [FirestoreProperty]
        public int CorrectOptionIndex { get; set; }

        public Question ToQuestion()
        {
            var question = new Question
            {
                QuestionId = QuestionId,
                Content = Content,
                CorrectOptionIndex = CorrectOptionIndex
            };
            question.Options.AddRange(Options);
            return question;
        }

        public static FirestoreQuestion FromQuestion(Question question)
        {
            return new FirestoreQuestion
            {
                QuestionId = question.QuestionId,
                Content = question.Content,
                Options = new List<string>(question.Options),
                CorrectOptionIndex = question.CorrectOptionIndex
            };
        }
    }
}
