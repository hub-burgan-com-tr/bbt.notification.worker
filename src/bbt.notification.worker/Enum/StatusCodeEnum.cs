using System.ComponentModel;

namespace bbt.notification.worker.Enum
{
    public enum StatusCodeEnum
    {

        [Description("200")]
        StatusCode200 = 1,
        [Description("500")]
        StatusCode500 = 2,
        [Description("460")]
        StatusCode460 = 3,
        [Description("461")]
        StatusCode461 = 4,
        [Description("470")]
        StatusCode470 = 5
    }

    public struct StructStatusCode
    {
        public const string StatusCode200 = "Execute request is executed successfully.";

        public const string StatusCode500 = "Technical error on the system.";

        public const string StatusCode460 = "Source is not found.";

        public const string StatusCode461 = "This path is parent. You can not execute on parent paths.";

        public const string StatusCode470 = "This path is parent. You can not execute on parent paths.";
    }
    /// <response code="200">Limit execute request is executed successfully.</response>
    /// <response code="452">Path can not be emtpy or null.</response>
    /// <response code="453">Invalid amount. Amount can not be negative. Negative is not applicable. Zero is used for rate limiting function.</response>
    /// <response code="454">"path" not enough remaining times left to process. One or more paths have zero remaining times left. So can not execute request. Could also mean there are not enough times to do reversal transactions.</response>
    /// <response code="455">"path" Minimum transaction limit:"amount". So can not execute request.</response>
    /// <response code="456">"path" Maximum transaction limit:"amount". So can not execute request.</response>
    /// <response code="457">"path" remaining amount limit not enough. One or more paths dont have enough remaining amount limit left. So can not execute request. Could also mean there are not enough limit to do reversal transactions.</response>
    /// <response code="458">Currency code is invalid. Please check the currency code.</response>
    /// <response code="459">Transaction time is not available. It could be due to exceptions such as holidays or out of available time frame. Will return closest available time. If the available time frame older than current time will return null.</response> 
    /// <response code="460">Path is not defined.</response>
    /// <response code="461">This path is parent. You can not execute on parent paths.</response>
    /// <response code="462">Exchange rates could not be obtained. Exchange rates API could be down.</response>
    /// <response code="463">Path at also-look is not defined. The path at also-look not found. If you do not want to use it send "none" or null.</response>
    /// <response code="464">An error has been occured in database.</response>
    /// <response code="500">Technical error on the system.</response>

}
