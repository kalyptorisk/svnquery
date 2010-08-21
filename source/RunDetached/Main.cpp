// Copyright 2008-2010 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#include "stdafx.h"

int _tmain()
{
    LPWSTR pCmd = ::GetCommandLine();

    // skip the executable
    if (*pCmd++ == L'"') while (*pCmd++ != L'"'); 
    else while (*pCmd != NULL && *pCmd != L' ') ++pCmd;
    while (*pCmd == L' ') ++pCmd;

    STARTUPINFO si;
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);

    PROCESS_INFORMATION pi;
    ZeroMemory( &pi, sizeof(pi) );

    // Start the child process. 
    BOOL result = CreateProcess
    ( 
        NULL,              // No module name (use command line)
        pCmd,              // Command line
        NULL,              // Process handle not inheritable
        NULL,              // Thread handle not inheritable
        FALSE,             // Set bInheritHandles to FALSE
        DETACHED_PROCESS,  // Detach process 
        NULL,              // Use parent's environment block
        NULL,              // Use parent's starting directory 
        &si,               // Pointer to STARTUPINFO structure
        &pi                // Pointer to PROCESS_INFORMATION structure (returned)
    );           

    if (result) 
    {
        SetPriorityClass(pi.hProcess, BELOW_NORMAL_PRIORITY_CLASS);
        return 0;
    }

    // in case of errors 
    wchar_t msg[2048];    
    FormatMessage
    (
        FORMAT_MESSAGE_FROM_SYSTEM, 
        NULL, 
        ::GetLastError(), 
        MAKELANGID(LANG_NEUTRAL, SUBLANG_SYS_DEFAULT),
        msg, sizeof(msg), 
        NULL
    );
    fputws(msg, stderr);    
    _flushall();

    return -1;
}

