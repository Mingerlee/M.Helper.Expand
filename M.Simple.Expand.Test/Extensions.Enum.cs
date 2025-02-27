using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M.Simple.Expand.Models;
using M.Simple.Expand.Test.TestAttributes;
using Newtonsoft.Json;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsEnum
    {
        [TestMethod]
        public void TestEnum()
        {
            Console.WriteLine(PersonEnum.Teacher.GetDescription());
            Console.WriteLine(PersonEnum.Teacher.GetDisplayName());
            Console.WriteLine(PersonEnum.Teacher.GetDisplayDescription());
            
            Console.WriteLine(JsonConvert.SerializeObject(new ComboBoxIntViewModel().ToComboBoxs<PersonEnum>()));

            Console.WriteLine(JsonConvert.SerializeObject(typeof(PersonEnum).ToTextDescriptions()));

            Console.WriteLine(JsonConvert.SerializeObject(typeof(PersonEnum).ToValueDescriptions()));

            var s = "Professor".ToEnum<PersonEnum>();

            Console.WriteLine(s.ToInt32());

           var title= s.GetAttributeOfType<MyTitleAttribute>().Title;
            Console.WriteLine(title);

            int value = 0;
            int value1 = 2;
            int? value2 = 4;
            int? value3 = null;
            Console.WriteLine();
            Console.WriteLine(value.ToEnumText<PersonEnum>());
            Console.WriteLine(value1.ToEnumText<PersonEnum>());
            Console.WriteLine(value2.ToEnumText<PersonEnum>());
            Console.WriteLine(value3.ToEnumText<PersonEnum>());

        }
    }
}
