namespace ASPCommerce.Helpers
{
    public class SqlAttackHelper
    {

        public static bool IsSafeSQL(string text)
        {
            // Check if the input string is null or empty
            if (string.IsNullOrEmpty(text))
            {
                // If the input string is null or empty, consider it safe
                return true;
            }

            // Define a list of potentially dangerous characters and patterns
            string[] dangerousPatterns = { "--", ";", "'", "\"", "/*", "*/", "@@", "@", "char", "nchar", "varchar", "nvarchar", "alter", "begin", "cast", "create", "cursor", "declare", "delete", "drop", "end", "exec", "execute", "fetch", "insert", "kill", "open", "select", "sys", "sysobjects", "syscolumns", "table", "update" };

            // Convert the input string to lowercase for case-insensitive matching
            string lowercaseText = text.ToLower();

            // Loop through each potentially dangerous pattern
            foreach (string pattern in dangerousPatterns)
            {
                // Check if the current pattern exists in the input string
                if (lowercaseText.Contains(pattern))
                {
                    // If the pattern is found in the input string, it may be an SQL injection attempt
                    Console.WriteLine("I found an sql attack. which is: " + pattern);
                    return false;
                }
            }

            // If none of the potentially dangerous patterns are found in the input string,
            // consider the input string safe
            return true;
        }
    }
}
