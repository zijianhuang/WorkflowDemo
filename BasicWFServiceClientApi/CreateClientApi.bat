cd %~dp0
rem generate client proxy classes with the wsdl
"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\Bin\NETFX 4.5.1 Tools\svcutil.exe" ..\deployment\Service1.wsdl  /language:C# /n:http://fonlow.com/WorkflowDemo,Fonlow.WorkflowDemo.Clients /o:WFServiceClientApiAuto.cs /config:appAuto.config