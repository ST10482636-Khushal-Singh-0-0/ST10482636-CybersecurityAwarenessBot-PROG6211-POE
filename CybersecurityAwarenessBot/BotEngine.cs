using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CybersecurityAwarenessBot
{
    public delegate string SentimentModifier(string botResponse);

    public enum BotState
    {
        Normal,
        QuizActive
    }

    public class BotEngine
    {
        public string UserName { get; set; } = "User";
        private BotState _currentState = BotState.Normal;

        // Modules
        private DatabaseHelper _dbHelper;
        private Random _randomizer;

        // Memory & Logs
        private string _favoriteTopic = string.Empty;
        private List<string> _activityLog;

        // Quiz State
        private int _quizScore = 0;
        private int _currentQuestionIndex = 0;
        private List<QuizQuestion> _quizQuestions;

        // Part 2 Basic Responses
        private readonly Dictionary<string, List<string>> _topicResponses;

        public BotEngine()
        {
            _randomizer = new Random();
            _activityLog = new List<string>();
            _quizQuestions = new List<QuizQuestion>();
            _dbHelper = new DatabaseHelper();

            LogActivity("Application started. BotEngine initialized.");

            // Standard responses for generic keywords
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
            if (_activityLog.Count >= 10) _activityLog.RemoveAt(0); // Keep max 10 logs
            _activityLog.Add($"[{DateTime.Now:HH:mm}] {action}");
        }

        public string ProcessInput(string? userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput)) return "I didn't quite catch that.";
            string normalizedInput = userInput.Trim().ToLower();

            // 1. Handle Active Quiz State
            if (_currentState == BotState.QuizActive)
            {
                return HandleQuizInput(normalizedInput);
            }

            // 2. Task 4: Activity Log
            if (Regex.IsMatch(normalizedInput, @"\b(activity log|what have you done)\b"))
            {
                LogActivity("User requested to view the activity log.");
                return GetActivityLog();
            }

            // 3. Task 2: Start Quiz Command
            if (Regex.IsMatch(normalizedInput, @"\b(start quiz|play game|take quiz)\b"))
            {
                _currentState = BotState.QuizActive;
                _quizScore = 0;
                _currentQuestionIndex = 0;
                LogActivity("Cybersecurity Quiz started.");
                return $"Awesome! Let's test your knowledge. I'll ask you 11 questions.\n\nQuestion 1: {_quizQuestions[0].QuestionText}";
            }

            // 4. SMART NLP: Adding a task and detecting "in X days"
            Match addTaskMatch = Regex.Match(normalizedInput, @"(?:add task|set reminder|remind me)(?: in (\d+) days?)?(?: to)? (.+)");
            if (addTaskMatch.Success)
            {
                string daysString = addTaskMatch.Groups[1].Value;
                string taskExtract = addTaskMatch.Groups[2].Value.Trim();

                // Capitalize the first letter so it looks neat
                if (taskExtract.Length > 0)
                    taskExtract = char.ToUpper(taskExtract[0]) + taskExtract.Substring(1);

                // If you specified a number of days, calculate it. Otherwise, default to 1 day.
                int daysToAdd = string.IsNullOrEmpty(daysString) ? 1 : int.Parse(daysString);
                DateTime reminderDate = DateTime.Now.AddDays(daysToAdd);

                _dbHelper.AddUserTask(taskExtract, "Task added via smart chat.", reminderDate);
                LogActivity($"Task added: '{taskExtract}' (Reminder set for {reminderDate:MMM dd})");

                return $"Got it! I've added '{taskExtract}' and set your reminder for {daysToAdd} day(s) from now ({reminderDate.ToShortDateString()}).";
            }

            // 5. SMART NLP: Viewing your reminders
            if (Regex.IsMatch(normalizedInput, @"\b(show|tell|what|view).*?(tasks|reminders)\b"))
            {
                LogActivity("User requested to view their task list.");
                var tasks = _dbHelper.GetAllTasks();
                if (tasks.Count == 0) return "You currently have no reminders saved.";

                string response = "Here are your reminders, in the order you gave them to me:\n\n";
                for (int i = 0; i < tasks.Count; i++)
                {
                    response += $"{i + 1}. {tasks[i]}\n";
                }
                return response;
            }

            // 6. SMART NLP: Marking a task as completed (Handles Numbers and Words)
            Match numberMatch = Regex.Match(normalizedInput, @"(?:mark|complete|finish) (?:task|reminder )?(\d+)");

            if (numberMatch.Success)
            {
                // The user typed a number! Extract the digit (e.g., "1")
                int listNumber = int.Parse(numberMatch.Groups[1].Value);
                bool success = _dbHelper.MarkTaskCompleteByListNumber(listNumber);

                if (success)
                {
                    LogActivity($"Marked item #{listNumber} as completed.");
                    return $"Awesome! I've officially checked item #{listNumber} off your list.";
                }
                else
                {
                    return $"I couldn't check off item #{listNumber}. Are you sure that number is on your list and is currently [Pending]?";
                }
            }
            else if (normalizedInput.StartsWith("mark ") || normalizedInput.StartsWith("i finished ") || normalizedInput.StartsWith("i completed "))
            {
                // The user typed the name of the task instead of a number
                string taskName = normalizedInput
                    .Replace("mark ", "")
                    .Replace("i finished ", "")
                    .Replace("i completed ", "")
                    .Replace(" as done", "")
                    .Replace(" as complete", "")
                    .Replace(" as completed", "")
                    .Replace(" task", "")
                    .Replace(" reminder", "")
                    .Trim();

                bool success = _dbHelper.MarkTaskAsCompleted(taskName);

                if (success)
                {
                    LogActivity($"Marked task as completed: '{taskName}'");
                    return $"Great job! I've officially marked '{taskName}' as [Completed] in your database.";
                }
                else
                {
                    return $"I couldn't find an unfinished task matching '{taskName}'. Try asking to 'show tasks' and make sure you spell it right, or just say 'mark 1 as complete'.";
                }
            }

            // 7. Part 2 Logic - Sentiment & Keyword Fallback
            SentimentModifier? sentimentModifier = DetectSentiment(normalizedInput);
            foreach (var topic in _topicResponses.Keys)
            {
                if (normalizedInput.Contains(topic))
                {
                    string baseResponse = _topicResponses[topic][_randomizer.Next(_topicResponses[topic].Count)];
                    LogActivity($"Provided information about '{topic}'.");
                    return sentimentModifier != null ? sentimentModifier(baseResponse) : baseResponse;
                }
            }

            return "I'm not sure I understand. You can ask me to 'start quiz', 'remind me to [action]', 'show tasks', 'show activity log', or ask about scams/passwords.";
        }

        private string GetActivityLog()
        {
            if (_activityLog.Count == 0) return "I haven't done much yet today!";
            string logString = "Here's a summary of recent actions:\n";
            for (int i = 0; i < _activityLog.Count; i++)
            {
                logString += $"{i + 1}. {_activityLog[i]}\n";
            }
            return logString;
        }

        private void LoadQuizQuestions()
        {
            _quizQuestions = new List<QuizQuestion>
            {
                new QuizQuestion("What should you do if you receive an unexpected email asking for your password?\nA) Reply with it\nB) Delete it\nC) Report as phishing\nD) Ignore it", "c", "Reporting phishing emails helps prevent scams and alerts security teams."),
                new QuizQuestion("True or False: Using 'Password123' is a secure choice.", "false", "Weak passwords are easily cracked. Always use a mix of characters, numbers, and symbols."),
                new QuizQuestion("What does 2FA stand for?\nA) Two-File Access\nB) Two-Factor Authentication\nC) To Find Accounts", "b", "2FA adds an extra layer of security beyond just your password."),
                new QuizQuestion("True or False: Public Wi-Fi is safe for online banking.", "false", "Public networks can be easily intercepted by hackers. Avoid sensitive transactions on them."),
                new QuizQuestion("What type of malware locks your files until you pay a fee?\nA) Spyware\nB) Adware\nC) Ransomware", "c", "Ransomware encrypts your data and demands money for the decryption key."),
                new QuizQuestion("Why should you update your phone and PC software regularly?\nA) To get new emojis\nB) To patch security vulnerabilities\nC) To use more battery", "b", "Updates often contain critical patches for newly discovered security flaws."),
                new QuizQuestion("What is a VPN primarily used for?\nA) Speeding up the internet\nB) Encrypting your internet connection\nC) Blocking ads", "b", "A Virtual Private Network (VPN) encrypts your data, keeping your browsing private."),
                new QuizQuestion("What is the main purpose of a firewall?\nA) To block unauthorized network access\nB) To cool down your CPU\nC) To store passwords", "a", "Firewalls act as a barrier between your internal network and external threats."),
                new QuizQuestion("True or False: You should use the same password for all your accounts so you don't forget it.", "false", "If one account is breached, all your accounts are compromised. Always use unique passwords."),
                new QuizQuestion("What is it called when a scammer manipulates you into giving up confidential info?\nA) Social Engineering\nB) Hacking\nC) Brute Forcing", "a", "Social engineering relies on human error and psychological manipulation rather than technical exploits."),
                new QuizQuestion("If a website URL starts with HTTPS, what does the 'S' stand for?\nA) System\nB) Secure\nC) Server", "b", "HTTPS means the connection between your browser and the website is encrypted and secure.")
            };
        }

        private string HandleQuizInput(string input)
        {
            var currentQuestion = _quizQuestions[_currentQuestionIndex];

            // Allow the user to type just the letter (e.g., "c") or the word ("false")
            bool isCorrect = input == currentQuestion.CorrectAnswer.ToLower();

            string feedback = isCorrect ? "Correct!" : "Incorrect.";
            feedback += $" {currentQuestion.Explanation}\n\n";

            if (isCorrect) _quizScore++;
            _currentQuestionIndex++;

            if (_currentQuestionIndex >= _quizQuestions.Count)
            {
                _currentState = BotState.Normal;
                LogActivity($"Quiz completed. Final Score: {_quizScore}/{_quizQuestions.Count}");
                string rank = _quizScore > 8 ? "You're a cybersecurity pro!" : "Keep learning to stay safe online!";
                return feedback + $"--- Quiz Finished! ---\nYou scored {_quizScore} out of {_quizQuestions.Count}. {rank}";
            }

            return feedback + $"Question {_currentQuestionIndex + 1}: {_quizQuestions[_currentQuestionIndex].QuestionText}";
        }

        private SentimentModifier? DetectSentiment(string input)
        {
            if (input.Contains("worried") || input.Contains("anxious"))
                return (resp) => "It's completely understandable to feel that way. Let me share a tip to help you stay safe:\n\n" + resp;
            if (input.Contains("frustrated") || input.Contains("confused"))
                return (resp) => "Cybersecurity can feel overwhelming, but taking it one step at a time helps. Here is a simple tip:\n\n" + resp;
            return null;
        }
    }

    public class QuizQuestion
    {
        public string QuestionText { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }

        public QuizQuestion(string question, string answer, string explanation)
        {
            QuestionText = question;
            CorrectAnswer = answer;
            Explanation = explanation;
        }
    }
}