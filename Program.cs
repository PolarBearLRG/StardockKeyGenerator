using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SmorcIRL.TempMail;
using System.Windows;
using System.IO;
using SmorcIRL.TempMail.Models;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
namespace StardockKeyGenerator
{
    class Program
    {
        [STAThread]
        static void Main()

        {
            Console.WriteLine("Stardock Trial Key Generator");
            Console.WriteLine("This tool resets the trial period for Stardock software.");
            Console.WriteLine("\n!!  BEFORE USING, please launch your Stardock software and create a settings backup file.  !!\n\n");
            Console.WriteLine("Press Enter to continue...");
            if (Console.ReadKey(true).Key != ConsoleKey.Enter) return;
            Console.WriteLine("\n");
            if (!RunRegReset())
            {
                Console.WriteLine("Press any key to exit or press Enter to continue with account generation.");
                if (Console.ReadKey(true).Key != ConsoleKey.Enter)
                {
                    return;
                }
            }
            Console.WriteLine("Preparing to generate mail account");
            Console.WriteLine();
            var accountTask = MailAPI.InitializeAccount();
            while (accountTask.Status != TaskStatus.RanToCompletion)
            {
                if (accountTask.Status == TaskStatus.Faulted
                || accountTask.Status == TaskStatus.Canceled)
                {
                    Console.WriteLine("Error generating account. \n" + accountTask.Exception?.Message);
                    return;
                }
                Thread.Sleep(100); // Wait for 1000 milliseconds before checking again
            }
            Clipboard.SetText(MailAPI.mailClient.Email);
            Console.WriteLine($"Email copied to clipboard: {MailAPI.mailClient.Email}");
            Console.WriteLine("\nPlease paste the email address into your Stardock software now.");
            Console.WriteLine("Press Enter to Check Messages...");
            Console.ReadLine();
            var messageTask = MailAPI.FindTrialMessage();
            while (messageTask.Status != TaskStatus.RanToCompletion)
            {
                if (messageTask.Status == TaskStatus.Faulted
                || messageTask.Status == TaskStatus.Canceled)
                {
                    Console.WriteLine("Error reading messages. \n" + messageTask.Exception?.Message);
                    return;
                }
                Thread.Sleep(100); // Wait for 1000 milliseconds before checking again
            }
            // launch link in browser
            var link = MailAPI.ExtractLink(messageTask.Result);
            while (link.Status != TaskStatus.RanToCompletion)
            {
                if (link.Status == TaskStatus.Faulted
                || link.Status == TaskStatus.Canceled)
                {
                    Console.WriteLine("Error extracting link. \n" + link.Exception?.Message);
                    return;
                }
                Thread.Sleep(100); // Wait for 1000 milliseconds before checking again
            }
            MailAPI.mailClient.DeleteAccount();
            if (link.Result != null)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = link.Result,
                    UseShellExecute = true
                });
            }
        }
        public static bool RunRegReset()
        {
            string regResetCMD = "StardockKeyGenerator.bat";
            string batPath = AppContext.BaseDirectory + regResetCMD;
            if (!File.Exists(batPath))
            {
                if (GenerateBatch(regResetCMD) == null) return false;
            }
            Process pReg = new();
            pReg.StartInfo.FileName = "cmd.exe";
            pReg.StartInfo.Arguments = $"/C \"{batPath}\"";
            pReg.Start();
            pReg.WaitForExit();
            if (pReg.ExitCode != 0)
            {
                Console.WriteLine("Execution failed. Please check the instructions and try again.");
                Console.WriteLine($"Process exiting with code {pReg.ExitCode}");
                return false;
            }
            pReg.Close();
            Console.WriteLine("\n\nDone.");
            return true;
        }
        private static string? GenerateBatch(string fileName)
        {
            string tempBatPath;
            Console.WriteLine("Batch file not found. Generating...");
            try
            {
                // 1. Get the embedded resource stream
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream? stream = assembly.GetManifestResourceStream("StardockKeyGenerator.ClearRegistry.bat"))
                {
                    if (stream == null)
                    {
                        Console.WriteLine("Embedded file not found.");
                        return null;
                    }
                    tempBatPath = AppContext.BaseDirectory + fileName;
                    using (FileStream fileStream = File.Create(tempBatPath))
                    {
                        stream.CopyTo(fileStream);
                        Console.WriteLine($"Successfully Generated {fileName}");
                        Console.WriteLine(tempBatPath);
                        return tempBatPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate file. Exception: {ex.Message}");
                return null;
            }
        }
    }
}
public class MailAPI
{
    private static readonly MailClient client = new();
    public static MailClient mailClient => client;
    public static async Task InitializeAccount()
    {
        // This method initializes the TempMail client and generates an account.
        Random random = new Random();
        string password = random.Next(10000000, 99999999).ToString();
        Console.WriteLine("Generating account...");
        try { await client.GenerateAccount(password); }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating account: {ex.Message}");
            return;
        }
        Console.WriteLine("Account generated successfully.");
        Console.WriteLine($"Password: {password}");

        return;
    }
    public static async Task<string?> FindTrialMessage()
    {
        // This method reads messages from the TempMail account.
        var srcAddress = "stardock.net";
        List<string> targets = [];
        MessageInfo[] messages;
        int choice = 0;
        try
        {
            messages = await client.GetAllMessages();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting inbox: {ex.Message}");
            return null;
        }
        ;// Check if there are any messages
        do
        {
            Console.WriteLine("Reading messages...");
            if (messages.Length == 0)
            {
                Console.WriteLine($"No valid messages found");
                Console.WriteLine($"Press Enter to continue searching for messages or press any other key to exit.");
                if (Console.ReadKey(true).Key != ConsoleKey.Enter) return null;
                Thread.Sleep(1000); // Wait for 1000 milliseconds before checking again
                messages = await mailClient.GetAllMessages(); // Check if there are any messages
            }
            foreach (var message in messages)
            {
                if (message.From.Address.Contains(srcAddress))
                {
                    targets.Add(message.Id);
                }
            }
        } while (targets.Count == 0);
        if (targets.Count == 1)
        {
            Console.WriteLine($"Found 1 message from {srcAddress}. Automatically selecting it.");
            return targets[0];
        }
        for (int i = 0; i < targets.Count; i++)
        {
            Console.WriteLine($"Checking message: {messages[i].Subject}");
            if (messages[i].Subject.Contains("Activation"))
            {
                Console.WriteLine($"Found Trial message: {targets[0]}");
                return targets[i];
            }
        }
        Console.WriteLine("Could not automatically find a trial message.");
        Console.WriteLine("Please select a message from the list below: Or press Enter to exit");
        for (int i = 1; i < messages.Length; i++)
        {
            Console.WriteLine($"{i}: {messages[i].Subject} - {messages[i].From.Address}");
        }
        while (choice == 0)
        {
            string? key = Console.ReadLine();
            if (key == null) return null;
            if (int.TryParse(key, out choice) ||
                    choice < 1 || choice >= targets.Count)
            {
                Console.Write('\r');
                await Task.Delay(200);
                Console.Write("Invalid choice. try again");
            }
        }
        return targets[choice - 1];
    }
    public static async Task<string?> ExtractLink(string? targetID)
    {
        MessageDetailInfo msgDetails;
        if (targetID == null)
        {
            Console.WriteLine("Message not found or could not be retrieved.");
            return null;
        }
        try { msgDetails = await client.GetMessage(targetID); }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting message details {targetID}: {ex.Message}");
            return null;
        }
        string messageBody = msgDetails.BodyText;
        int linkStartIndex = messageBody.IndexOf("[https://activate.api.stardock.net");
        int linkEndIndex = messageBody.IndexOf("]", linkStartIndex);
        if (linkStartIndex != -1)
        {
            Console.WriteLine("Activation link found.");
            return messageBody.Substring(linkStartIndex + 1, linkEndIndex - linkStartIndex - 1);
        }
        List<string> bodyData = new();
        int choice = 0;
        Console.WriteLine("Could not find activation link in the message body.");
        Console.WriteLine("Please select link manually or copy the license key.\n");
        while (linkStartIndex != -1 && linkEndIndex != -1)
        {
            linkStartIndex = messageBody.IndexOf('[');
            linkEndIndex = messageBody.IndexOf(']', linkStartIndex);
            bodyData.Add(messageBody.Substring(linkStartIndex + 1, linkEndIndex - linkStartIndex - 1));
            messageBody = messageBody[(linkEndIndex + 1)..];
        }
        await Task.Delay(5000); // Wait for 5 seconds before posting
        Console.WriteLine(messageBody);
        foreach (var data in bodyData)
        {
            Console.WriteLine(data);
        }
        for (int i = 1; i < bodyData.Count; i++)
        {
            Console.WriteLine(bodyData[i - 1]);
        }
        if (int.TryParse(Console.ReadLine(), out choice) ||
                choice < 1 || choice >= bodyData.Count)
        {
            Console.WriteLine("Invalid choice. Exiting.");
            return null;
        }
        return bodyData[choice];
    }
}
