using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Speech.Synthesis;

namespace CybersecurityAwarenessBot
{
    public partial class MainWindow : Window
    {
        private BotEngine _bot;
        private bool _isAwaitingName = true;
        private SpeechSynthesizer _synth;

        public MainWindow()
        {
            InitializeComponent();
            _bot = new BotEngine();

            // Initialize the Voice Synthesizer
            _synth = new SpeechSynthesizer();
            _synth.SetOutputToDefaultAudioDevice();

            DisplayHeader();

            // Play your custom WAV file greeting
            PlayVoiceGreeting();

            AppendMessage("System", "Welcome to your personalized Cybersecurity Awareness Bot! Please enter your name:");
            // NOTE: SpeakText is intentionally left out here so it doesn't talk over your custom .wav file!
        }

        private void SpeakText(string text)
        {
            try
            {
                // Clean up emojis and symbols so the bot doesn't literally read them out loud
                string cleanText = text.Replace("✅", "").Replace("❌", "").Replace("🎮", "").Replace("📋", "").Replace("📜", "").Replace("🗑️", "").Replace("➕", "").Replace("*", "");

                _synth.SpeakAsyncCancelAll(); // Stop talking if the user interrupts with a new message
                _synth.SpeakAsync(cleanText);
            }
            catch
            {
                // Fails silently if the computer has no speakers plugged in
            }
        }

        private void DisplayHeader()
        {
            string guardianArt = @"
 _______    __   __     _____    _______   ______    _________    _____    ___   __
/ _____ \  | |   | |   / ___ \   | ___  \  | ___ \   |___  __|   / ___ \   |   \ | |
| |        | |   | |  / /   \ \  | |  | |  | |  \ |     | |     / /   \ \  | |\ \| |
| | _____  | |   | |  | |___| |  | |__| |  | |  | |     | |     | |___| |  | | \ \ |
| | |__ |  | |   | |  | _____ |  | __  /   | |  | |     | |     | _____ |  | |  \ \|
| |   | |  | |   | |  | |   | |  | | \ \   | |  | |     | |     | |   | |  | |   | |
| |___| |  | |___| |  | |   | |  | |  | |  | |__/ |   __| |___  | |   | |  | |   | |
\_______/  \_______/  |_|   |_|  |_|  |_|  |_____/   |_______|  |_|   |_|  |_|   |_|
                                                  
 +++ MZANSI'S CYBERSECURITY AWARENESS ASSISTANT +++
";
            ChatOutput.Text += guardianArt + "\n===============================================================================\n\n";
        }

        private void PlayVoiceGreeting()
        {
            try
            {
                SoundPlayer player = new SoundPlayer("greeting.wav");
                player.LoadAsync();
                player.Play();
            }
            catch (Exception) { /* Fails silently if wav is missing */ }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) => ProcessInput(UserInputBox.Text);

        private void UserInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ProcessInput(UserInputBox.Text);
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                string rawText = clickedButton.Content.ToString() ?? "";
                ProcessInput(rawText.Substring(3).Trim());
            }
        }

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            TaskDashboardOverlay.Visibility = Visibility.Collapsed;
            ChatScroll.Visibility = Visibility.Visible;

            UserInputBox.Text = "remind me to ";
            UserInputBox.Focus();
            UserInputBox.CaretIndex = UserInputBox.Text.Length;

            string msg = "What would you like me to remind you about?";
            AppendMessage("System", msg);
            SpeakText(msg);
            ChatScroll.ScrollToEnd();
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            ChatOutput.Text = string.Empty;
            DisplayHeader();

            string msg = "Chat history cleared.";
            AppendMessage("System", msg);
            SpeakText(msg);
        }

        private void ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input) && !_bot.IsQuizActive) return;

            if (!_bot.IsQuizActive)
            {
                AppendMessage(_isAwaitingName ? "User" : _bot.UserName, input);
                UserInputBox.Clear();
            }

            if (_isAwaitingName)
            {
                _bot.UserName = input.Trim();
                _isAwaitingName = false;

                string greeting = $"Hello, {_bot.UserName}! I am ready to help.";
                AppendMessage("Guardian", greeting);
                SpeakText(greeting);
            }
            else
            {
                string response = _bot.ProcessInput(input);

                if (response == "[DISPLAY_TASKS]")
                {
                    ShowInteractiveTasks();
                    SpeakText("Opening your interactive task manager.");
                }
                else
                {
                    AppendMessage("Guardian", response);
                    SpeakText(response);
                }

                UpdateQuizInterface();
            }
            ChatScroll.ScrollToEnd();
        }

        private void ShowInteractiveTasks()
        {
            ChatScroll.Visibility = Visibility.Collapsed;
            TaskDashboardOverlay.Visibility = Visibility.Visible;
            InteractiveTaskList.Children.Clear();

            var tasks = _bot.GetUserTasks();

            if (tasks.Count == 0)
            {
                InteractiveTaskList.Children.Add(new TextBlock { Text = "You have no pending tasks!", Foreground = Brushes.White, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) });
                return;
            }

            foreach (var task in tasks)
            {
                Border taskCard = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                Grid taskGrid = new Grid();
                taskGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                taskGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                string statusIcon = task.IsCompleted ? "✅" : "⏳";
                string dateText = task.ReminderDate.HasValue ? $" (Due: {task.ReminderDate.Value.ToShortDateString()})" : "";

                TextBlock txtTitle = new TextBlock
                {
                    Text = $"{statusIcon} {task.Title}{dateText}",
                    Foreground = Brushes.White,
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null
                };
                Grid.SetColumn(txtTitle, 0);
                taskGrid.Children.Add(txtTitle);

                StackPanel btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
                Grid.SetColumn(btnPanel, 1);

                if (!task.IsCompleted)
                {
                    Button btnComplete = new Button
                    {
                        Content = "Complete",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                        Margin = new Thickness(0, 0, 10, 0),
                        Style = (Style)FindResource("InteractiveButton")
                    };
                    btnComplete.Click += delegate {
                        _bot.CompleteTask(task.TaskId);
                        ShowInteractiveTasks();
                        SpeakText("Task completed.");
                    };
                    btnPanel.Children.Add(btnComplete);
                }

                Button btnDelete = new Button
                {
                    Content = "Delete",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    Style = (Style)FindResource("InteractiveButton")
                };
                btnDelete.Click += delegate {
                    _bot.DeleteTask(task.TaskId);
                    ShowInteractiveTasks();
                    SpeakText("Task deleted.");
                };
                btnPanel.Children.Add(btnDelete);

                taskGrid.Children.Add(btnPanel);
                taskCard.Child = taskGrid;
                InteractiveTaskList.Children.Add(taskCard);
            }
        }

        private void CloseTaskDashboard_Click(object sender, RoutedEventArgs e)
        {
            TaskDashboardOverlay.Visibility = Visibility.Collapsed;
            ChatScroll.Visibility = Visibility.Visible;

            string msg = "Task Manager closed.";
            AppendMessage("Guardian", msg);
            SpeakText(msg);
        }

        private void UpdateQuizInterface()
        {
            QuizAnswersPanel.Children.Clear();
            if (_bot.IsQuizActive)
            {
                UserInputBox.IsEnabled = false;
                SendButton.IsEnabled = false;
                QuizAnswersPanel.Visibility = Visibility.Visible;

                var currentQuestion = _bot.GetCurrentQuizQuestion();
                if (currentQuestion != null)
                {
                    foreach (var option in currentQuestion.Options)
                    {
                        Button optionButton = new Button
                        {
                            Content = option,
                            Height = 40,
                            Margin = new Thickness(5),
                            Background = Brushes.DarkSlateGray,
                            Style = (Style)FindResource("InteractiveButton")
                        };
                        optionButton.Click += (s, e) => ProcessInput(option);
                        QuizAnswersPanel.Children.Add(optionButton);
                    }
                }
            }
            else
            {
                UserInputBox.IsEnabled = true;
                SendButton.IsEnabled = true;
                QuizAnswersPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void AppendMessage(string sender, string message) => ChatOutput.Text += $"[{sender.ToUpper()}]: {message}\n\n";
    }
}