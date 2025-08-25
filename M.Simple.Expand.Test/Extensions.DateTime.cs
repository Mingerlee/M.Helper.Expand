using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M.Simple.Expand.Enums;
using M.Simple.Expand.Models;

namespace M.Simple.Expand.Test
{
    [TestClass]
    public class ExtensionsDateTime
    {
        [TestMethod]
        public void TestDate()
        {
            DateTime dt = DateTime.Now;
            DateTime? dt1 = DateTime.Now;
            DateTime dt2 = new(2025, 2, 20, 16, 00, 00);
            DateTime birthDate = new(1994, 2, 20);
            DateTime defaultDate = new(2025, 01, 01);
            DateTime? dt3 = null;

            Console.WriteLine(dt.ToDefaultDate());
            Console.WriteLine(dt1.ToDefaultDate());
            Console.WriteLine(dt3.ToDefaultDate());
            Console.WriteLine(dt3.ToDefaultDate(defaultDate));
            Console.WriteLine();

            Console.WriteLine(dt.ToChineseDateString());
            Console.WriteLine(dt1.ToChineseDateString());
            Console.WriteLine(dt3.ToChineseDateString());
            Console.WriteLine(dt3.ToChineseDateString(defaultDate));
            Console.WriteLine();
            Console.WriteLine(dt.ToChineseDateTimeString());
            Console.WriteLine(dt1.ToChineseDateTimeString(true));
            Console.WriteLine(dt3.ToChineseDateTimeString());
            Console.WriteLine(dt3.ToChineseDateTimeString(defaultDate));
            Console.WriteLine();

            Console.WriteLine(dt.ToTimesTampCode());
            Console.WriteLine(dt2.ToPastCurrentTime(language: LanguageEnum.en_US));
            Console.WriteLine(dt2.ToPastCurrentTime(language: LanguageEnum.zh_CN));
            Console.WriteLine(dt2.ToPastCurrentTime(new DateTime(2025, 2, 21, 16, 00, 00), LanguageEnum.en_US));
            Console.WriteLine(dt2.ToPastCurrentTime(new DateTime(2025, 2, 21, 16, 00, 00), LanguageEnum.zh_CN));
            Console.WriteLine(birthDate.CalculateAge());
            Console.WriteLine();

            Console.WriteLine(dt.Date);
            Console.WriteLine(dt1.GetDate());
            Console.WriteLine(dt3.GetDate());
            Console.WriteLine(dt3.GetDate(defaultDate));
            Console.WriteLine();

            Console.WriteLine(dt.ToLunarCalendarDate());
            Console.WriteLine(dt1.ToLunarCalendarDate());
            Console.WriteLine(dt3.ToLunarCalendarDate());
            Console.WriteLine(dt3.ToLunarCalendarDate(defaultDate));

            Console.WriteLine();

            Console.WriteLine(dt.NextDay());
            Console.WriteLine(dt1.NextDay());
            Console.WriteLine(dt3.NextDay());
            Console.WriteLine(dt3.NextDay(defaultDate));
            Console.WriteLine();

            Console.WriteLine(dt.PervDay());
            Console.WriteLine(dt1.PervDay());
            Console.WriteLine(dt3.PervDay());
            Console.WriteLine(dt3.PervDay(defaultDate));
            Console.WriteLine();
            Console.WriteLine(dt2.DiffDays(defaultDate));


            DateTime dateTime1 = new(2025, 04, 16);
            DateTime defaultDate2 = new(2025, 01, 15);
            Console.WriteLine();
            Console.WriteLine(dateTime1.ToString());
            Console.WriteLine(defaultDate2.ToString());

            Console.WriteLine(dateTime1.DiffMonth(defaultDate2, DiffMonthWayEnum.精确));
            


        }
        [TestMethod]
        public void TestIsOverlap()
        {
            List<DatePeriod> dates = [
                new DatePeriod(new DateTime(2024, 01, 01), new DateTime(2024, 05, 31)),
                new DatePeriod(new DateTime(2024, 06, 01), new DateTime(2024, 12, 31)),
                new DatePeriod(new DateTime(2025, 01, 01), new DateTime(2025, 06, 30)),
                new DatePeriod(new DateTime(2025, 07, 01), new DateTime(2025, 12, 31))
            ];
            List<DatePeriod> dates1 = [
                new DatePeriod(new DateTime(2024, 01, 01), new DateTime(2024, 05, 31)),
                new DatePeriod(new DateTime(2024, 05, 01), new DateTime(2024, 12, 31)),
                new DatePeriod(new DateTime(2025, 01, 01), new DateTime(2025, 06, 30)),
                new DatePeriod(new DateTime(2025, 06, 01), new DateTime(2025, 12, 31))
            ];
            List<DatePeriod> dates2 = [
                new DatePeriod(new DateTime(2024, 01, 01), new DateTime(2024, 05, 31)),
                new DatePeriod(new DateTime(2025, 07, 01), new DateTime(2025, 12, 31)),
                new DatePeriod(new DateTime(2024, 06, 01), new DateTime(2024, 12, 31)),
                new DatePeriod(new DateTime(2025, 01, 01), new DateTime(2025, 06, 30)),
            ];
            List<DatePeriod> dates3 = [
                new DatePeriod(new DateTime(2024, 01, 01), new DateTime(2024, 05, 31)),
                new DatePeriod(new DateTime(2024, 06, 01), new DateTime(2024, 12, 31)),
                new DatePeriod(new DateTime(2024, 01, 01), new DateTime(2025, 06, 30)),
                new DatePeriod(new DateTime(2025, 07, 01), new DateTime(2025, 12, 31))
            ];
            List<DatePeriod> dates4 = [
                //new DatePeriod(new DateTime(2024, 05, 31), new DateTime(2024, 01, 01)),
                new DatePeriod(new DateTime(2024, 06, 01), new DateTime(2024, 12, 31)),
                new DatePeriod(new DateTime(2025, 01, 01), new DateTime(2025, 06, 30)),
                new DatePeriod(new DateTime(2025, 07, 01), new DateTime(2025, 12, 31))
            ];

            Console.WriteLine(dates.IsOverlap());
            Console.WriteLine(dates1.IsOverlap());
            Console.WriteLine(dates2.IsOverlap());
            Console.WriteLine(dates3.IsOverlap());
            Console.WriteLine(dates4.IsOverlap());

        }
        [TestMethod]
        public void TestIsOverlap2()
        {
            List<DateTimePeriod> dates = [
                new DateTimePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2024, 06, 01, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];
            List<DateTimePeriod> dates1 = [
                new DateTimePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2024, 05, 31, 01, 01, 02), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 06, 30, 12, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];
            List<DateTimePeriod> dates2 = [
                new DateTimePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 02)),
                new DateTimePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2024, 05, 31, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
            ];
            List<DateTimePeriod> dates3 = [
                new DateTimePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2024, 06, 01, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];
            List<DateTimePeriod> dates4 = [
                //new DateTimePeriod(new DateTime(2024, 05, 31, 01, 01, 01), new DateTime(2024, 01, 01, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2024, 06, 01, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DateTimePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];

            Console.WriteLine(dates.IsOverlap());
            Console.WriteLine(dates1.IsOverlap());
            Console.WriteLine(dates2.IsOverlap());
            Console.WriteLine(dates3.IsOverlap());
            Console.WriteLine(dates4.IsOverlap());

        }
        [TestMethod]
        public void TestIsOverlap3()
        {
            List<DatePeriod> dates = [
                new DatePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2024, 06, 01, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];
            List<DatePeriod> dates1 = [
                new DatePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2024, 05, 31, 01, 01, 02), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 06, 30, 12, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];
            List<DatePeriod> dates2 = [
                new DatePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 02)),
                new DatePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2024, 05, 31, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
            ];
            List<DatePeriod> dates3 = [
                new DatePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2024, 05, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2024, 06, 01, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2024, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];
            List<DatePeriod> dates4 = [
                //new DateTimePeriod(new DateTime(2024, 05, 31, 01, 01, 01), new DateTime(2024, 01, 01, 01, 01, 01)),
                new DatePeriod(new DateTime(2024, 06, 01, 01, 01, 01), new DateTime(2024, 12, 31, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 01, 01, 01, 01, 01), new DateTime(2025, 06, 30, 01, 01, 01)),
                new DatePeriod(new DateTime(2025, 07, 01, 01, 01, 01), new DateTime(2025, 12, 31, 01, 01, 01))
            ];

            Console.WriteLine(dates.IsOverlap());
            Console.WriteLine(dates1.IsOverlap());
            Console.WriteLine(dates2.IsOverlap());
            Console.WriteLine(dates3.IsOverlap());
            Console.WriteLine(dates4.IsOverlap());

        }
        [TestMethod]
        public void TestIsOverlap4()
        {
            List<Person> persons =
            [
                new Person {
                     CreartDate=new DateTime(2024, 01, 01, 01, 01, 01),
                     UpdateDate=new DateTime(2024, 05, 31, 01, 01, 01)
                },
                new Person {
                     CreartDate=new DateTime(2024, 05, 31, 01, 01, 02),
                     UpdateDate=new DateTime(2024, 12, 31, 01, 01, 01)
                },
                new Person {
                     CreartDate=new DateTime(2025, 01, 01, 01, 01, 01),
                     UpdateDate=new DateTime(2025, 06, 30, 01, 01, 01)
                },
                new Person {
                     CreartDate=new DateTime(2025, 07, 01, 01, 01, 01),
                     UpdateDate=new DateTime(2025, 12, 31, 01, 01, 01)
                },
            ];
            Console.WriteLine(persons.Select(p=>new DateTimePeriod(p.CreartDate,p.UpdateDate)).ToList().IsOverlap());
        }
    }
}
