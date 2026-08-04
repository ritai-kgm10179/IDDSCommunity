using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Eventing.Reader;
namespace IdsServiceForWindowsTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestEventLogReader()
        {
            string eventLogQuery = @"<QueryList>
                  <Query Id=""0"" Path=""Security"">
                    <Select Path=""Security"">
                        *[System[(EventID=4625) and
                        TimeCreated[timediff(@SystemTime) &lt;= 86400000]]]
                    </Select>

                  </Query>
                </QueryList>";


            try
            {
                EventLogQuery query = new("Security", PathType.LogName,
                    string.Format(eventLogQuery));
                EventLogReader rdr = new(query);

                EventRecord eventRecord = rdr.ReadEvent();
                if (eventRecord != null)
                {
                    foreach (string s in eventRecord.KeywordsDisplayNames)
                    {
                        System.Diagnostics.Debug.Print(s);

                    }
                    string[] xPathProperties = new string[1] { @"Event/EventData/Data[@Name=""IpAddress""]" };

                    EventLogPropertySelector props = new(xPathProperties);
                    System.Diagnostics.Debug.Print(((EventLogRecord)eventRecord).GetPropertyValues(props)[0].ToString());

                    System.Diagnostics.Debug.Print(eventRecord.Properties[0].Value.ToString());
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (EventLogException)
            {
            }
        }



        [TestMethod]
        public void WriteEventLogTest()
        {
            try
            {
                Cyberarms.IntrusionDetection.WindowsLogManager.Instance.WriteEntry("Test from unit test", System.Diagnostics.EventLogEntryType.Information, 1, 1);
            }
            catch (System.Security.SecurityException)
            {
                // Requires administrator privileges to register event source
            }
            catch (UnauthorizedAccessException)
            {
                // Requires administrator privileges
            }
        }
    }
}
