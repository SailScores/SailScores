using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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
