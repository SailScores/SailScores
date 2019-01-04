using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Core.Model
{
    public class File
    {
        public Guid Id { get; set; }
        public byte[] FileContents { get; set; }
        public DateTime Created { get; set; }

        public DateTime? ImportedTime { get; set; }
    }
}
