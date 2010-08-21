
Starts a new detached process that doesn't inherit any handles. This is 
necessary to detach a long running process from a subversion post-commit
hook. Subversion internally creates pipes that are inherited to the hook 
process. Subversion waits for all pipes being closed, so a process
created by the START command blocks the subversion process and hence 
the committing client. Under windows you need to use the DETACHED_PROCESS 
flag and set bInherit to FALSE to create a process that can run parallel 
to a subversion hook process. 

Usage:
  RunDetached <program.exe> [arguments]
  starts program.exe with arguments as a detached process 


