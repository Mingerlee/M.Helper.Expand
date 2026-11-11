using System.Globalization;
using M.Simple.Expand.Enums;

namespace M.Simple.Expand.Test;

[TestClass]
public class DateDifferenceTests
{
    [DataTestMethod]
    [DataRow("2026-01-15", "2026-01-15", 0, 0)]
    [DataRow("2026-01-01", "2026-01-31", 0, 0)]
    [DataRow("2026-01-31", "2026-02-01", 1, 0)]
    [DataRow("2026-01-15", "2026-02-14", 1, 0)]
    [DataRow("2026-01-15", "2026-02-15", 1, 1)]
    [DataRow("2026-01-15", "2026-02-16", 1, 1)]
    [DataRow("2026-01-31", "2026-02-27", 1, 0)]
    [DataRow("2026-01-31", "2026-02-28", 1, 1)]
    [DataRow("2024-01-31", "2024-02-28", 1, 0)]
    [DataRow("2024-01-31", "2024-02-29", 1, 1)]
    [DataRow("2024-02-29", "2025-02-27", 12, 11)]
    [DataRow("2024-02-29", "2025-02-28", 12, 12)]
    [DataRow("2026-01-31", "2026-03-28", 2, 1)]
    [DataRow("2026-01-31", "2026-03-30", 2, 1)]
    [DataRow("2026-01-31", "2026-03-31", 2, 2)]
    [DataRow("2026-01-30", "2026-03-29", 2, 1)]
    [DataRow("2026-01-30", "2026-03-30", 2, 2)]
    [DataRow("2026-02-28", "2026-03-28", 1, 1)]
    [DataRow("2026-04-30", "2026-05-29", 1, 0)]
    [DataRow("2026-04-30", "2026-05-30", 1, 1)]
    [DataRow("2025-12-31", "2026-01-01", 1, 0)]
    [DataRow("2025-12-31", "2026-01-31", 1, 1)]
    [DataRow("0001-01-01", "9999-12-31", 119987, 119987)]
    [DataRow("0001-01-31", "9999-12-30", 119987, 119986)]
    public void DiffMonth_UsesDefinedBoundariesAndSymmetricDirection(string from, string to, int calendarMonths, int fullMonths)
    {
        DateTime start = DateTime.ParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        DateTime end = DateTime.ParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.AreEqual(calendarMonths, start.DiffMonth(end));
        Assert.AreEqual(-calendarMonths, end.DiffMonth(start));
        foreach (var mode in Enum.GetValues<DiffMonthWayEnum>())
        {
            int expected = mode == DiffMonthWayEnum.精确 ? fullMonths : calendarMonths;
            Assert.AreEqual(expected, start.DiffMonth(end, mode), mode.ToString());
            Assert.AreEqual(-expected, end.DiffMonth(start, mode), mode + "反向");
        }
    }

    [DataTestMethod]
    [DataRow(DiffMonthWayEnum.简单)]
    [DataRow(DiffMonthWayEnum.精确)]
    [DataRow(DiffMonthWayEnum.跨月)]
    public void DiffMonth_IgnoresTimeOfDayAndMatchesDiffDaysDirection(DiffMonthWayEnum mode)
    {
        var start = new DateTime(2026, 1, 15, 23, 59, 59);
        var end = new DateTime(2026, 2, 15);
        Assert.AreEqual(0, start.DiffMonth(start.Date, mode));
        Assert.AreEqual(0, start.Date.DiffMonth(start, mode));
        Assert.AreEqual(1, start.DiffMonth(end, mode));
        Assert.AreEqual(-1, end.DiffMonth(start, mode));
        Assert.AreEqual(Math.Sign(start.DiffDays(end)), Math.Sign(start.DiffMonth(end, mode)));
        Assert.AreEqual(Math.Sign(end.DiffDays(start)), Math.Sign(end.DiffMonth(start, mode)));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(4)]
    public void DiffMonth_RejectsUndefinedModeEvenForSameDay(int mode)
    {
        var date = new DateTime(2026, 11, 11);
        var error = Assert.ThrowsException<ArgumentOutOfRangeException>(() => date.DiffMonth(date, (DiffMonthWayEnum)mode));
        Assert.AreEqual("diffMonthWay", error.ParamName);
    }
}
