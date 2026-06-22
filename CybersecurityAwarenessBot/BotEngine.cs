using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CybersecurityAwarenessBot
{
    // Delegate to allow dynamic modification of strings (e.g., altering tone based on sentiment)
    public delegate string SentimentModifier(string botResponse);

    // Enum to strictly control application logic flow
    public enum BotState { Normal, QuizActive }


    // The core logical brain of the application. Processes Natural Language Processing (NLP),
    // maintains conversation state, and handles the Quiz mechanics.

    public class BotEngine
    {
        public string UserName { get; set; } = "User";
        private BotState _currentState = BotState.Normal;
        public bool IsQuizActive => _currentState == BotState.QuizActive;

        private DatabaseHelper _dbHelper;
        private Random _randomizer;
        private List<string> _activityLog; // In-memory cache tracking user interaction

        // Quiz State Tracking
        private int _quizScore = 0;
        private int _currentQuestionIndex = 0;
        private List<QuizQuestion> _quizQuestions;

        // Knowledge Base: Dictionary mapping keywords to a list of possible responses
        private readonly Dictionary<string, List<string>> _topicResponses;

        public BotEngine()
        {
            _randomizer = new Random();
            _activityLog = new List<string>();
            _quizQuestions = new List<QuizQuestion>();
            _dbHelper = new DatabaseHelper();

            LogActivity("Application started. BotEngine initialized.");

            // Initialize the knowledge base with arrays of randomized responses to make the bot feel natural
            _topicResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "password", new List<string> { "Make sure to use strong, unique passwords for each account.", "Consider using a password manager." } },
                { "phishing", new List<string> { "Never click on unexpected links in SMSes or emails.", "Look out for spelling errors in emails." } },
                { "scam", new List<string> { "If an online deal looks too good to be true, it probably is.", "Never share your OTP with anyone." } },
                { "privacy", new List<string> { "Check your social media settings to ensure your profile is private.", "Avoid sharing your real-time location." } }
            };
            LoadQuizQuestions();
        }


        // Appends an action to the volatile memory log. Caps the log at 10 items to save memory.

        private void LogActivity(string action)
        {
            if (_activityLog.Count >= 10) _activityLog.RemoveAt(0);
            _activityLog.Add($"[{DateTime.Now:HH:mm}] {action}");
        }

        // Bridge methods exposing Database actions directly to the WPF UI Buttons
        public List<UserTaskModel> GetUserTasks() => _dbHelper.GetTaskModels();
        public void CompleteTask(int id) { _dbHelper.MarkTaskCompleteById(id); LogActivity($"Marked Task #{id} as complete via Dashboard."); }
        public void DeleteTask(int id) { _dbHelper.DeleteTaskById(id); LogActivity($"Deleted Task #{id} via Dashboard."); }

        // Returns the active quiz question object so the UI can generate the correct buttons
        public QuizQuestion? GetCurrentQuizQuestion() => IsQuizActive && _currentQuestionIndex < _quizQuestions.Count ? _quizQuestions[_currentQuestionIndex] : null;


        // Primary NLP processing function. Takes raw user string, matches via Regex, and determines response.

        public string ProcessInput(string? userInput)
        {
            // Edge case handling for empty input
            if (string.IsNullOrWhiteSpace(userInput) && !IsQuizActive) return "I didn't quite catch that.";
            string normalizedInput = userInput?.Trim() ?? "";

            // If a quiz is running, override all normal logic and funnel input to the Quiz Handler
            if (_currentState == BotState.QuizActive) return HandleQuizInput(normalizedInput);

            string lowerInput = normalizedInput.ToLower();

            // Check for Activity Log request
            if (Regex.IsMatch(lowerInput, @"\b(activity log|what have you done)\b"))
            {
                LogActivity("User requested to view the activity log.");
                return GetActivityLog();
            }

            // Check for Quiz start trigger
            if (Regex.IsMatch(lowerInput, @"\b(start quiz|play game|take quiz)\b"))
            {
                _currentState = BotState.QuizActive;
                _quizScore = 0;
                _currentQuestionIndex = 0;
                LogActivity("Cybersecurity Quiz started.");
                return $"Awesome! Let's test your knowledge. I'll ask you {_quizQuestions.Count} questions.\n\nQuestion 1: {_quizQuestions[0].QuestionText}";
            }

            // Complex Regex: Captures optional timeframes ("in 5 days") and the task title itself via capture groups
            Match addTaskMatch = Regex.Match(lowerInput, @"(?:add task|set reminder|remind me)(?: in (\d+) days?)?(?: to)? (.+)");
            if (addTaskMatch.Success)
            {
                string daysString = addTaskMatch.Groups[1].Value;
                string taskExtract = addTaskMatch.Groups[2].Value.Trim();

                // Capitalize the first letter for neatness in the database
                if (taskExtract.Length > 0) taskExtract = char.ToUpper(taskExtract[0]) + taskExtract.Substring(1);

                // Default to 1 day if no specific timeframe was given
                int daysToAdd = string.IsNullOrEmpty(daysString) ? 1 : int.Parse(daysString);
                DateTime reminderDate = DateTime.Now.AddDays(daysToAdd);

                _dbHelper.AddUserTask(taskExtract, "Task added via smart chat.", reminderDate);
                LogActivity($"Task added: '{taskExtract}'");
                return $"Got it! I've added '{taskExtract}' and set your reminder for {reminderDate.ToShortDateString()}.";
            }

            // Intercept requests to view tasks and return a special token that the UI reads to open the Dashboard overlay
            if (Regex.IsMatch(lowerInput, @"\b(show|tell|what|view).*?(tasks|reminders)\b"))
            {
                LogActivity("User opened the Interactive Task Dashboard.");
                return "[DISPLAY_TASKS]";
            }

            // Fallback: Check knowledge base dictionary for keyword matches
            SentimentModifier? sentimentModifier = DetectSentiment(lowerInput);
            foreach (var topic in _topicResponses.Keys)
            {
                if (lowerInput.Contains(topic))
                {
                    // Select a random response from the matched list
                    string baseResponse = _topicResponses[topic][_randomizer.Next(_topicResponses[topic].Count)];
                    LogActivity($"Provided info about '{topic}'.");
                    // Apply delegate modification if sentiment was detected, otherwise return standard string
                    return sentimentModifier != null ? sentimentModifier(baseResponse) : baseResponse;
                }
            }

            // Absolute fallback if no regex or dictionary conditions are met
            return "I'm not sure I understand. You can ask me to 'start quiz', 'remind me to [action]', 'show tasks', or ask about scams.";
        }


        // Formats the volatile List memory into a readable string for the UI.

        private string GetActivityLog()
        {
            if (_activityLog.Count == 0) return "I haven't done much yet today!";
            string logString = "Here's a summary of recent actions:\n";
            for (int i = 0; i < _activityLog.Count; i++) logString += $"{i + 1}. {_activityLog[i]}\n";
            return logString;
        }


        // Hardcoded population of the Quiz data structures.

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


        // Evaluates answers against the current QuizQuestion object and tracks scores.

        private string HandleQuizInput(string selectedAnswer)
        {
            var currentQuestion = _quizQuestions[_currentQuestionIndex];
            // Uses OrdinalIgnoreCase to ensure correct matching regardless of UI capitalization
            bool isCorrect = selectedAnswer.Equals(currentQuestion.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

            string feedback = isCorrect ? "✅ Correct!" : $"❌ Incorrect. The right answer was: {currentQuestion.CorrectAnswer}.";
            feedback += $" {currentQuestion.Explanation}\n\n";

            if (isCorrect) _quizScore++;
            _currentQuestionIndex++;

            // Check if quiz array is exhausted
            if (_currentQuestionIndex >= _quizQuestions.Count)
            {
                _currentState = BotState.Normal; // Release the UI lock
                return feedback + $"--- Quiz Finished! ---\nYou scored {_quizScore} out of {_quizQuestions.Count}.";
            }
            // Proceed to next question
            return feedback + $"Question {_currentQuestionIndex + 1}: {_quizQuestions[_currentQuestionIndex].QuestionText}";
        }

        // Sentiment analysis placeholder. Can return null if no modifier is needed.
        private SentimentModifier? DetectSentiment(string input) { return null; }
    }


    // Data structure holding the properties of a single quiz question.
    // Lists are used for Options to allow the WPF UI to dynamically render a variable number of buttons.

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