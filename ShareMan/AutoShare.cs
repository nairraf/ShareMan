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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShareManager.Structures;
using System.IO;

namespace ShareManager
{
    public static class AutoShare
    {

        #region public methods
        /// <summary>
        /// processes an autoshare line from a compatible share manager command file
        /// </summary>
        /// <param name="cmdLine"></param>
        /// <returns></returns>
        public static ProcessStatus Process(string[] cmdLine)
        {
            ProcessStatus retStatus = new ProcessStatus();

            //make sure the first index is set to "autoshare"
            if (cmdLine[0].ToLower().Trim() == "autoshare")
            {
                //find all our needed parameters
                string rootDir = null;
                string fileServerName = null;
                bool? hidden = null;
                string subDirMatch = "*";       //optional
                bool shareRoot = false;         //optional
                string rootShareName = null;    //optional
                bool hideRootShare = false;     //optional
                bool registerDFS = false;       //optional
                bool registerRootDFS = false;   //optional
                bool disableCSC = true;         //optional
                Farrworks.Net.Share.CSCType cscType = Farrworks.Net.Share.CSCType.CSC_CACHE_MANUAL_REINT;      //optional
                string dfsRoot = null;

                int numParams = cmdLine.Count();

                //we start looping at the secound index (index 1)
                //the first index is the command - which we confirmed is autoshare
                for (int i = 1; i < numParams; i++)
                {
                    //split the paramter line by ->
                    string[] paramLine = cmdLine[i].Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    if (paramLine.Count() == 2)
                    {
                        string key = paramLine[0].ToLower().Trim();
                        string val = paramLine[1].ToLower().Trim();

                        switch (key)
                        {
                            case "rootdir":
                                rootDir = val;
                                break;
                            case "fileservername":
                                fileServerName = val;
                                break;
                            case "hidden":
                                hidden = val == "true" ? true : false;
                                break;
                            case "subdirmatch":
                                subDirMatch = val;
                                break;
                            case "shareroot":
                                shareRoot = val == "true" ? true : false;
                                break;
                            case "rootsharename":
                                rootShareName = val;
                                break;
                            case "hiderootshare":
                                hideRootShare = val == "true" ? true : false;
                                break;
                            case "registerdfs":
                                registerDFS = val == "true" ? true : false;
                                break;
                            case "registerrootdfs":
                                registerRootDFS = val == "true" ? true : false;
                                break;
                            case "dfsroot":
                                dfsRoot = val;
                                break;
                            case "disablecsc":
                                disableCSC = val == "true" ? true : false;
                                break;
                            case "csctype":
                                switch (val)
                                {
                                    case "manual":
                                        cscType = Farrworks.Net.Share.CSCType.CSC_CACHE_MANUAL_REINT;
                                        break;
                                    case "optimized":
                                        cscType = Farrworks.Net.Share.CSCType.CSC_CACHE_VDO;
                                        break;
                                    case "auto":
                                        cscType = Farrworks.Net.Share.CSCType.CSC_CACHE_AUTO_REINT;
                                        break;
                                    default:
                                        cscType = Farrworks.Net.Share.CSCType.CSC_CACHE_MANUAL_REINT;
                                        break;
                                }
                                break;
                        }
                    }
                }

                //make sure that we have our needed paramters to continue
                if (!(rootDir == null) && !(fileServerName == null) && !(hidden == null))
                {
                    //make sure the root path exists
                    if (Directory.Exists(rootDir))
                    {
                        Console.WriteLine("Processing (autoshare): {0}", rootDir);
                        //autoshare our rootdir's subfolders
                        retStatus = AutoShare.ShareSubFolders(rootDir, (bool)hidden, fileServerName, subDirMatch, registerDFS, dfsRoot, disableCSC, cscType);

                        //if we also want the root shared...
                        if (shareRoot)
                        {
                            if (rootShareName == null)
                            {
                                //if rootshare is blank, use rootdir's name for the share name.
                                System.IO.DirectoryInfo root = new DirectoryInfo(rootDir);
                                rootShareName = root.Name;
                            }

                            //create the root share...
                            Share.CreateShare(fileServerName, rootDir, rootShareName, null, hideRootShare, ref retStatus, registerRootDFS, dfsRoot, disableCSC, cscType);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Root Directory doesn't exist for {0}, skipping..", rootDir);
                    }
                }
                else
                {
                    //we do not have all the needed parameters - error out
                    retStatus.ErrList.Add(String.Format("error: not all needed parameters were defined for autoshare! command file line #({0})\r\n {1}", Program.curLineNumber.ToString(), Program.curLine ));
                    retStatus.Errors++;
                }
            }
            else
            {
                //autoshare command not found - error out
                retStatus.ErrList.Add(String.Format("error in autoshare, command 'autoshare' expected but '{0}' was found instead", cmdLine[0]));
                retStatus.Errors++;
            }
            
            return retStatus;
        }

        #endregion public methods


        #region private methods

        /// <summary>
        ///     Creates shares from a given path. all folders under the given path are shared using their folder names as the share name
        /// </summary>
        /// <param name="rootFolder">The root folder under which all subfolders will be auto-shared</param>
        /// <param name="isHidden">Should the shares be created as hidden shares?</param>
        /// <param name="serverName">the server to check for/create the shares on</param>
        /// <returns>ProcessStatus</returns>
        private static ProcessStatus ShareSubFolders(string rFolder, bool isHidden, string serverName, string subDirMatch, bool registerDfs, string dfsRoot, bool disableCSC, Farrworks.Net.Share.CSCType cscType)
        {

            System.IO.DirectoryInfo rootFolder = new DirectoryInfo(rFolder);

            //initialize our struct to hold the processed items and errors we encounter
            ProcessStatus pStatus = new ProcessStatus(new List<string>(), 0, 0);

            foreach (System.IO.DirectoryInfo sharedir in rootFolder.GetDirectories(subDirMatch))
            {
                //check to see if the folder is already shared:
                string curShare = sharedir.Name;
                if (isHidden)
                    curShare = sharedir.Name + "$";

                Share.CreateShare(serverName, sharedir.FullName, sharedir.Name, "AutoShared Folder for: " + sharedir.Name, isHidden, ref pStatus, registerDfs, dfsRoot, disableCSC, cscType);
            }
            return pStatus;
        }

        #endregion private methods
    }

}
