using System;
using System.Collections.Generic;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace CybersecurityAwarenessBot
{
    public class UserTaskModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? ReminderDate { get; set; }
    }

    public class DatabaseHelper
    {
        // IMPORTANT: Add your MySQL password to Pwd= if you have one!
        private readonly string _connectionString = "Server=localhost;Database=CybersecurityBotDB;Uid=root;Pwd=;";


        // Attempts to add a task. Returns TRUE if successful, FALSE if the DB connection fails.
        // </summary>
        public bool AddUserTask(string title, string description, DateTime? reminderDate)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open(); // Will immediately throw an exception if MySQL is offline
                    string query = "INSERT INTO UserTasks (Title, Description, ReminderDate) VALUES (@Title, @Description, @ReminderDate)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Description", description ?? string.Empty);
                        cmd.Parameters.AddWithValue("@ReminderDate", reminderDate.HasValue ? (object)reminderDate.Value : DBNull.Value);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0; // Ensures the row was actually written
                    }
                }
            }
            catch (MySqlException sqlEx)
            {
                Debug.WriteLine($"[MySQL Specific Error]: {sqlEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[General DB Error]: {ex.Message}");
                return false;
            }
        }


        // Retrieves tasks. If the DB is offline, safely returns an empty list so the app doesn't crash.
        // </summary>
        public List<UserTaskModel> GetTaskModels()
        {
            List<UserTaskModel> tasks = new List<UserTaskModel>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
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
            }
            catch (MySqlException sqlEx)
            {
                Debug.WriteLine($"[MySQL Sync Error]: {sqlEx.Message}");
            }
            return tasks; // Returns empty list on failure
        }


        // Updates a task. Returns TRUE if successful.
        // </summary>
        public bool MarkTaskCompleteById(int taskId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE UserTasks SET IsCompleted = TRUE WHERE TaskId = @TaskId";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TaskId", taskId);
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Update Error]: {ex.Message}");
                return false;
            }
        }


        // Deletes a task. Returns TRUE if successful.
        // </summary>
        public bool DeleteTaskById(int taskId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM UserTasks WHERE TaskId = @TaskId";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TaskId", taskId);
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB Delete Error]: {ex.Message}");
                return false;
            }
        }

        // Legacy Fallbacks
        public List<string> GetAllTasks() { return new List<string>(); }
        public bool MarkTaskAsCompleted(string searchTitle) { return false; }
        public bool MarkTaskCompleteByListNumber(int listNumber) { return false; }
    }
}