using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace VoarVR.Editor
{
    public sealed class TelemetryBuildStamp:IPreprocessBuildWithReport
    {
        public int callbackOrder=>-100;
        public void OnPreprocessBuild(BuildReport report)
        {
            var info=new ProcessStartInfo("git","rev-parse HEAD") { WorkingDirectory=Path.GetFullPath(".."),RedirectStandardOutput=true,UseShellExecute=false,CreateNoWindow=true };
            string revision="unknown";
            using(var p=Process.Start(info)) { revision=p.StandardOutput.ReadToEnd().Trim();p.WaitForExit();if(p.ExitCode!=0)throw new BuildFailedException("Cannot identify build revision"); }
            // Source-content fingerprint identifies uncommitted tuning without invoking
            // Git LFS or claiming the HEAD alone describes a dirty development build.
            using(var hash=SHA256.Create())
            {
                foreach(var file in Directory.GetFiles("Assets/Game","*.cs",SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles("Assets/Art/Models","*.fbx"))
                    .Concat(Directory.GetFiles("Assets/Resources/Characters","*.asset")).OrderBy(x=>x,StringComparer.Ordinal))
                {
                    var name=Encoding.UTF8.GetBytes(file);hash.TransformBlock(name,0,name.Length,null,0);
                    var bytes=File.ReadAllBytes(file);hash.TransformBlock(bytes,0,bytes.Length,null,0);
                }
                hash.TransformFinalBlock(Array.Empty<byte>(),0,0);
                revision+=" / SourceSHA256="+BitConverter.ToString(hash.Hash).Replace("-","").ToLowerInvariant();
            }
            File.WriteAllText("Assets/Resources/BuildRevision.txt",revision+"\n");
            AssetDatabase.ImportAsset("Assets/Resources/BuildRevision.txt",ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
