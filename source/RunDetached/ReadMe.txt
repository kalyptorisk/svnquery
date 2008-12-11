
Starts a new detached process that doesn't inherit any handles. This is 
necessary to detach a long running process from a subversion post-commit
hook. Subversion internally creates pipes that are inherited to the hook 
process. Subversion continues only when all pipes are closed, so processes
created by the START command or the .NET Process. Start still block the
subversion process and therefore the committing client. 
Under windows, you need to use the DETACHED_PROCESS flag and set bInherit
to FALSE to create a process that can run in parralel to a subversion hook.

Usage:
  SDP <program.exe> [arguments]
  starts program.exe with arguments as a detached process.


