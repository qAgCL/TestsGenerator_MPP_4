using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestsGeneratorLib
{
    public class Tests
    {
        public string FileName { get; }
        public string TestCode { get; }

        public Tests(string FileName, string TestCode) {
            this.FileName = FileName;
            this.TestCode = TestCode;
        }
    }
}
