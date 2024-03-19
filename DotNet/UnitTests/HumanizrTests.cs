using Humanizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Showcase.Misc.Tests
{

    [TestClass]
    public class HumanizrTests
    {
        [TestMethod]
        public void CanGenerateFromUserLastSignOnDate()
        {
            DateTime lastSignedOn = new DateTime(2022, 3, 23, 13, 19, 42, DateTimeKind.Utc);
            Console.WriteLine(lastSignedOn);

            string lastActivityText = lastSignedOn.Humanize(utcDate: false, DateTime.UtcNow);
            Console.WriteLine(lastActivityText);
        }

        [TestMethod]
        public void CanGenerateLastSecondsAgo()
        {
            string lastActivityText = DateTime.UtcNow.Humanize(utcDate: true, DateTime.UtcNow.AddSeconds(20));
            Console.WriteLine(lastActivityText);
        }

        [TestMethod]
        public void CanGenerateLastMinutesAgo()
        {
            string lastActivityText = DateTime.UtcNow.Humanize(utcDate: true, DateTime.UtcNow.AddMinutes(20));
            Console.WriteLine(lastActivityText);
        }

        [TestMethod]
        public void CanGenerateLastHoursAgo()
        {
            string lastActivityText = DateTime.UtcNow.Humanize(utcDate: true, DateTime.UtcNow.AddHours(2));
            Console.WriteLine(lastActivityText);
        }

        [TestMethod]
        public void CanGenerateLastDaysAgo()
        {
            string lastActivityText = DateTime.UtcNow.Humanize(utcDate: true, DateTime.UtcNow.AddDays(4));
            Console.WriteLine(lastActivityText);
        }

        [TestMethod]
        public void CanGenerateLastMonthsAgo()
        {
            string lastActivityText = DateTime.UtcNow.Humanize(utcDate: true, DateTime.UtcNow.AddMonths(6));
            Console.WriteLine(lastActivityText);
        }

        [TestMethod]
        public void CanGenerateLastYeasAgo()
        {
            string lastActivityText = DateTime.UtcNow.Humanize(utcDate: true, DateTime.UtcNow.AddYears(1));
            Console.WriteLine(lastActivityText);

        }

    }

}
