cd %~dp0
rem generate client proxy classes with the wsdl
"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\Bin\NETFX 4.5.1 Tools\svcutil.exe" ..\deployment2\*.wsdl ..\deployment2\*.xsd /language:C# /n:http://fonlow.com/WorkflowDemo,Fonlow.WcfServic1.Client /o:ClientApiAuto.cs /config:appAuto.config