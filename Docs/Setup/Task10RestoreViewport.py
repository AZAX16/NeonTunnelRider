"""Restore the original selected viewport and remove only Task10's custom sizes."""
import json
import sys
sys.dont_write_bytecode=True
from Task10Tools import evaluate,save,EVIDENCE

def main():
    original=json.loads((EVIDENCE/'viewport-setup.json').read_text())['data']['result']['result']['oldIndex']
    assert not evaluate('return UnityEditor.EditorApplication.isPlaying;')
    code='''var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
var a=typeof(UnityEditor.Editor).Assembly; var t=a.GetType("UnityEditor.GameViewSizes");
var singleton=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance",flags).GetValue(null);
var group=t.GetProperty("currentGroup",flags).GetValue(singleton);
var custom=(System.Collections.IList)group.GetType().GetField("m_Custom",flags).GetValue(group); int removed=0;
for(int i=custom.Count-1;i>=0;i--) { var size=custom[i]; if((string)size.GetType().GetProperty("baseText",flags).GetValue(size)=="Task10 verification") { custom.RemoveAt(i); removed++; } }
t.GetMethod("Changed",flags).Invoke(singleton,null);
var viewType=a.GetType("UnityEditor.GameView"); var view=UnityEditor.EditorWindow.GetWindow(viewType);
viewType.GetProperty("selectedSizeIndex",flags).SetValue(view,ORIGINAL_INDEX); view.Repaint();
return new {removed,selected=(int)viewType.GetProperty("selectedSizeIndex",flags).GetValue(view)};'''.replace('ORIGINAL_INDEX',str(original))
    save('viewport-restored.json',evaluate(code))

if __name__=='__main__': main()
