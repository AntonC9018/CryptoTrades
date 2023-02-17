Use this tool to fetch dependencies for Unity.

The `copy` project includes the references to packages. It is required in order to set force the version to .netstandard 2.1.

The `fetch` project references `copy` and should be invoked to copy the referenced dlls with the documentation files. Its version is set to .net7.0 so it can be run.