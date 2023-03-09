This folder is based off the [DocumentationSrc~ Folder in the IVLab's Template-UnityPackage](https://github.umn.edu/ivlab-cs/Template-UnityPackage/tree/main/DocumentationSrc%7E).
However, there are several key differences:

- in <docfx.json>:
    - `src` is changed to use the actual `*.csproj` files generated by Unity instead of the faked `api-documentation.csproj` file.
    - `xref`s for Unity are added so cross references to Unity types link to the correct Unity documentation.
    - `xrefService` added to perform cross reference lookup.
- in <filterConfig.yml>
    - Only include namespaces that actually matter to this package. Exclude everything else.
- in <templates/statictoc-with-github-umn-edu/class.header.tmpl.partial>
    - Deleted the section on "Inherited methods"; this is distracting and takes up a huge amount of space.