using CalendarSharing.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

namespace CalendarSharing.Utils
{
    public static class Office365Remoting
    {
        private static readonly string username = ConfigurationManager.AppSettings["o365Username"];
        private static readonly string password = ConfigurationManager.AppSettings["o365Password"];

        public static WSManConnectionInfo getOffice365Connection()
        {            
            SecureString securePassword = new SecureString();
            foreach (char c in password)
                securePassword.AppendChar(c);
            securePassword.MakeReadOnly();

            PSCredential o365credentials = new PSCredential(username, securePassword);

            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(
                new Uri("https://ps.outlook.com/Powershell-LiveID?PSVersion=3.0"), 
                "http://schemas.microsoft.com/powershell/Microsoft.Exchange", 
                o365credentials);
            connectionInfo.AuthenticationMechanism = AuthenticationMechanism.Basic;
            connectionInfo.SkipCACheck = true;
            connectionInfo.SkipCNCheck = true;
            connectionInfo.MaximumConnectionRedirectionCount = 4;

            return connectionInfo;
        }

        public static PSCommand ParseCommand(string commandLine)
        {
            PSCommand command = new PSCommand();
            int i = commandLine.IndexOf(" ");
            if (i < 0)
            {
                command.AddCommand(commandLine);
                return command;
            }

            // Add the command
            command.AddCommand(commandLine.Substring(0, i));

            // Now parse and add parameters
            try
            {
                i = commandLine.IndexOf("-", i);
                while ((i > 0) && (i < commandLine.Length))
                {
                    int j = commandLine.IndexOf("-", i + 1);
                    if (j < 0) j = commandLine.Length;
                    int p = commandLine.IndexOf(" ", i + 1);
                    string sParamName = commandLine.Substring(i + 1, p - i - 1);
                    string sParamValue = commandLine.Substring(p + 1, j - p - 1);
                    if (sParamValue.StartsWith("\"") && sParamValue.EndsWith("\""))
                        sParamValue = sParamValue.Substring(1, sParamValue.Length - 2);
                    command.AddParameter(sParamName, sParamValue);
                    i = j;
                }
            }
            catch (Exception)
            {
                
            }
            return command;
        }

        public static List<UserDetails> getCalendarUrls(List<UserDetails> users, WSManConnectionInfo connection)
        {
            PowerShell o365ps = null;            

            try
            {                
                o365ps = PowerShell.Create();                
                o365ps.Runspace = RunspaceFactory.CreateRunspace(connection);
                o365ps.Runspace.Open();

                List<string> commands = new List<string>();

                foreach (var user in users)
                {
                    commands.Add("Get-MailboxCalendarFolder -Identity " + user.mailNickname + ":\\Calendar");

                    Collection<PSObject> results = null;
                    o365ps.Commands = ParseCommand(commands[0]);
                    results = o365ps.Invoke<PSObject>();

                    string calendarUrl = string.Empty;
                    string identity = string.Empty;
                    foreach (PSObject obj in results)
                    {
                        var rawIdentity = obj.Properties["Identity"].Value.ToString().Split(':');
                        identity = rawIdentity[0];
                        if (identity.ToLowerInvariant() == user.mailNickname.ToLowerInvariant())
                        {
                            calendarUrl = obj.Properties["PublishedCalendarUrl"].Value.ToString();
                            user.PublishedCalendarUrl = calendarUrl;
                        }
                    }
                    commands.Clear();
                }

            }
            catch (Exception)
            {
                //something went wrong
            }
            finally
            {                
                o365ps.Runspace.Close();
                o365ps.Runspace.Dispose();
            }

            return users;
        }

        public static List<UserDetails> enablePublishedCalendars(List<UserDetails> users, WSManConnectionInfo connection)
        {
            PowerShell o365ps = null;

            try
            {
                o365ps = PowerShell.Create();
                o365ps.Runspace = RunspaceFactory.CreateRunspace(connection);
                o365ps.Runspace.Open();

                List<string> commands = new List<string>();

                foreach (var user in users)
                {
                    commands.Add("Set-MailboxCalendarFolder -Identity " + user.mailNickname + ":\\Calendar" + "-PublishEnabled:$true");

                    Collection<PSObject> results = null;
                    o365ps.Commands = ParseCommand(commands[0]);

                    //For a successfully enabled calendar there is no return value
                    results = o365ps.Invoke<PSObject>();

                    commands.Clear();
                }

            }
            catch (Exception)
            {
                //something went wrong
            }
            finally
            {
                o365ps.Runspace.Close();
                o365ps.Runspace.Dispose();
            }

            return users;
        }

        public static List<UserDetails> disablePublishedCalendars(List<UserDetails> users, WSManConnectionInfo connection)
        {
            PowerShell o365ps = null;

            try
            {
                o365ps = PowerShell.Create();
                o365ps.Runspace = RunspaceFactory.CreateRunspace(connection);
                o365ps.Runspace.Open();

                List<string> commands = new List<string>();

                foreach (var user in users)
                {
                    commands.Add("Set-MailboxCalendarFolder -Identity " + user.mailNickname + ":\\Calendar" + "-PublishEnabled:$false");

                    Collection<PSObject> results = null;
                    o365ps.Commands = ParseCommand(commands[0]);

                    //For a successfully disabled calendar there is no return value
                    results = o365ps.Invoke<PSObject>();

                    commands.Clear();
                }

            }
            catch (Exception)
            {
                //something went wrong
            }
            finally
            {
                o365ps.Runspace.Close();
                o365ps.Runspace.Dispose();
            }

            return users;
        }
    }
}