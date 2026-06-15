using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace CybersecurityAwarenessBot
{
    public class DatabaseHelper
    {
        // IMPORTANT: Ensure this matches your MySQL setup. If you have a password, put it between the Pwd= and the semicolon.
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                }
            }
        }

        public List<string> GetAllTasks()
        {
            List<string> tasks = new List<string>();

            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    // ORDER BY TaskId ASC ensures they always appear in the exact order you added them
                    string query = "SELECT Title, ReminderDate, IsCompleted FROM UserTasks ORDER BY TaskId ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string title = reader["Title"].ToString() ?? "Unknown Task";
                            string status = Convert.ToBoolean(reader["IsCompleted"]) ? "[Completed]" : "[Pending]";
                            string taskInfo = $"{status} {title}";

                            if (reader["ReminderDate"] != DBNull.Value)
                            {
                                DateTime reminder = Convert.ToDateTime(reader["ReminderDate"]);
                                taskInfo += $" (Reminder: {reminder.ToShortDateString()})";
                            }
                            tasks.Add(taskInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tasks.Add($"MySQL Error: {ex.Message}");
                }
            }
            return tasks;
        }

        public bool MarkTaskAsCompleted(string searchTitle)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE UserTasks SET IsCompleted = TRUE WHERE Title LIKE @SearchTitle AND IsCompleted = FALSE LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SearchTitle", "%" + searchTitle + "%");
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                    return false;
                }
            }
        }

        public bool MarkTaskCompleteByListNumber(int listNumber)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();

                    // 1. Find the internal Database ID for the item at that position in your list
                    int targetTaskId = -1;
                    string selectQuery = "SELECT TaskId FROM UserTasks ORDER BY TaskId ASC LIMIT 1 OFFSET @Offset";

                    using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn))
                    {
                        selectCmd.Parameters.AddWithValue("@Offset", listNumber - 1);
                        object result = selectCmd.ExecuteScalar();
                        if (result != null)
                        {
                            targetTaskId = Convert.ToInt32(result);
                        }
                    }

                    // 2. If the item exists, update its status
                    if (targetTaskId != -1)
                    {
                        string updateQuery = "UPDATE UserTasks SET IsCompleted = TRUE WHERE TaskId = @TaskId AND IsCompleted = FALSE";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@TaskId", targetTaskId);
                            int rowsAffected = updateCmd.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                    return false;
                }
            }
        }
    }
}