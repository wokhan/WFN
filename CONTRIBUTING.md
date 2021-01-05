# How to help on WFN?

*Please note this is a draft. To be completed.*

## For Everyone

You can use the app and tell us what you think or what we should improve. 

No need to be a geek but please keep in mind this is a power-user tool...

We'll do whatever we can to help, but you must keep in mind you can face unwanted issues or consequences, since there is no way for us to test the app with all and every configurations. 


And if you believe in this project, you can donate using the Sponsors link above!

Donations will be redistributed amongst all members according to their investment in this project. 

## For Developers

While you can of course create forks of WFN, I think it would be better to contribute directly on the main repo. 

You can do it either by creating pull requests, or by joining the team. 

Some rules apply, of course... 

### Requirements

Any IDE would do as long as conventions are followed. 

The best one being Visual Studio (Community is way enough) 2019 (last update to support .NET 5.0).

You can use the latest update, but avoid any preview as they're not production ready. 

### Code convention

The project comes with an editorconfig file which should help normalize your code. 

Standard C# conventions (regarding naming or case) usually apply, but custom ones can also be found. 

*Note: to be completed*

### Branches and merges

The **master** branch is protected. 

Any modification must be done in a newly created branch, named after the following convention (thanks @harrwiss):

Bug fixing: ***bugfix**/short_description*

New features: ***feature**/my_new_feature*

Bug fixing on official releases: *2.1/**bugfix**/short_description*

A review is then required to merge with master. 

### Nightlies

Nightlies will be automatically compiled on each push / merge on the master branch, and will always be uploaded in the same "Nightly" Release.

### Releases

Releases can be created the standard way through GitHub Releases feature.

WFN will be compiled once a release is created, and named according to the tag name which must reflect the current version (ex : if the tag/release name is v2.0.0-beta, the asset will be named wfn-[x64|x86]{-standalone}-v2.0.0-beta.zip).

So you don't have to compile locally and upload your assets: it will be done automatically by the "Release" GitHub action.

Note that three packages will be created:
- one for x64 (standalone)
- one for x86 (standalone)
- one for AnyCPU (requires .NET to be installed on the client computer)
