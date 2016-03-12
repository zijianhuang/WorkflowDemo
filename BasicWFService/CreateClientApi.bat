:: Workflow WCF service generates contracts info at run time. So the service needs to be running to provide meta data trhough contract inference.
start "Launch IIS Express" "C:\Program Files (x86)\IIS Express\iisexpress.exe" /site:"BasicWFService" /apppool:"Clr4IntegratedAppPool" /config:"C:\VsProjects\FonlowWorkflowDemo\.vs\config\applicationhost.config"

"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\Bin\NETFX 4.5.1 Tools\svcutil.exe" http://localhost:3065/Service1.xamlx?singleWsdl /noConfig /language:C# /n:http://fonlow.com/WorkflowDemo,Fonlow.WorkflowDemo.Clients /directory:..\BasicWFServiceClientApi /out:WFServiceClientApiAuto.cs
pause