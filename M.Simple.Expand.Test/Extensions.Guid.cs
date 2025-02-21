using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M.Simple.Expand;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsGuid
    {
        [TestMethod]
        public void TestGuid()
        {
            Console.WriteLine(new Guid().GetNextGuid());
            Console.WriteLine(new Guid().GetEncryptSecret());
        }
    }
}
