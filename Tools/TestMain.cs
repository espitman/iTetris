using System;
using System.IO;
using iTetris.Core;
class TestMain {static void Main(){string result=RulesVerification.Run()+"\n"+BrickGardenVerification.Run();Console.Write(result);File.WriteAllText("Documentation/RulesTests.txt",result);}}
