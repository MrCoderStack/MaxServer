fn startWebServer = (
 Assembly = dotNetClass "System.Reflection.Assembly"
 maxroot = pathConfig.GetDir #maxroot
 Assembly.LoadFile (maxroot + "\\bin\\assemblies\\MaxServer.dll")
 cls = dotNetClass "MaxServer.MaxServer"
 server = dotNetObject cls
 print "Created: " + server.actionText
 server.execute undefined
)
startWebServer ()