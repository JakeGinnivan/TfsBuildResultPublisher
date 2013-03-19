TfsCreateBuild
==============

In TFS it's possible to create "faked" builds, without actually running them. You can read about it in this [blog post](http://blogs.msdn.com/b/jpricket/archive/2010/02/23/creating-fake-builds-in-tfs-build-2010.aspx) by [Jason Prickett](http://social.msdn.microsoft.com/profile/jason%20prickett%20-%20msft/).

Main scenarios are:

 - Using a different build tool than TFS Build 
(by creating faked builds will let you use some of the great TFS features).
 - Creating sample data to show off TFS functionality
You can create "faked" builds on the command line using the following tool.

## Syntax/Usage:

`TfsCreateBuild.exe /collection:http://tfsserver:8080/tfs/MyCollection 
/project:TeamProject /builddefinition:"Daily Build" 
/buildnumber:"MyApplication_Daily_1.0"`

## References
**Original Blog post:** [http://blogs.msdn.com/b/jpricket/archive/2010/02/23/creating-fake-builds-in-tfs-build-2010.aspx](http://blogs.msdn.com/b/jpricket/archive/2010/02/23/creating-fake-builds-in-tfs-build-2010.aspx)  
Inspiration for creating a 2012 command line tool: [http://msmvps.com/blogs/vstsblog/archive/2011/04/26/creating-fake-builds-in-tfs-build-2010-using-the-command-line.aspx](http://msmvps.com/blogs/vstsblog/archive/2011/04/26/creating-fake-builds-in-tfs-build-2010-using-the-command-line.aspx)
