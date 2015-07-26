/**************************************************************************
Copyright(C) 2011-2015 Ian Farr

This file is part of ShareMan.exe

ShareMan.exe is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

ShareMan.exe is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
**************************************************************************/

using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ShareManager.Structures;

namespace ShareManager
{
    class Program
    {
        internal static int curLineNumber;
        internal static string curLine;

        static void Main(string[] cArgs)
        {
            DateTime start = DateTime.Now;
            int errors = 0;
            int processedItems = 0;
            //int threads = 0;
            List<ProcessStatus> sharesStatus = new List<ProcessStatus>();
            List<string> shareErrors = new List<string>();

            //get the default command file in app.config
            string commandFile = System.Configuration.ConfigurationManager.AppSettings["commandFile"];

            //get our command line options..
            foreach (string arg in cArgs)
            {
                //split the arg
                string[] argSplit = arg.Split('=');

                //if we only have a single element in argSplit - there was no equal sign present, which means a simple option was detected
                if (argSplit.Length == 1)
                {
                    //simple command line options
                    switch (argSplit[0].ToLower())
                    {
                        default:
                            showUsage();
                            break;
                    }
                }
                else
                {
                    //complex command line detected
                    switch (argSplit[0].ToLower())
                    {
                        case "/c":
                            commandFile = argSplit[1];
                            break;
                    }
                }
            }

            //see if we can access the command file
            if (File.Exists(commandFile))
            {
                //read our command file
                curLineNumber = 0;
                System.IO.StreamReader cmdReader = new System.IO.StreamReader(commandFile);
                while ((curLine = cmdReader.ReadLine()) != null)
                {
                    curLineNumber++;
                    //make sure that the line isn't a comment, and is not empty
                    if (!curLine.StartsWith("#") && !string.IsNullOrEmpty(curLine) && !string.IsNullOrWhiteSpace(curLine))
                    {
                        string[] lineParts = curLine.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

                        //get the current command, it's always the first linePart
                        string curCommand = lineParts[0].Trim().ToLower();

                        switch (curCommand)
                        {
                            case "autoshare":
                                ProcessStatus autoStatus = AutoShare.Process(lineParts);
                                sharesStatus.Add(autoStatus);
                                break;
                            case "share":
                                ProcessStatus shareStatus = Share.Process(lineParts);
                                sharesStatus.Add(shareStatus);
                                break;
                            default:
                                Console.WriteLine("\r\n\r\nError processing command file line #{0} (valid command not found): \r\n  {1}\r\n\r\n", curLineNumber, curLine);
                                break;
                        }
                    }
                }
                cmdReader.Dispose();

                //get all our status'
                foreach (ProcessStatus status in sharesStatus)
                {
                    if (status.Errors > 0)
                    {
                        errors += status.Errors;
                        foreach (string err in status.ErrList)
                        {
                            shareErrors.Add(err);
                        }
                    }
                    processedItems += status.ProcessedItems;
                }

                DateTime end = DateTime.Now;
                TimeSpan runtime = end - start;

                StringBuilder msgBody = new StringBuilder();
                msgBody.AppendLine(String.Format("Runtime Information generated on: {0}", start));

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Total Processing Time (seconds): {0}", runtime.TotalSeconds.ToString());
                msgBody.AppendLine(String.Format("Total Processing Time (seconds): {0}", runtime.TotalSeconds.ToString()));
                
                Console.WriteLine("Total Number of Shares processed: {0}", processedItems.ToString());
                msgBody.AppendLine(String.Format("Total Number of Shares processed: {0}", processedItems.ToString()));

                Console.WriteLine("Shares/Sec: {0}", (processedItems / runtime.TotalSeconds).ToString());
                msgBody.AppendLine(String.Format("Shares/Sec: {0}", (processedItems / runtime.TotalSeconds).ToString()));

                Console.WriteLine("Errors: {0}", errors.ToString());
                msgBody.AppendLine(String.Format("Errors: {0}", errors.ToString()));

                if (errors > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Errors Encountered:");
                    msgBody.AppendLine("Errors Encountered:");
                    msgBody.AppendLine();
                    foreach (string err in shareErrors)
                    {
                        Console.WriteLine(err);
                        msgBody.AppendLine(err);
                    }
                }

                //if we requested e-mail alerts..
                if (System.Configuration.ConfigurationManager.AppSettings["sendEmail"].ToLower() == "true")
                {
                    sendmail(msgBody.ToString());
                }

                Console.WriteLine("Done");

                Environment.Exit(0);
            }
            else
            {
                //command file not found
                Console.WriteLine("Command file {0} not found! exiting", commandFile);

                Environment.Exit(1);
            }
        }


        public static void showUsage()
        {
            string usage = @"
  usage: shareman.exe [options]

  [options]:
    /c=path        path is the full path to the command file that should be used.
                   By default, shareman.shm is used as the command file, and it
                   is expected to exist in the same directory as shareman.exe.
                   You can use this option to change the name/location of the 
                   command file at runtime. Also, see Shareman.exe.config's
                   appSettings XML configuration (commandFile key). You can
                   change the location of the default command file for every
                   runtime there.
    
    /h             this help screen.
            ";

            Console.WriteLine(usage);
            Environment.Exit(1);
        }


        public static void sendmail(string message)
        {
            //....send e-mail alert...
            string fromAddr = System.Configuration.ConfigurationManager.AppSettings["emailFrom"];
            string toAddr = System.Configuration.ConfigurationManager.AppSettings["emailTo"];
            string subject = System.Configuration.ConfigurationManager.AppSettings["emailSubject"];
            System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage(fromAddr, toAddr, subject, message);

            System.Net.Mail.SmtpClient mailClient = new System.Net.Mail.SmtpClient(System.Configuration.ConfigurationManager.AppSettings["smtpServer"]);
            mailClient.Send(msg);
        }
    }
}