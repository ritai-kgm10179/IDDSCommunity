using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Eventing.Reader;
namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public class UnitTest1
{
    /// <summary>
    /// Executes the test event log reader operation.
    /// </summary>

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
                string[] xPathProperties = [@"Event/EventData/Data[@Name=""IpAddress""]"];

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



}
