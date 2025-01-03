using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DotnetGeminiSDK.Client;
using DotnetGeminiSDK.Client.Interfaces;

namespace QuizService.Services
{
    public class GeminiService
    {
        private readonly IGeminiClient _geminiClient;

        public GeminiService(IGeminiClient geminiClient)
        {
            _geminiClient = geminiClient;
        }

        public async Task<List<Question>> GenerateQuizQuestionsAsync(string topic, int numberOfQuestions)
        {
            var prompt = $@"Generate {numberOfQuestions} multiple choice questions about {topic}. 
                Format the response as a JSON array with each question having the following structure:
                {{
                    ""content"": ""question text"",
                    ""options"": [""option1"", ""option2"", ""option3"", ""option4""],
                    ""correctOptionIndex"": 0-3
                }}
                Make sure the questions are challenging but clear, and the options are plausible but with only one correct answer.
                IMPORTANT: Return ONLY the JSON array, no additional text.";

            var response = await _geminiClient.TextPrompt(prompt);
            if (response == null)
            {
                throw new Exception("Failed to generate quiz questions");
            }

            try
            {
                var jsonText = response.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text;
                Console.WriteLine($"Received JSON: {jsonText}"); // Debug log

                var questions = JsonSerializer.Deserialize<List<QuestionDto>>(jsonText, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"Deserialized {questions?.Count} questions"); // Debug log
                if (questions != null && questions.Any())
                {
                    foreach (var q in questions)
                    {
                        Console.WriteLine($"Question: {q.Content}");
                        Console.WriteLine($"Options count: {q.Options?.Count}");
                        Console.WriteLine($"Options: {string.Join(", ", q.Options ?? new List<string>())}");
                    }
                }

                return ConvertToQuestions(questions);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse generated questions: {ex.Message}. Response: {response.ToString()}");
            }
        }

        private List<Question> ConvertToQuestions(List<QuestionDto> questionDtos)
        {
            var questions = new List<Question>();
            foreach (var dto in questionDtos)
            {
                var question = new Question
                {
                    QuestionId = Guid.NewGuid().ToString(),
                    Content = dto.Content,
                    CorrectOptionIndex = dto.CorrectOptionIndex
                };
                question.Options.AddRange(dto.Options);
                questions.Add(question);
            }
            return questions;
        }

        private class QuestionDto
        {
            public QuestionDto()
            {
                Options = new List<string>();
            }

            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public string Content { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("options")]
            public List<string> Options { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("correctOptionIndex")]
            public int CorrectOptionIndex { get; set; }
        }
    }
}
