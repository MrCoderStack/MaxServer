# MaxServer
3dmax 远程执行命令服务

使用vs2010编译，通过后将MAXScriptWebServer.dll复制到C:\Program Files\Autodesk\3ds Max 2014\bin\assemblies\(3dmax安装目录,根据个人实际情况),启动3dmax（2014,其它版本未测）,
打开3dmax script调试器运行startup.ms内容即可，之后只需post maxscript脚本命令至3dmax服务部署的IP和端口下即可执行。


extra提供一些字节写的bat，帮助你更快的使用
