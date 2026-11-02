using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NeonTunnelRunner
{
    public static class Task11Viewport
    {
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        public static object Configure()
        {
            var a=typeof(Editor).Assembly;
            var viewType=a.GetType("UnityEditor.GameView"); var view=EditorWindow.GetWindow(viewType);
            var sizeType=a.GetType("UnityEditor.GameViewSize"); var sizesType=a.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance",Flags).GetValue(null);
            var group=sizesType.GetProperty("currentGroup",Flags).GetValue(singleton);
            int old=(int)viewType.GetProperty("selectedSizeIndex",Flags).GetValue(view);
            var ctor=sizeType.GetConstructors(Flags).First(c=>c.GetParameters().Length==4);
            object size=ctor.Invoke(new object[]{Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"),1),1280,720,"Task11 verification"});
            group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null);
            viewType.GetProperty("selectedSizeIndex",Flags).SetValue(view,count-1);
            view.Repaint();
            return new {oldIndex=old,addedIndex=count-1};
        }
    }
}
