﻿using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Server
{
    public class Token {

        public Boolean IsLoggedIn { get; set; } = false;

        public Boolean IsAdmin { get; set; } = false;

        public string? LoggedInUser { get; set; }

        public void AuthenticateUser(HttpSvrEventArgs e) {
            var index = Array.FindIndex(e.Headers, x => x.Name.Contains("Authorization"));
            if (index >= 0) {
                string[] authHeader = e.Headers[index].Value.Split(' ');
                string[] tokenName = authHeader[1].Split('-');
                string passedUsername = tokenName[0];
                bool userExists = false;
                try {
                    var connectionString = "Host=localhost; Username=swe1user; Password=swe1pw; Database=swe1db";
                    using var conn = new NpgsqlConnection(connectionString);
                    conn.Open();

                    // Check if user exists
                    using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM users WHERE username = @p1)", conn)) {
                        cmd.Parameters.AddWithValue("@p1", passedUsername);
                        var result = cmd.ExecuteScalar();
                        userExists = result != null && (bool)result;
                    }

                    if (userExists) {
                        IsLoggedIn = true;
                        IsAdmin = passedUsername == "admin";
                        LoggedInUser = passedUsername;
                    }
                }
                catch (Exception ex) {
                    e.Reply(400, "Error occurred while logging in: " + ex.Message);
                }
            }
        }
    }
}
