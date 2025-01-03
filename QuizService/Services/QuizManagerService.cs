using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace QuizService.Services
{
    public class QuizManagerService : QuizManager.QuizManagerBase
    {
        private readonly GeminiService _geminiService;
        private readonly FirebaseService _firebaseService;

        public QuizManagerService(GeminiService geminiService, FirebaseService firebaseService)
        {
            _geminiService = geminiService;
            _firebaseService = firebaseService;
        }

        public override async Task<QuizResponse> CreateQuiz(CreateQuizRequest request, ServerCallContext context)
        {
            try
            {
                var questions = await _geminiService.GenerateQuizQuestionsAsync(request.Topic, request.NumberOfQuestions);
                
                var quiz = new Quiz
                {
                    Topic = request.Topic,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    CreatedBy = context.GetHttpContext().User?.Identity?.Name ?? "anonymous"
                };
                quiz.Questions.AddRange(questions);

                var quizId = await _firebaseService.CreateQuizAsync(quiz);
                quiz.QuizId = quizId;

                return new QuizResponse { Quiz = quiz };
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<QuizResponse> GetQuiz(GetQuizRequest request, ServerCallContext context)
        {
            try
            {
                var quiz = await _firebaseService.GetQuizAsync(request.QuizId);
                return new QuizResponse { Quiz = quiz };
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<QuizResponse> EditQuiz(EditQuizRequest request, ServerCallContext context)
        {
            try
            {
                await _firebaseService.UpdateQuizAsync(request.Quiz);
                return new QuizResponse { Quiz = request.Quiz };
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<ListQuizzesResponse> ListQuizzes(ListQuizzesRequest request, ServerCallContext context)
        {
            try
            {
                var (quizzes, nextPageToken) = await _firebaseService.ListQuizzesAsync(request.PageSize, request.PageToken);
                
                var response = new ListQuizzesResponse { NextPageToken = nextPageToken };
                foreach (var quiz in quizzes)
                {
                    response.Quizzes.Add(new QuizSummary
                    {
                        QuizId = quiz.QuizId,
                        Topic = quiz.Topic,
                        QuestionCount = quiz.Questions.Count,
                        CreatedAt = quiz.CreatedAt
                    });
                }
                
                return response;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<QuizPlayResponse> GetQuizForPlay(GetQuizRequest request, ServerCallContext context)
        {
            try
            {
                var quiz = await _firebaseService.GetQuizAsync(request.QuizId);
                var playResponse = new QuizPlayResponse
                {
                    QuizId = quiz.QuizId,
                    Topic = quiz.Topic
                };

                foreach (var question in quiz.Questions)
                {
                    playResponse.Questions.Add(new PlayQuestion
                    {
                        QuestionId = question.QuestionId,
                        Content = question.Content,
                        Options = { question.Options }
                    });
                }

                return playResponse;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<ValidateAnswerResponse> ValidateAnswer(ValidateAnswerRequest request, ServerCallContext context)
        {
            try
            {
                var quiz = await _firebaseService.GetQuizAsync(request.QuizId);
                var question = quiz.Questions.FirstOrDefault(q => q.QuestionId == request.QuestionId);

                if (question == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Question not found"));
                }

                var isCorrect = question.CorrectOptionIndex == request.SelectedOptionIndex;
                var feedback = isCorrect ? "Correct answer!" : "Incorrect answer. Try again!";

                return new ValidateAnswerResponse
                {
                    IsCorrect = isCorrect,
                    Feedback = feedback
                };
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }
    }
}
