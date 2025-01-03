using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuizService.Models;

namespace QuizService.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _db;
        private const string QuizCollection = "quizzes";

        public FirebaseService(string projectId, string credentialsPath)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            _db = FirestoreDb.Create(projectId);
        }

        public async Task<string> CreateQuizAsync(Quiz quiz)
        {
            try
            {
                var firestoreQuiz = FirestoreQuiz.FromQuiz(quiz);
                var docRef = await _db.Collection(QuizCollection).AddAsync(firestoreQuiz);
                return docRef.Id;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<Quiz> GetQuizAsync(string quizId)
        {
            try
            {
                var docRef = _db.Collection(QuizCollection).Document(quizId);
                var snapshot = await docRef.GetSnapshotAsync();
                var firestoreQuiz = snapshot.ConvertTo<FirestoreQuiz>();

                // Convert FirestoreQuiz to Quiz and set QuizId
                var quiz = firestoreQuiz.ToQuiz();
                quiz.QuizId = snapshot.Id;

                return quiz;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task UpdateQuizAsync(Quiz quiz)
        {
            var firestoreQuiz = FirestoreQuiz.FromQuiz(quiz);
            var docRef = _db.Collection(QuizCollection).Document(quiz.QuizId);
            await docRef.SetAsync(firestoreQuiz);
        }

        public async Task<(List<Quiz>, string)> ListQuizzesAsync(int pageSize, string pageToken = null)
        {
            var query = _db.Collection(QuizCollection).OrderByDescending("CreatedAt").Limit(pageSize);

            if (!string.IsNullOrEmpty(pageToken))
            {
                var lastDoc = await _db.Collection(QuizCollection).Document(pageToken).GetSnapshotAsync();
                query = query.StartAfter(lastDoc);
            }

            var snapshot = await query.GetSnapshotAsync();
            var quizzes = new List<Quiz>();

            foreach (var doc in snapshot.Documents)
            {
                var firestoreQuiz = doc.ConvertTo<FirestoreQuiz>();
                quizzes.Add(firestoreQuiz.ToQuiz());
            }

            var lastSnapshot = snapshot.Documents.LastOrDefault();
            var nextPageToken = lastSnapshot?.Id;

            return (quizzes, nextPageToken);
        }
    }
}
