
/*  Constructor: Initialize a new user with default or specified attributes
    Login/Logout: Authenticate a user and manage user sessions
    Register: Create a new user account with unique username and password
    EditProfile: Allow users to update their profile information
    ViewStats: Display user stats such as win/loss record, number of battles, etc
    AddCard: Add a new card to the user's collection
    RemoveCard: Remove a card from the user's collection
    CreateDeck: Allow users to create or modify their deck from their card collection
    PurchasePackage: Handle the logic for users to buy card packages with virtual coins
    ListCards: List all cards owned by the user
    ListDecks: Show all decks created by the user
    TradeCard: Facilitate card trading between users
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Net.NetworkInformation;
using Microsoft.VisualBasic;
using MTCG.Server;
using System.Drawing;
using Npgsql;
using BCrypt.Net;

namespace MTCG.Models {
    public class User {
        // Public Properties
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
        public string? Info { get; set; }
        public string? Image { get; set; }

        //public Methods
        //Save a new User and an entry into battle statistics in the database
        public void CreateUser(HttpSvrEventArgs e) {
            try {
                User? newuser = JsonSerializer.Deserialize<User>(e.Payload);
                if (string.IsNullOrWhiteSpace(newuser?.Username) || string.IsNullOrWhiteSpace(newuser?.Password)) {
                    e.Reply(400, "Error: Username or password cannot be empty.");
                    return;
                }

                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();
                using var transaction = conn.BeginTransaction();

                var insertUserCmd = "INSERT INTO users (username, password, coins) VALUES (@p1, @p2, @p3)";
                using (var cmd = new NpgsqlCommand(insertUserCmd, conn, transaction)) {
                    cmd.Parameters.AddWithValue("@p1", newuser.Username);
                    cmd.Parameters.AddWithValue("@p2", BCrypt.Net.BCrypt.HashPassword(newuser.Password));
                    cmd.Parameters.AddWithValue("@p3", 20);
                    cmd.ExecuteNonQuery();
                }

                var insertStatsCmd = "INSERT INTO stats (wins, losses, elo, username) VALUES (@p1, @p2, @p3, @p4)";
                using (var cmd = new NpgsqlCommand(insertStatsCmd, conn, transaction)) {
                    cmd.Parameters.AddWithValue("@p1", 0);
                    cmd.Parameters.AddWithValue("@p2", 0);
                    cmd.Parameters.AddWithValue("@p3", 100);
                    cmd.Parameters.AddWithValue("@p4", newuser.Username);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                e.Reply(200, "User created successfully");
            }
            catch (NpgsqlException ex) {
                if (ex.SqlState == "23505") {
                    e.Reply(400, "Error: Username already in use.");
                }
                else {
                    e.Reply(400, "Error occurred while creating user.");
                }
            }
            catch {
                e.Reply(400, "Error occurred while creating user.");
            }
        }

        //Check if Username exists in the database and if the authorization via token is valid
        public void LoginUser(HttpSvrEventArgs e) {
            try {
                User? user = JsonSerializer.Deserialize<User>(e.Payload);
                if (string.IsNullOrWhiteSpace(user?.Username) || string.IsNullOrWhiteSpace(user?.Password)) {
                    e.Reply(400, "Error: Username or password cannot be empty.");
                    return;
                }

                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string? passwordHash = null;
                using (var cmd = new NpgsqlCommand("SELECT password FROM users WHERE username = @username", conn)) {
                    cmd.Parameters.AddWithValue("@username", user.Username);
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            passwordHash = reader.GetString(0);
                        }
                    }
                }

                if (passwordHash == null || !BCrypt.Net.BCrypt.Verify(user.Password, passwordHash)) {
                    e.Reply(400, "Error: Incorrect username or password.");
                    return;
                }

                // Token handling (simplified for brevity)
                e.Reply(200, "User logged in successfully");
            }
            catch {
                e.Reply(400, "Error occurred while logging in.");
            }
        }

        // The User should aquire a carddeck from the server
        public void aquireCarddeck(HttpSvrEventArgs e, Token userToken) {
            try {
                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                Guid[] cards = new Guid[5];
                int pid = 0;
                int coins = 0;

                // Check if User has enough coins
                using (var cmd = new NpgsqlCommand("SELECT coins FROM users WHERE username = @p1", conn)) {
                    cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser ?? "");
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            coins = reader.GetInt32(0);
                        }
                    }
                }

                if (coins < 5) {
                    e.Reply(400, "Funds are too low.");
                    return;
                }

                // Retrieve first carddeck from the database
                using (var cmd = new NpgsqlCommand("SELECT pid, card1, card2, card3, card4, card5 FROM packages ORDER BY pid ASC LIMIT 1", conn))
                using (var reader = cmd.ExecuteReader()) {
                    if (reader.Read()) {
                        pid = reader.GetInt32(0);
                        for (int i = 0; i < 5; i++) {
                            cards[i] = reader.GetGuid(i + 1);
                        }
                    }
                }

                if (cards[0] == Guid.Empty) {
                    e.Reply(400, "Carddecks not available.");
                    return;
                }

                // Transaction for updating cards and user coins
                using var transaction = conn.BeginTransaction();

                foreach (Guid cardid in cards) {
                    using (var cmd = new NpgsqlCommand("UPDATE cards SET username = @p1 WHERE id = @p2", conn)) {
                        cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser ?? "");
                        cmd.Parameters.AddWithValue("@p2", cardid);
                        cmd.ExecuteNonQuery();
                    }
                }

                using (var cmd = new NpgsqlCommand("DELETE FROM packages WHERE pid = @p1", conn)) {
                    cmd.Parameters.AddWithValue("@p1", pid);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand("UPDATE users SET coins = @p1 WHERE username = @p2", conn)) {
                    cmd.Parameters.AddWithValue("@p1", coins - 5);
                    cmd.Parameters.AddWithValue("@p2", userToken.LoggedInUser ?? "");
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                e.Reply(200, "Carddeck aquired!");
            }
            catch {
                e.Reply(400, "Error occurred while acquiring Carddeck.");
            }
        }
        // DB request for User data
        public void GetUserData(HttpSvrEventArgs e, Token userToken) {
            try {
                string[] pathUser = e.Path.Split("/");
                // Check if username matches token
                if (userToken.LoggedInUser == pathUser[2]) {
                    var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                    using var conn = new NpgsqlConnection(connectionString);
                    conn.Open();

                    string replyString = "Your Profile: \n";
                    using (var cmd = new NpgsqlCommand("SELECT username, coins, name, info, image FROM users WHERE username = @p1", conn)) {
                        cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser);
                        using (var reader = cmd.ExecuteReader()) {
                            while (reader.Read()) {
                                replyString += "username: " + reader.GetString(0) +
                                    "\ncoins: " + reader.GetInt32(1) +
                                    "\nname: " + (reader.IsDBNull(2) ? "" : reader.GetString(2)) +
                                    "\ninfo: " + (reader.IsDBNull(3) ? "" : reader.GetString(3)) +
                                    "\nimage: " + (reader.IsDBNull(4) ? "" : reader.GetString(4)) + "\n";
                            }
                        }
                    }
                    e.Reply(200, replyString);
                }
                else {
                    e.Reply(400, "Authorization does not match request.");
                }
            }
            catch {
                e.Reply(400, "An Error occured while gathering the data.");
            }
        }

        // Update User data in DB
        public void UpdateUserData(HttpSvrEventArgs e, Token userToken) {
            try {
                string[] pathUser = e.Path.Split("/");
                // Check if username matches token
                if (userToken.LoggedInUser == pathUser[2]) {
                    User? userUpdate = JsonSerializer.Deserialize<User>(e.Payload);

                    var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                    using var conn = new NpgsqlConnection(connectionString);
                    conn.Open();

                    // Update user profile
                    using (var cmd = new NpgsqlCommand("UPDATE users SET name = @p1, info = @p2, image = @p3 WHERE username = @p4", conn)) {
                        cmd.Parameters.AddWithValue("@p1", userUpdate?.Name ?? string.Empty);
                        cmd.Parameters.AddWithValue("@p2", userUpdate?.Info ?? string.Empty);
                        cmd.Parameters.AddWithValue("@p3", userUpdate?.Image ?? string.Empty);
                        cmd.Parameters.AddWithValue("@p4", userToken.LoggedInUser);
                        cmd.ExecuteNonQuery();
                    }

                    e.Reply(200, "Profile update successful.");
                }
                else {
                    e.Reply(400, "Authorization doesn't match request.");
                }
            }
            catch {
                e.Reply(400, "Error occurred while updating profile.");
            }
        }

        //Update User's stats - Client connection
        public void GetStats(HttpSvrEventArgs e, Token userToken) {
            try {
                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string replyString = "Your Stats: \n";
                using (var cmd = new NpgsqlCommand("SELECT wins, losses, elo FROM stats WHERE username = @p1", conn)) {
                    cmd.Parameters.AddWithValue("@p1", userToken.LoggedInUser ?? "");
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            replyString += "username: " + userToken.LoggedInUser +
                                "\nwins: " + reader.GetInt32(0) +
                                "\nlosses: " + reader.GetInt32(1) +
                                "\nelo: " + reader.GetInt32(2) + "\n";
                        }
                    }
                }
                e.Reply(200, replyString);
            }
            catch {
                e.Reply(400, "Error occurred while fetching profile data.");
            }
        }

        public void GetScoreboard(HttpSvrEventArgs e, Token userToken) {
            try {
                var connectionString = "Host=localhost; Username=user1; Password=userpwd1; Database=swen1db";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string replyString = "Top 10 Scores: \n";
                using (var cmd = new NpgsqlCommand("SELECT username, wins, losses, elo FROM stats ORDER BY elo DESC, wins DESC, losses ASC, username DESC LIMIT 10;", conn)) {
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            replyString += "username: " + reader.GetString(0) +
                                "\nwins: " + reader.GetInt32(1) +
                                "\nlosses: " + reader.GetInt32(2) +
                                "\nelo: " + reader.GetInt32(3) + "\n";
                        }
                    }
                }
                e.Reply(200, replyString);
            }
            catch {
                e.Reply(400, "Error occurred while fetching scoreboard data.");
            }
        }

    }
}
