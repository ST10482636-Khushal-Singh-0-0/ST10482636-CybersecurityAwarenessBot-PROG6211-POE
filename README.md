# Cybersecurity Awareness Assistant

A sleek, interactive desktop Graphical User Interface (GUI) application built with C# and Windows Presentation Foundation (WPF). This virtual assistant is designed to educate users about common online threats—such as phishing, scams, and identity theft—with a specific focus on the South African digital landscape.

Beyond just a chatbot, this application features a fully interactive task manager, dynamic text-to-speech capabilities, a retro-arcade aesthetic, and a built-in interactive cybersecurity quiz.

---

## Features

* **Retro-Arcade Animated UI:** A custom-styled, highly responsive interface featuring neon accents, sliding message animations, and custom UI templates.
* **Text-to-Speech:** Automatically reads out bot responses using `System.Speech.Synthesis`, creating a highly immersive, hands-free assistant experience.
* **Interactive Task Dashboard:** A built-in database-driven task manager. Add, complete, and delete cybersecurity-related tasks or reminders directly from a fluid UI overlay.
* **Dynamic Cybersecurity Quiz:** Test your knowledge with a built-in multiple-choice quiz that temporarily locks standard input and dynamically generates clickable option buttons.
* **Smart Natural Language Processing (NLP):** Uses Regular Expressions (Regex) to parse complex commands like *"remind me to update my firewall in 3 days"* and automatically formats them into database entries.
* **Persistent Memory & Activity Logging:** Remembers your name and logs your interactions for the current session.

---

## Technologies Used

* **Language:** C# 10.0+
* **Framework:** .NET 8.0 / 10.0 (Windows Desktop Development)
* **UI Technology:** Windows Presentation Foundation (WPF) / XAML
* **Database:** MySQL (via `MySql.Data`)
* **Audio/Speech:** `System.Speech` and `System.Media`

---

## Setup & Installation Instructions

Follow these steps to get the application running on your local machine.

### 1. Prerequisites
* **Visual Studio 2022** with the ".NET desktop development" workload installed.
* **MySQL Server** installed locally (you can use standalone MySQL or a package like XAMPP/WAMP).

### 2. Database Configuration
Before running the application, you must configure the local MySQL database to store your tasks and reminders.

1. Open your MySQL command line or a tool like phpMyAdmin/MySQL Workbench.
2. Execute the following SQL script to create the database and required table:
   ```sql
   CREATE DATABASE CybersecurityBotDB;
   
   USE CybersecurityBotDB;
   
   CREATE TABLE UserTasks (
       TaskId INT AUTO_INCREMENT PRIMARY KEY,
       Title VARCHAR(255) NOT NULL,
       Description TEXT,
       ReminderDate DATETIME NULL,
       IsCompleted BOOLEAN DEFAULT FALSE
   );
