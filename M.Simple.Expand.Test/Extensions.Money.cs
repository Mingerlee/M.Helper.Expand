using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsMoney
    {
        [TestMethod]
        public void TestMoney()
        {
            decimal money = -545414.451M;

            Console.WriteLine(money.ToChinese());

            Console.WriteLine(money.ToRound(1));
            Console.WriteLine(money.ToRound(2));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C0));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C0_en_US));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C0_zh_CN));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C1));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C1_en_US));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C1_zh_CN));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C2));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C2_en_US));
            Console.WriteLine(money.ToFormatMoney(Enums.FormatMoneyEnum.C2_zh_CN));
            Console.WriteLine();
            decimal? money1 = 5432112345.4545M;
            Console.WriteLine(money1.ToFormatMoney(Enums.FormatMoneyEnum.C1));
            Console.WriteLine(money1.ToFormatMoney(Enums.FormatMoneyEnum.C1_en_US));
            Console.WriteLine(money1.ToFormatMoney(Enums.FormatMoneyEnum.C1_zh_CN));
            Console.WriteLine(money1.ToFormatMoney(Enums.FormatMoneyEnum.C2));
            Console.WriteLine(money1.ToFormatMoney(Enums.FormatMoneyEnum.C2_en_US));
            Console.WriteLine(money1.ToFormatMoney(Enums.FormatMoneyEnum.C2_zh_CN));
            Console.WriteLine();
            decimal? money2 = null;
            Console.WriteLine(money2.ToFormatMoney(Enums.FormatMoneyEnum.C1));
            Console.WriteLine(money2.ToFormatMoney(Enums.FormatMoneyEnum.C1_en_US));
            Console.WriteLine(money2.ToFormatMoney(Enums.FormatMoneyEnum.C1_zh_CN));
            Console.WriteLine(money2.ToFormatMoney(Enums.FormatMoneyEnum.C2));
            Console.WriteLine(money2.ToFormatMoney(Enums.FormatMoneyEnum.C2_en_US));
            Console.WriteLine(money2.ToFormatMoney(Enums.FormatMoneyEnum.C2_zh_CN));
            Console.WriteLine();
            var strMoney1 = money1.ToFormatMoney(Enums.FormatMoneyEnum.C2_zh_CN);
            Console.WriteLine(strMoney1.MoneyStringToDecimal());
            var strMoney = money1.ToFormatMoney(Enums.FormatMoneyEnum.C2_en_US);
            Console.WriteLine(strMoney.MoneyStringToDecimal());
            var strMoney2 = money2.ToFormatMoney(Enums.FormatMoneyEnum.C2_en_US);
            Console.WriteLine(strMoney2.MoneyStringToDecimal());
            Console.WriteLine("#123456,45a.sa".MoneyStringToDecimal());
        }
        [TestMethod]
        public void Ceiling()
        {
            decimal money = 212.0001M;

            Console.WriteLine($"212.0001->{money.CeilingSpecial(3):#0.0000}");

            money = 212.0010M;

            Console.WriteLine($"212.0010->{money.CeilingSpecial(3):#0.0000}");

            money = 212.0000M;

            Console.WriteLine($"212.0000->{money.CeilingSpecial(3):#0.0000}");

            money = 212.0100M;

            Console.WriteLine($"212.0100->{money.CeilingSpecial(3):#0.0000}");

            money = 212.0110M;

            Console.WriteLine($"212.0110->{money.CeilingSpecial(3):#0.0000}");

            money = 212.0101M;

            Console.WriteLine($"212.0101->{money.CeilingSpecial(3):#0.0000}");

            money = 212.0011M;

            Console.WriteLine($"212.0011->{money.CeilingSpecial(3):#0.0000}");

        }
    }
}
