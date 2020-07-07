namespace EventR.PostgreSql
{
    /// <summary>
    /// Provides null implementation of routing information; that is saying 'do not rout'.
    /// </summary>
    public class VoidRouting : IProvideRoutingInfo
    {
        public static readonly RoutingInfo NoRouting = new RoutingInfo(null, 0);

        public RoutingInfo CreateRoutingInfo(string streamId)
        {
            return NoRouting;
        }
    }
}
