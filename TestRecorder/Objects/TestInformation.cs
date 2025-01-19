using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountNetAppTests.Objects
{
    public class TestInformation
    {
        public string TestName { get; set; }
        public int TestIteration { get; set; } = 1;
        public dynamic TestData { get; set; }
    }
}
