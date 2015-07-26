/**************************************************************************
Copyright(C) 2011-2015 Ian Farr

This file is part of TestDFS.exe

TestDFS.exe is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

TestDFS.exe is distributed in the hope that it will be useful,
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

namespace TestDFS
{
    class Program
    {
        static void Main(string[] args)
        {
            string curDFSPath = @"\\montreappmgmt02\test\public";
            string curTarget = @"\\montreappmgmt03\public";
            Farrworks.Net.Dfs.DFSActionStatus s;
            Farrworks.Net.Dfs.DfsShareDetails stat = Farrworks.Net.Dfs.GetDfsShareInfo(curDFSPath, curTarget);

            Console.WriteLine(stat.linkExists);

            //test to see if we can remove a dfs link...
            //s = Farrworks.Net.Dfs.RemoveDFSTarget(@"\\montreappmgmt02\test\public", "montreappmgmt03", "public_slave");

            //Console.WriteLine(s.ToString());

            //test to see if we can create a dfs link...
            //s = Farrworks.Net.Dfs.AddDFSTarget(@"\\montreappmgmt02\test\public", "montreappmgmt03", "public_slave");
            //Console.WriteLine(s.ToString());

            //do it again...duplicate...should fail..
            s = Farrworks.Net.Dfs.AddDFSTarget(@"\\montreappmgmt02\test\public$", "montreappmgmt03", "public");
            s = Farrworks.Net.Dfs.AddDFSTarget(@"\\montreappmgmt02\test\public$", "montreappmgmt03", "public_slave");
            Console.WriteLine(s.ToString());

            Console.WriteLine();
        }
    }
}
