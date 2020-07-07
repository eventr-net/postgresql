namespace EventR.PostgreSql
{
    using EventR.Abstractions;
    using System.Data;
    using System.Data.Common;
    using System.IO;
    using System.Text.RegularExpressions;

    public static class Extensions
    {
        public static PgsqlBuilder Pgsql(this BuilderBase coreBuilder)
        {
            return new PgsqlBuilder(coreBuilder.Context);
        }

        /// <summary>
        /// Reads all bytes from the field stream in data reader.
        /// Used for reading BYTEA value.
        /// </summary>
        internal static byte[] GetAllBytes(this DbDataReader reader, int ordinal)
        {
            using (var stream = reader.GetStream(ordinal))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets user login from connestion string.
        /// </summary>
        internal static string GetUserId(this IDbConnection connection)
        {
            if (connection?.ConnectionString == null)
            {
                return null;
            }

            var cs = connection.ConnectionString;
            var match = OwnerRx.Match(cs);
            return match.Success ? match.Groups["UserId"].Value : null;
        }

        private static readonly Regex OwnerRx = new Regex(
            @"(user( )?id|username)\s*=\s*(?<UserId>[^;\s]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
