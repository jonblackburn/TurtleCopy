# TurtleCopy
## A developer tool for copying files **_slowly_**.  

TurtleCopy is a command line application, written in C#.  It accepts 3 parameters: Source, Destination, and Duration of the copy.

    Usage:   TurtleCopy.exe SourcePath DestinationPath Duration
        
      SourcePath: The path to the source file, include quotes if filename contains spaces
      DestinationPath: The path, including a file name, to which the file should be copied
      Duration: The time (in seconds) that it should take to copy the file to the destination
      
As the file copies it will create and lock a temp file containing a ".filepart" extension.  
once the file is complete it will rename it, removing ".filepart" and release the lock.
