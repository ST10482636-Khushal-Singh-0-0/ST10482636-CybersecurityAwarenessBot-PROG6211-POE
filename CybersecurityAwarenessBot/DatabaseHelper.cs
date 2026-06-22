using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient; // Requires MySql.Data NuGet package

namespace CybersecurityAwarenessBot
{

    // A structured model representing a single task. 
    // This allows the UI to easily bind to and display task properties.

    public class UserTaskModel
    {
        public int TaskId { get; set; }
        // Initialized with string.Empty to prevent null reference warnings in C# 10+
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? ReminderDate { get; set; }
    }


    // Handles all external database communications (CRUD Operations).

    public class DatabaseHelper
    {
        // IMPORTANT: Ensure MySQL is running on localhost (e.g., via XAMPP) 
        // and 'CybersecurityBotDB' is created. Add your password to 'Pwd=' if configured.
        private readonly string _connectionString = "Server=localhost;Database=CybersecurityBotDB;Uid=root;Pwd=;";

    
        // Inserts a new task into the database. Uses Parameterized queries to prevent SQL Injection attacks.
    
        public void AddUserTask(string title, string description, DateTime? reminderDate)
        {
            // The 'using' statement ensures the connection is automatically closed and disposed of after execution
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO UserTasks (Title, Description, ReminderDate) VALUES (@Title, @Description, @ReminderDate)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        // Safely binding parameters
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Description", description);
                        // Handle potential null dates appropriately for MySQL
                        cmd.Parameters.AddWithValue("@ReminderDate", reminderDate.HasValue ? (object)reminderDate.Value : DBNull.Value);

                        cmd.ExecuteNonQuery(); // Executes the INSERT command
                    }
                }
                catch (Exception ex) { Console.WriteLine($"DB Error: {ex.Message}"); }
            }
        }

    
        // Retrieves all tasks from the database and maps them to UserTaskModel objects.
    
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
                        // Loop through result set and construct model objects
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

    
        // Updates a specific task's status to Completed based on its exact ID.
    
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

    
        // Permanently removes a task from the database based on its exact ID.
    
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

        // Legacy support methods (Maintained in case older text-based command logic is required)
        public List<string> GetAllTasks() { return new List<string>(); }
        public bool MarkTaskAsCompleted(string searchTitle) { return false; }
        public bool MarkTaskCompleteByListNumber(int listNumber) { return false; }
    }
}