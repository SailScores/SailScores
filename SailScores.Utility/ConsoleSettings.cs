using System;

namespace SailScores.Utility
{
    class ConsoleSettings : Api.Services.ISettings
    {
        private string _serverUrl;
        //            = "https://localhost:5001/";
        public string ServerUrl
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_serverUrl))
                {
                    Console.Write("Server? > ");
                    _serverUrl = Console.ReadLine();
                }
                return _serverUrl;
            }
            set
            {
                _serverUrl = value;
            }
        }
        private string _userName;
        public string UserName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_userName))
                {
                    Console.Write("UserName? > ");
                    _userName = Console.ReadLine();
                }
                return _userName;
            }
            set
            {
                _userName = value;
            }
        }

        private string _password;
        public string Password
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_password))
                {
                    Console.Write("Password? > ");
                    _password = ReadPassword();
                }
                return _password;
            }
            set
            {
                _password = value;
            }
        }
        public static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }

            // add a new line because user pressed enter at the end of their password
            Console.WriteLine();
            return password;
        }
    }
}
