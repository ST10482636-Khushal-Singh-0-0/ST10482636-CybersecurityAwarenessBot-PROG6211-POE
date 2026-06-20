using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace CybersecurityAwarenessBot
{
    // A clean model to hold task data for the UI
    public class UserTaskModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? ReminderDate { get; set; }
    }

    public class DatabaseHelper
    {
        private readonly string _connectionString = "Server=localhost;Database=CybersecurityBotDB;Uid=root;Pwd=;";

        public void AddUserTask(string title, string description, DateTime? reminderDate)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO UserTasks (Title, Description, ReminderDate) VALUES (@Title, @Description, @ReminderDate)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@ReminderDate", reminderDate.HasValue ? (object)reminderDate.Value : DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) { Console.WriteLine($"DB Error: {ex.Message}"); }
            }
        }

        // Fetches tasks as structured objects for the interactive dashboard
        public List<UserTaskModel> GetTaskModels()
        {
            List<UserTaskModel> tasks = new List<UserTaskModel>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT TaskId, Title, ReminderDate, IsCompleted FROM UserTasks ORDER BY TaskId ASC";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new UserTaskModel
                            {
                                TaskId = Convert.ToInt32(reader["TaskId"]),
                                Title = reader["Title"].ToString() ?? "Unknown",
                                IsCompleted = Convert.ToBoolean(reader["IsCompleted"]),
                                ReminderDate = reader["ReminderDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReminderDate"]) : (DateTime?)null
                            });
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"DB Error: {ex.Message}"); }
            }
            return tasks;
        }

        // Direct ID complete for the UI Buttons
        public void MarkTaskCompleteById(int taskId)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE UserTasks SET IsCompleted = TRUE WHERE TaskId = @TaskId";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TaskId", taskId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) { Console.WriteLine($"DB Error: {ex.Message}"); }
            }
        }

        // Delete a task from the database
        public void DeleteTaskById(int taskId)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "DELETE FROM UserTasks WHERE TaskId = @TaskId";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TaskId", taskId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) { Console.WriteLine($"DB Error: {ex.Message}"); }
            }
        }

        public List<string> GetAllTasks() { return new List<string>(); }
        public bool MarkTaskAsCompleted(string searchTitle) { return false; }
        public bool MarkTaskCompleteByListNumber(int listNumber) { return false; }
    }
}