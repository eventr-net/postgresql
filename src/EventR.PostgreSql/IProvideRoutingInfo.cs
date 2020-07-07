namespace EventR.PostgreSql
{
    /// <summary>
    /// Provides routing information based on stream ID (of any given root aggregate).
    /// </summary>
    public interface IProvideRoutingInfo
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="streamId">Event stream ID</param>
        RoutingInfo CreateRoutingInfo(string streamId);
    }
}
