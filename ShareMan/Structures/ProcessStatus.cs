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

namespace ShareManager.Structures
{
    /// <summary>
    /// Holds runtime processing information (list of errors, number of errors, number of processed items)
    /// </summary>
    public struct ProcessStatus
    {
        public List<string> ErrList;
        public int Errors;
        public int ProcessedItems;

        public ProcessStatus(List<string> errlist, int errors, int processedItems)
        {
            this.ErrList = errlist;
            this.Errors = errors;
            this.ProcessedItems = processedItems;
        }
    }
}
