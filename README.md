# Windows installers for the Elastic stack

This is the repository for the Elastic stack MSI-based Windows installers.

![Example Install Flow](images/example-flow.gif)

**NOTE**: *Building from source should only be done for development purposes. Only the officially distributed and signed Elastic installer should be used in production. Using an unofficial Elastic installer is not supported.*

## Installation, Upgrades and Uninstallation

Elasticsearch can be installed on Windows using the `.msi` package which can be found on the [downloads page](https://www.elastic.co/downloads/elasticsearch). This `.msi` can install Elasticsearch as a Windows service or allow it to be run manually using the included `elasticsearch.exe` executable.

Installation, upgrades and uninstallation is possible using [the command line](https://www.elastic.co/guide/en/elasticsearch/reference/current/windows.html#install-msi-command-line) or via a [graphical user interface](https://www.elastic.co/guide/en/elasticsearch/reference/current/windows.html#install-msi-gui). All settings exposed within the GUI are also available [as command line arguments](https://www.elastic.co/guide/en/elasticsearch/reference/current/windows.html#msi-command-line-options) (referred to as properties within Windows Installer documentation) that can be passed to msiexec.exe.


## Reporting problems
To report any problems encountered during installation, or to request features, please open an [issue](https://github.com/elastic/windows-installers/issues) on GitHub and attach the MSI installation log if applicable. 

- When installing from the command-line, the log file can be captured by passing the `/l <logfilename>`.
- When installing through the UI, a link to the log file will be provided at the end of the installation.

For general questions and comments, please use the Elastic [discussion forum](https://discuss.elastic.co/).

## Building

Clone the repository and run the build script (`build.bat`) which will download the latest version of the stack and create the installation MSIs. You can specify a specific version when building and there are many other configuration options available, run the following to see them all

```bat
build.bat help
```

If you check out this repository in Windows Subsystem for Linux then the the case sensitive flag will get set on the checkout folder which cause conflicts with WixToolSet as it uppercases the filenames.

From procmon:

> light.exe    5744    CreateFile    <checkout>\BUILD\IN\ELASTICSEARCH-6.4.0\MODULES\X-PACK-WATCHER\ACTIVATION-1.1.1.JAR  

This will yield a `light.exe : error LGHT0001 : The system cannot find the path specified. (Exception from HRESULT: 0x80070003)` during the `FAKE` build which will then complain it can not find the `msi`.



To fix this run `fsutil.exe file setCaseSensitiveInfo <checkout> disable`

## Where to look for information that helps us troubleshoot:

HINT: wrapper process accepts `--debug-env` command line argument which might help in troubleshooting as well:

![image](https://user-images.githubusercontent.com/51912343/107683221-f05ca700-6c66-11eb-8692-18f85cdbe1c3.png)

On Windows we run Elasticsearch using a wrapper service process. This process reports its errors to Windows Event Log. Please take note of `Error` entries, They usually contain vital clues: `Service cannot be started. Elastic.ProcessHosts.Process.StartupException: Could not evaluate jvm.options file. `
