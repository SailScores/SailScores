using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Utility
{
    class ConsoleSettings : Api.Services.ISettings
    {
        private string _serverUrl
            = "https://localhost:5001/";
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
                    _password = Console.ReadLine();
                }
                return _password;
            }
            set
            {
                _password = value;
            }
        }
    }
}
