# sln-tools - CLI helper for Visual Studio .sln & .csproj files
## Introduction
Support the following operations
```
usage: sln-tools COMMAND [OPTIONS]+
        switch               switch dll reference to project reference in a
                               solution
        merge                merge solution files to a single solution
        remove-imports       remove imports from project files
        remove-projects      remove projects from solution files
```

### switch - Switch DLL reference to project references
A lot of large legacy codebase use DLL reference for hundreds of projects to a single Assembly/Artifacts folder, without a single .sln file to contain or group these projects.
`switch` command will replace DLL reference with project reference.
- Support replacing .csproj inside solutions 
- Support glob-matched projects outside solutions and automatically add them to .sln files. 
- Automatically resolve GUID conflicts. This is handy because a lot of developers just copy paste these .csproj files, not knowing VS Solution can not load projects with duplicate GUID

```
sln-tools switch --help
  -s, --solution=VALUE       the path to solution file.
  -p, --pattern=VALUE        the path to project files
  -i, --in                   include only projects inside source solution file
  -w, --write                overwrite project file(s)
  -h, --help                 show options
```

## merge - Merge multiple solution files to a single solution
There's a CLI tool (`slntools`) that handles merging solution files, but in a way that is not helpful to me. So `merge` command will add projects in child .sln files to a single .sln parent. 
- Support merging multiple .sln files using glob-pattern
- Automatically resolve csproj duplicates, GUID conflicts.

```
sln-tools merge --help
  -s, --source=VALUE         the glob pattern to match solution files
  -t, --target=VALUE         the target solution file
  -f, --add-folder           add relative solution folder
  -g, --add-same-guid        add projects with same guid by creating new guid
  -w, --write                overwrite project file(s)
  -h, --help                 show options
```

## remove-imports - Remove a specific import statement inside .csproj files matching a glob pattern
This can be useful when you want to remove an erroneous import statement in legacy codebase. MSBuild tool is simply unable to load .csproj with erroneous import statements.

```
sln-tools remove-imports --help
  -f, --files=VALUE          the glob pattern for project files.
  -p, --pattern=VALUE        the regex pattern for import statement
  -w, --write                overwrite project file(s)
  -h, --help                 show options
```

## remove-proj - Remove regex-matched projects in solution files
Useful when you are tired of removing hundreds of, say UnitTest projects, from solution files.

```
sln-tools remove-projects --help
  -s, --solution=VALUE       the path to solution files.
  -p, --pattern=VALUE        the regex pattern for project name
  -t, --target=VALUE         the target solution file to write to
  -h, --help                 show options
```