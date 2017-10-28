// Custom Fiddler JScript to dump the current session to file
// place in [Environment]::GetFolderPath("MyDocuments")\Fiddler2\Scripts before loading Fiddler

import System;
import System.Windows.Forms;
import Fiddler;

class Handlers {
  static function OnExecAction(sParams: String[]): Boolean {
    FiddlerObject.StatusText = "ExecAction: " + sParams[0];
    var sAction = sParams[0].toLowerCase();
    switch(sAction) {
      case "dump_session": 
      var oSessions = FiddlerApplication.UI.GetAllSessions(); 
      var oExportOptions = FiddlerObject.createDictionary(); 
      oExportOptions.Add("Filename", "C:\\session.har"); 
      FiddlerApplication.DoExport("HTTPArchive v1.2", oSessions, oExportOptions, null); 
      break; 
    }
  }
}
