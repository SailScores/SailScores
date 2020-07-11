using System;

namespace SailScores.Web.Models.SailScores
{
    public class AdminToDoViewModel
    {
        public String Title { get; set; }
        public String Details { get; set; }
        public bool Completed { get; set; }
        public ToDoLinkViewModel Link { get; set; }

    }
}
