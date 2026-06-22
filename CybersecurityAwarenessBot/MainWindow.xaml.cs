using System;
using System.Collections.Generic; // Added for list filtering
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

        // NEW: Prevents an infinite loop when we highlight dates via code
        private bool _isUpdatingCalendar = false;

        public MainWindow()
        {
            InitializeComponent();
            _bot = new BotEngine();

            _synth = new SpeechSynthesizer();
            _synth.SetOutputToDefaultAudioDevice();

            DisplayHeader();
            PlayVoiceGreeting();

            AppendMessage("System", "Welcome to your personalized Cybersecurity Awareness Bot! Please enter your name:");
        }

        private void SpeakText(string text)
        {
            try
            {
                string cleanText = text.Replace("✅", "").Replace("❌", "").Replace("🎮", "").Replace("📋", "").Replace("📜", "").Replace("🗑️", "").Replace("➕", "").Replace("*", "");
                _synth.SpeakAsyncCancelAll();
                _synth.SpeakAsync(cleanText);
            }
            catch { }
        }

        private void AppendMessage(string sender, string message)
        {
            ChatOutput.Text += $"[{sender.ToUpper()}] // {DateTime.Now:HH:mm}\n{message}\n\n";
            ChatScroll.ScrollToEnd();
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
            catch (Exception) { }
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
                ProcessInput(rawText.Trim());
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
        }

        // NEW: Handles the user clicking on a specific day in the calendar
        private void TaskCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            // If the code is updating the highlights, ignore the event so it doesn't loop
            if (_isUpdatingCalendar) return;

            if (TaskCalendar.SelectedDate.HasValue)
            {
                // Filter the task list to show ONLY tasks for the clicked date
                ShowInteractiveTasks(TaskCalendar.SelectedDate.Value);
            }
        }

        // UPDATED: Now accepts an optional filter date parameter
        private void ShowInteractiveTasks(DateTime? filterDate = null)
        {
            ChatScroll.Visibility = Visibility.Collapsed;
            TaskDashboardOverlay.Visibility = Visibility.Visible;
            InteractiveTaskList.Children.Clear();

            var allTasks = _bot.GetUserTasks();

            // 1. Highlight all pending tasks on the calendar
            _isUpdatingCalendar = true;
            TaskCalendar.SelectedDates.Clear();
            foreach (var task in allTasks)
            {
                if (!task.IsCompleted && task.ReminderDate.HasValue)
                {
                    TaskCalendar.SelectedDates.Add(task.ReminderDate.Value);
                }
            }

            // Keep the selected filter date highlighted even if it doesn't have a task yet
            if (filterDate.HasValue && !TaskCalendar.SelectedDates.Contains(filterDate.Value))
            {
                TaskCalendar.SelectedDates.Add(filterDate.Value);
            }
            _isUpdatingCalendar = false;

            // 2. Filter the tasks if a date was clicked
            List<UserTaskModel> displayTasks = allTasks;
            if (filterDate.HasValue)
            {
                displayTasks = allTasks.FindAll(t => t.ReminderDate.HasValue && t.ReminderDate.Value.Date == filterDate.Value.Date);

                // Add a "Show All Tasks" button at the top so the user can reset the filter
                Button btnShowAll = new Button
                {
                    Content = "View All Tasks",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2196F3")),
                    Margin = new Thickness(0, 0, 0, 15),
                    Style = (Style)FindResource("InteractiveButton")
                };
                btnShowAll.Click += (s, e) => ShowInteractiveTasks(); // Passing nothing resets it
                InteractiveTaskList.Children.Add(btnShowAll);
            }

            // 3. Render the tasks to the screen
            if (displayTasks.Count == 0)
            {
                string emptyMsg = filterDate.HasValue ? $"No tasks due on {filterDate.Value.ToShortDateString()}" : "You have no pending tasks!";
                InteractiveTaskList.Children.Add(new TextBlock { Text = emptyMsg, Foreground = Brushes.White, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });
                return;
            }

            foreach (var task in displayTasks)
            {
                Border taskCard = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444")),
                    CornerRadius = new CornerRadius(5),
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
                        ShowInteractiveTasks(filterDate); // Maintain filter on click
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
                    ShowInteractiveTasks(filterDate); // Maintain filter on click
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
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                            Foreground = Brushes.White,
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
    }
}