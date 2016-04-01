cd %~dp0
:: generate wsdl. Run this when there's a breaking change in the interface. You still need to be careful about interface versioning.
"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\Bin\NETFX 4.5.1 Tools\svcutil.exe" bin\WcfService1.dll /directory:..\deployment2
pause