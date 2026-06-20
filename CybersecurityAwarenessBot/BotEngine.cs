using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CybersecurityAwarenessBot
{
    public delegate string SentimentModifier(string botResponse);
    public enum BotState { Normal, QuizActive }

    public class BotEngine
    {
        public string UserName { get; set; } = "User";
        private BotState _currentState = BotState.Normal;
        public bool IsQuizActive => _currentState == BotState.QuizActive;

        private DatabaseHelper _dbHelper;
        private Random _randomizer;
        private List<string> _activityLog;

        private int _quizScore = 0;
        private int _currentQuestionIndex = 0;
        private List<QuizQuestion> _quizQuestions;
        private readonly Dictionary<string, List<string>> _topicResponses;

        public BotEngine()
        {
            _randomizer = new Random();
            _activityLog = new List<string>();
            _quizQuestions = new List<QuizQuestion>();
            _dbHelper = new DatabaseHelper();

            LogActivity("Application started. BotEngine initialized.");

            _topicResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "password", new List<string> { "Make sure to use strong, unique passwords for each account.", "Consider using a password manager." } },
                { "phishing", new List<string> { "Never click on unexpected links in SMSes or emails.", "Look out for spelling errors in emails." } },
                { "scam", new List<string> { "If an online deal looks too good to be true, it probably is.", "Never share your OTP with anyone." } },
                { "privacy", new List<string> { "Check your social media settings to ensure your profile is private.", "Avoid sharing your real-time location." } }
            };
            LoadQuizQuestions();
        }

        private void LogActivity(string action)
        {
            if (_activityLog.Count >= 10) _activityLog.RemoveAt(0);
            _activityLog.Add($"[{DateTime.Now:HH:mm}] {action}");
        }

        public List<UserTaskModel> GetUserTasks() => _dbHelper.GetTaskModels();
        public void CompleteTask(int id) { _dbHelper.MarkTaskCompleteById(id); LogActivity($"Marked Task #{id} as complete via Dashboard."); }
        public void DeleteTask(int id) { _dbHelper.DeleteTaskById(id); LogActivity($"Deleted Task #{id} via Dashboard."); }

        public QuizQuestion? GetCurrentQuizQuestion() => IsQuizActive && _currentQuestionIndex < _quizQuestions.Count ? _quizQuestions[_currentQuestionIndex] : null;

        public string ProcessInput(string? userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput) && !IsQuizActive) return "I didn't quite catch that.";
            string normalizedInput = userInput?.Trim() ?? "";

            if (_currentState == BotState.QuizActive) return HandleQuizInput(normalizedInput);

            string lowerInput = normalizedInput.ToLower();

            if (Regex.IsMatch(lowerInput, @"\b(activity log|what have you done)\b"))
            {
                LogActivity("User requested to view the activity log.");
                return GetActivityLog();
            }

            if (Regex.IsMatch(lowerInput, @"\b(start quiz|play game|take quiz)\b"))
            {
                _currentState = BotState.QuizActive;
                _quizScore = 0;
                _currentQuestionIndex = 0;
                LogActivity("Cybersecurity Quiz started.");
                return $"Awesome! Let's test your knowledge. I'll ask you {_quizQuestions.Count} questions.\n\nQuestion 1: {_quizQuestions[0].QuestionText}";
            }

            Match addTaskMatch = Regex.Match(lowerInput, @"(?:add task|set reminder|remind me)(?: in (\d+) days?)?(?: to)? (.+)");
            if (addTaskMatch.Success)
            {
                string daysString = addTaskMatch.Groups[1].Value;
                string taskExtract = addTaskMatch.Groups[2].Value.Trim();
                if (taskExtract.Length > 0) taskExtract = char.ToUpper(taskExtract[0]) + taskExtract.Substring(1);

                int daysToAdd = string.IsNullOrEmpty(daysString) ? 1 : int.Parse(daysString);
                DateTime reminderDate = DateTime.Now.AddDays(daysToAdd);

                _dbHelper.AddUserTask(taskExtract, "Task added via smart chat.", reminderDate);
                LogActivity($"Task added: '{taskExtract}'");
                return $"Got it! I've added '{taskExtract}' and set your reminder for {reminderDate.ToShortDateString()}.";
            }

            // The secret code that triggers the UI Dashboard
            if (Regex.IsMatch(lowerInput, @"\b(show|tell|what|view).*?(tasks|reminders)\b"))
            {
                LogActivity("User opened the Interactive Task Dashboard.");
                return "[DISPLAY_TASKS]";
            }

            SentimentModifier? sentimentModifier = DetectSentiment(lowerInput);
            foreach (var topic in _topicResponses.Keys)
            {
                if (lowerInput.Contains(topic))
                {
                    string baseResponse = _topicResponses[topic][_randomizer.Next(_topicResponses[topic].Count)];
                    LogActivity($"Provided info about '{topic}'.");
                    return sentimentModifier != null ? sentimentModifier(baseResponse) : baseResponse;
                }
            }
            return "I'm not sure I understand. You can ask me to 'start quiz', 'remind me to [action]', 'show tasks', or ask about scams.";
        }

        private string GetActivityLog()
        {
            if (_activityLog.Count == 0) return "I haven't done much yet today!";
            string logString = "Here's a summary of recent actions:\n";
            for (int i = 0; i < _activityLog.Count; i++) logString += $"{i + 1}. {_activityLog[i]}\n";
            return logString;
        }

        private void LoadQuizQuestions()
        {
            _quizQuestions = new List<QuizQuestion>
            {
                new QuizQuestion("What should you do if you receive an unexpected email asking for your password?", new List<string> { "Reply with it", "Delete it", "Report as phishing", "Ignore it" }, "Report as phishing", "Reporting phishing emails helps prevent scams and alerts security teams."),
                new QuizQuestion("True or False: Using 'Password123' is a secure choice.", new List<string> { "True", "False" }, "False", "Weak passwords are easily cracked. Always use a mix of characters, numbers, and symbols."),
                new QuizQuestion("What does 2FA stand for?", new List<string> { "Two-File Access", "Two-Factor Authentication", "To Find Accounts" }, "Two-Factor Authentication", "2FA adds an extra layer of security beyond just your password."),
                new QuizQuestion("True or False: Public Wi-Fi is safe for online banking.", new List<string> { "True", "False" }, "False", "Public networks can be easily intercepted by hackers. Avoid sensitive transactions on them."),
                new QuizQuestion("What type of malware locks your files until you pay a fee?", new List<string> { "Spyware", "Adware", "Ransomware" }, "Ransomware", "Ransomware encrypts your data and demands money for the decryption key."),
                new QuizQuestion("Why should you update your phone and PC software regularly?", new List<string> { "To get new emojis", "To patch security vulnerabilities", "To use more battery" }, "To patch security vulnerabilities", "Updates often contain critical patches for newly discovered security flaws."),
                new QuizQuestion("What is a VPN primarily used for?", new List<string> { "Speeding up the internet", "Encrypting your internet connection", "Blocking ads" }, "Encrypting your internet connection", "A Virtual Private Network (VPN) encrypts your data, keeping your browsing private."),
                new QuizQuestion("What is the main purpose of a firewall?", new List<string> { "To block unauthorized network access", "To cool down your CPU", "To store passwords" }, "To block unauthorized network access", "Firewalls act as a barrier between your internal network and external threats."),
                new QuizQuestion("True or False: You should use the same password for all your accounts so you don't forget it.", new List<string> { "True", "False" }, "False", "If one account is breached, all your accounts are compromised. Always use unique passwords."),
                new QuizQuestion("What is it called when an attacker manipulates you into giving up confidential info?", new List<string> { "Social Engineering", "Hacking", "Brute Forcing" }, "Social Engineering", "Social engineering relies on human interaction and psychological manipulation rather than pure technical exploits."),
                new QuizQuestion("If a website URL starts with HTTPS, what does the 'S' stand for?", new List<string> { "System", "Secure", "Server" }, "Secure", "HTTPS means the connection between your browser and the website is encrypted and secure.")
            };
        }

        private string HandleQuizInput(string selectedAnswer)
        {
            var currentQuestion = _quizQuestions[_currentQuestionIndex];
            bool isCorrect = selectedAnswer.Equals(currentQuestion.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

            string feedback = isCorrect ? "✅ Correct!" : $"❌ Incorrect. The right answer was: {currentQuestion.CorrectAnswer}.";
            feedback += $" {currentQuestion.Explanation}\n\n";

            if (isCorrect) _quizScore++;
            _currentQuestionIndex++;

            if (_currentQuestionIndex >= _quizQuestions.Count)
            {
                _currentState = BotState.Normal;
                return feedback + $"--- Quiz Finished! ---\nYou scored {_quizScore} out of {_quizQuestions.Count}.";
            }
            return feedback + $"Question {_currentQuestionIndex + 1}: {_quizQuestions[_currentQuestionIndex].QuestionText}";
        }

        private SentimentModifier? DetectSentiment(string input) { return null; }
    }

    public class QuizQuestion
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }

        public QuizQuestion(string question, List<string> options, string answer, string explanation)
        {
            QuestionText = question;
            Options = options;
            CorrectAnswer = answer;
            Explanation = explanation;
        }
    }
}