# troubleshooting

## inspecting an existing build

If you need to inspect or troubleshoot an existing build the the Windows Installer database and be converted into a set of WiX source files:

```bash
> ./packages/WixSharp.wix.bin/tools/bin/dark.exe elasticsearch.msi

Windows Installer XML Toolset Decompiler version 3.11.0.1701
Copyright (c) .NET Foundation and contributors. All rights reserved.

elasticsearch.msi -> elasticsearch.wxs
```