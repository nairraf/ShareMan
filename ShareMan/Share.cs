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
using System.Runtime.InteropServices;
using System.Security.Principal;
using ShareManager.Structures;
using System.IO;

namespace ShareManager
{
    public static class Share
    {
        #region public methods

        /// <summary>
        /// process a share line from a compatible sharemanager command file
        /// </summary>
        /// <param name="parsedCmdLine"></param>
        /// <returns></returns>
        public static ProcessStatus Process(string[] parsedCmdLine)
        {

            ProcessStatus retStatus = new ProcessStatus(new List<string>(), 0, 0);

            //make sure the first index is set to "autoshare"
            if (parsedCmdLine[0].ToLower().Trim() == "share")
            {
                //find all our needed parameters
                string path = null;
                string shareName = null;
                string fileServerName = null;
                bool? hidden = null;
                string shareDescripton = null;
                bool registerDFS = false;       //optional
                bool disableCSC = true;         //optional
                Farrworks.Net.Share.CSCType cscType = Farrworks.Net.Share.CSCType.CSC_CACHE_MANUAL_REINT;      //optional
                string dfsRoot = null;

                int numParams = parsedCmdLine.Count();

                //we start looping at the secound index (index 1)
                //the first index is the command - which we confirmed is share
                for (int i = 1; i < numParams; i++)
                {
                    //split the paramter line by ->
                    string[] paramLine = parsedCmdLine[i].Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);

                    //make sure that we have two indexes, index 0 is the key, index 1 is the value
                    if (paramLine.Count() == 2)
                    {
                        string key = paramLine[0].ToLower().Trim();
                        string val = paramLine[1].ToLower().Trim();

                        switch (key)
                        {
                            case "path":
                                path = val;
                                break;
                            case "sharename":
                                shareName = val;
                                break;
                            case "fileservername":
                                fileServerName = val;
                                break;
                            case "hidden":
                                hidden = val == "true" ? true : false;
                                break;
                            case "sharedescription":
                                shareDescripton = val;
                                break;
                            case "registerdfs":
                                registerDFS = val == "true" ? true : false;
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

                //make sure that we have all the needed parameters
                //make sure that we have our needed paramters to continue
                if (!(path == null) && !(shareName == null) && !(fileServerName == null) && !(hidden == null))
                {
                    //we have everything we need....make sure the directory exists
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine("Sharing: {0}", path);

                        Share.CreateShare(fileServerName, path, shareName, shareDescripton, (bool)hidden, ref retStatus, registerDFS, dfsRoot, disableCSC, cscType);
                    }
                    else
                    {
                        Console.WriteLine("Path doesn't exist for {0}, skipping..", path);
                    }
                }
                else
                {
                    //we do not have all the needed parameters - error out
                    retStatus.ErrList.Add("error: not all needed parameters were defined for share! command file line #(" + Program.curLineNumber.ToString() + "): \r\n   " + Program.curLine);
                    retStatus.Errors++;
                }
            }
            else
            {
                //error - expected "share" as the command...but it isn't
                retStatus.ErrList.Add("error in autoshare, command 'autoshare' expected but '" + parsedCmdLine[0] + "' was found instead");
                retStatus.Errors++;
            }

            return retStatus;
        }

        /// <summary>
        /// Checks if a share currently exists, and if not, tries to create it. Updates the referenced ProcessStatus structure with processed items count and error info if there are any failures
        /// </summary>
        /// <param name="server"></param>
        /// <param name="path"></param>
        /// <param name="shareName"></param>
        /// <param name="shareDesc"></param>
        /// <param name="isHiddenShare"></param>
        /// <param name="procStatus"></param>
        public static void CreateShare(string server, string path, string shareName, string shareDesc, bool isHiddenShare, ref ProcessStatus procStatus, bool registerDfs, string dfsRoot, bool disableCSC, Farrworks.Net.Share.CSCType cscType)
        {
            //if the shareName is set to be a hidden share...this is probably not what we want..so just set the dfsLinkName to the non-hidden shareName (no ending dolloar sign)
            //as there is no way to hide a DFS link, having a dfs link end with a dollar sign just looks stupid...
            string dfsLinkName = shareName;

            //add dollar sign for hidden shares
            if (isHiddenShare)
            {
                shareName += "$";
            }

            //make sure the servername is in UNC format (if specified)
            if (server[0] != '\\')
            {
                server = @"\\" + server;
            }

            //check if the share is shared or not
            Farrworks.Net.Share.State sfs = Farrworks.Net.Share.IsShared(server, shareName, path);

            //create our everyone full control ACL and add it to our list of ACL's
            Farrworks.Net.Share.ACL everyoneFC = new Farrworks.Net.Share.ACL("everyone", Farrworks.Net.Share.Action.Allow, Farrworks.Net.Share.Permission.FullControl);
            List<Farrworks.Net.Share.ACL> acl = new List<Farrworks.Net.Share.ACL>();
            acl.Add(everyoneFC);

            //only try and share it if we don't detect an existing share for this folder...
            if (sfs == Farrworks.Net.Share.State.IsNotShared)
            {
                //not shared - so share it
                Farrworks.Net.Share.ShareStatus status = Farrworks.Net.Share.CreateWin32Share(server, path, shareName, shareDesc, Farrworks.Net.Share.ShareType.Normal, acl);

                //check the NET_API_STATUS...make sure things are OK...
                switch (status)
                {
                    case Farrworks.Net.Share.ShareStatus.Success:
                        //make sure that the proper CSC setting is enabled for this share
                        SetShareCSC(server, shareName, disableCSC, cscType, ref procStatus);
                        break;
                    case Farrworks.Net.Share.ShareStatus.Err_DuplicateShare:
                        //force registerDFS to false...this is a duplicate..so we don't mess with things in the DFS tree
                        registerDfs = false;
                        //set error info
                        procStatus.Errors++;
                        procStatus.ErrList.Add(String.Format("Duplicate ShareName For: {0}, Path: {1}", shareName, path));
                        break;
                    case Farrworks.Net.Share.ShareStatus.Err_PathNotFound:
                        //force registerDFS to false...we couldn't find the path to share...so no DFS
                        registerDfs = false;
                        //set error info
                        procStatus.Errors++;
                        procStatus.ErrList.Add(String.Format("Cannot find device or Path does not exist for: {0}", path));
                        break;
                    default:
                        //force registerDFS to false...the share couldn't be created for some reason...no DFS
                        registerDfs = false;
                        //set error info
                        procStatus.Errors++;
                        procStatus.ErrList.Add(String.Format("Unknown error creating share for: {0}", path));
                        break;
                }
            }
            else if (sfs == Farrworks.Net.Share.State.IsDuplicateShare)
            {
                //force registerDFS to false...this is a duplicate..so we don't mess with things in the DFS tree
                registerDfs = false;

                //error - folder that would generate a duplicate share name detected!!
                procStatus.Errors++;
                procStatus.ErrList.Add(String.Format("Duplicate Share Detected! share: {0}, path: {1}", shareName, path));
            }
            else if (sfs == Farrworks.Net.Share.State.IsShared)
            {
                //check if client side caching matches the desired CSC state
                Farrworks.Net.Share.CSCType curCsc = Farrworks.Net.Share.GetShareCSCState(server, shareName);

                if (disableCSC)
                {
                    if ( curCsc != Farrworks.Net.Share.CSCType.CSC_CACHE_NONE)
                    {
                        SetShareCSC(server, shareName, disableCSC, Farrworks.Net.Share.CSCType.CSC_CACHE_NONE, ref procStatus);
                    }
                }
                else
                {
                    if (curCsc != cscType)
                    {
                        SetShareCSC(server, shareName, disableCSC, cscType, ref procStatus);
                    }
                }
            }

            //register it in DFS if everything looks good...
            if (registerDfs && dfsRoot != null)
            {
                string dfsLinkPath = dfsRoot + @"\" + dfsLinkName;
                string targetPath = server + @"\" + shareName;

                //check to see if there is a dfs link/target by getting the details for this dfs link path and target.
                Farrworks.Net.Dfs.DfsShareDetails dfsDetails = Farrworks.Net.Dfs.GetDfsShareInfo(dfsLinkPath, targetPath);

                //create the dfs target in case we couldn't find a matching one (dfs link/target doesn't exist)..
                if (!dfsDetails.targetMatch)
                {
                    //our flag to control if it's OK to create a new link/target
                    bool dfsClean = false;

                    //if we have a DFS link already, but no matching target, this means that if we create a new target, we will have multiple
                    //so the best thing to do is to delete the link, and recreate it with the "good" target
                    if (dfsDetails.linkExists)
                    {
                        // we do not have a target match...so make sure that things are clean..
                        Farrworks.Net.Dfs.DFSActionStatus rmStat = Farrworks.Net.Dfs.RemoveDFSTarget(dfsLinkPath);
                        if (rmStat == Farrworks.Net.Dfs.DFSActionStatus.Success)
                        {
                            //we cleaned up properly...set the clean flag to true.
                            dfsClean = true;
                        }
                        else
                        {
                            //we couldn't clean up for some reason...set error details
                            procStatus.Errors++;
                            procStatus.ErrList.Add(String.Format("Problem Removing DFS Path {0}, Target: {1}", dfsLinkPath, targetPath));
                        }
                    }
                    else
                    {
                        //the link doesn't currently exist, so we are good to create it.
                        dfsClean = true;
                    }

                    if (dfsClean)
                    {
                        Farrworks.Net.Dfs.DFSActionStatus status = Farrworks.Net.Dfs.AddDFSTarget(dfsLinkPath, server, shareName);
                        if (!(status == Farrworks.Net.Dfs.DFSActionStatus.Success))
                        {
                            procStatus.Errors++;
                            procStatus.ErrList.Add(String.Format("Problem Adding DFS Path {0}, Target: {1}", dfsLinkPath, targetPath));
                        }
                    }
                    
                }
                else
                {
                    //we have a target match, make sure that it is the only target for the link
                    if (dfsDetails.numberOfTargets > 1)
                    {
                        //apparently it's not, so loop through all the targets, and remove the ones that don't match the current valid target
                        foreach (Farrworks.Net.Dfs.DfsTarget target in dfsDetails.targets)
                        {
                            if (target.targetPath != targetPath)
                            {
                                string targetServer = "";
                                string targetShare = "";

                                //remove it - first we need to get seperate server and share names
                                //split by backslashes, and remove empty indexes...this will account for the beggining double slash that UNCs have.
                                string[] targetPathParts = target.targetPath.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);

                                targetServer = targetPathParts[0];
                                targetShare = targetPathParts[1];


                                Farrworks.Net.Dfs.DFSActionStatus rmStat = Farrworks.Net.Dfs.RemoveDFSTarget(dfsLinkPath, targetServer, targetShare);
                                if (!(rmStat == Farrworks.Net.Dfs.DFSActionStatus.Success))
                                {
                                    procStatus.Errors++;
                                    procStatus.ErrList.Add(String.Format("Problem Removing DFS Target: {0}", target.targetPath));
                                }
                            }
                        }
                    }
                }
            }

            procStatus.ProcessedItems++;
        }
        #endregion public methods

        #region private methods
        private static void SetShareCSC(string server, string shareName, bool disableCSC, Farrworks.Net.Share.CSCType cscType, ref ProcessStatus procStatus)
        {
            bool success;
            if (disableCSC)
            {
                success = Farrworks.Net.Share.SetShareCSC(server, shareName, Farrworks.Net.Share.CSCType.CSC_CACHE_NONE);
            }
            else
            {
                success = Farrworks.Net.Share.SetShareCSC(server, shareName, cscType);
            }

            if (!success)
            {
                procStatus.Errors++;
                procStatus.ErrList.Add(String.Format(@"Problem Setting CSC setting for: \\{0}\{1}", server, shareName));
            }
        }
        #endregion private methods
    }
}
