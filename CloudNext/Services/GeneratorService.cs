//using Microsoft.Extensions.Configuration;
//using Serilog;
//using Webapp.Database;


//namespace Webapp.Services
//{
//    public class PasswordStrings
//    {
//        public string? UpperCaseAlphabets { get; set; }
//        public string? LowerCaseAlphabets { get; set; }
//        public string? Digits { get; set; }
//        public string? SpecialCharacters { get; set; }
//    }

//    public class Generator
//    {
//        private readonly string UpperCaseChars;
//        private readonly string LowerCaseChars;
//        private readonly string Digits;
//        private readonly string SpecialChars;
//        private static readonly Random rand = new();
        
//        private readonly DatabaseManager database = new();

//        // Constructor
//        public Generator()
//        {
//            ConfigReader config = new ConfigReader();

//            // Retrieve the values using ConfigReader
//            UpperCaseChars = config.GetConfig("PasswordStrings", "UpperCaseAlphabets")!;
//            LowerCaseChars = config.GetConfig("PasswordStrings", "LowerCaseAlphabets")!;
//            Digits = config.GetConfig("PasswordStrings", "Digits")!;
//            SpecialChars = config.GetConfig("PasswordStrings", "SpecialCharacters")!;
//        }

//        public string GenerateSuggestedPassword(int length = 12)
//        {
//            if (length < 8) throw new ArgumentException("Password length should be at least 8 characters for security reasons.");

//            string allChars = UpperCaseChars + LowerCaseChars + Digits + SpecialChars;

//            // List to hold password characters
//            var passwordChars = new char[length];

//            // Ensure the password contains at least one character of each type
//            passwordChars[0] = UpperCaseChars[rand.Next(UpperCaseChars.Length)];
//            passwordChars[1] = LowerCaseChars[rand.Next(LowerCaseChars.Length)];
//            passwordChars[2] = Digits[rand.Next(Digits.Length)];
//            passwordChars[3] = SpecialChars[rand.Next(SpecialChars.Length)];

//            // Fill the rest of the password with random characters from the combined pool
//            for (int i = 4; i < length; i++)
//            {
//                passwordChars[i] = allChars[rand.Next(allChars.Length)];
//            }

//            // Shuffle the password to make the placement of characters random
//            for (int i = 0; i < length; i++)
//            {
//                int j = rand.Next(length);
//                char temp = passwordChars[i];
//                passwordChars[i] = passwordChars[j];
//                passwordChars[j] = temp;
//            }

//            return new string(passwordChars);
//        }

//        public string GenerateSuggestedUsername(string originalUsername)
//        {
//            string suggestedUsername;
//            int counter = 1;

//            do
//            {
//                suggestedUsername = originalUsername + counter.ToString();
//                counter++;
//            } while (database.UserNameTaken(suggestedUsername));

//            return suggestedUsername;
//        }

//        internal string GenerateOTP()
//        {   
//            // Log.Information("Generated OTP");
//            Random random = new Random();
//            return random.Next(100000, 999999).ToString();
//        }
//    }
//}
