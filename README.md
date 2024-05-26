# Console

Very simple `PowerShell` module for reading from stdin and writing to stdout

Build using

```
$ dotnet publish --configuration Release
```

Install by copying the publish into a directory on the [PSModulePath](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_psmodulepath)

The commands read and write either strings or byte arrays.

## Read-Console

```
Read-Console [-AsByteStream] [-ReadCount <int>]
```

In byte mode, byte arrays are written as they as read.

In text mode, strings are read line by line.

## Write-Console

```
Write-Console [-NoNewline] [-InputObject <IEnumerable[byte]>] [-InputString <IEnumerable[char]>]
```

This writes byte and character data directly to the console output.
