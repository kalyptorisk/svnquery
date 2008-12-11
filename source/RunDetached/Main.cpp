// RunDetached
// Copyright 2008 by Christian Rodemeyer

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

    if (result) return 0;
    
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

